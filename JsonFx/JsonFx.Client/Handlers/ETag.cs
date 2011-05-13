#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2009 Stephen M. McKamey

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.

\*---------------------------------------------------------------------------------*/
#endregion License

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Globalization;

namespace JsonFx.Handlers
{
	/// <summary>
	/// Generates an HTTP/1.1 Cache header Entity Tag (ETag)
	/// </summary>
	/// <remarks>
	/// HTTP/1.1 RFC:
	/// http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.19
	/// http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.26
	/// http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.25
	/// http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.29
	/// </remarks>
	public abstract class ETag
	{
		#region Constants

		private static readonly string RequestETagHeader = "If-None-Match";
		private static readonly string ResponseETagHeader = "ETag";
		private static readonly string RequestDateHeader = "If-Modified-Since";
		private static readonly string ResponseDateHeader = "Last-Modified";
		private static readonly int NotModified = (int)HttpStatusCode.NotModified;
		private static readonly SHA1 HashProvider = SHA1.Create();

		#endregion Constants

		#region Fields

		private string value = null;
		private DateTime? lastModified = null;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets the ETag value for the associated entity
		/// </summary>
		public string Value
		{
			get
			{
				if (this.value == null)
				{
					this.value = this.CalculateETag();
				}
				return this.value;
			}
		}

		/// <summary>
		/// Gets the UTC Last-Modified date for the resource. Returns DateTime.MinValue if does not apply.
		/// </summary>
		protected DateTime LastModifiedUtc
		{
			get
			{
				if (!this.lastModified.HasValue)
				{
					DateTime time = this.GetLastModified();
					if (time > DateTime.MinValue)
					{
						time = time.ToUniversalTime();
						if (time > DateTime.UtcNow)
						{
							time = DateTime.UtcNow;
						}
						time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Utc);
					}
					this.lastModified = time;
				}
				return this.lastModified.Value;
			}
		}

		#endregion Properties

		#region Public Methods

		/// <summary>
		/// Verifies if the client has a cached copy of the resource.
		/// Sets up HttpResponse appropriately.
		/// Returns true if cached.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>true if is cached</returns>
		public bool HandleETag(HttpContext context)
		{
			return this.HandleETag(context, HttpCacheability.ServerAndPrivate, false);
		}

		/// <summary>
		/// Verifies if the client has a cached copy of the resource.
		/// Sets up HttpResponse appropriately.
		/// Returns true if cached.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cacheability"></param>
		/// <returns>true if is cached</returns>
		public bool HandleETag(HttpContext context, HttpCacheability cacheability)
		{
			return this.HandleETag(context, cacheability, false);
		}

		/// <summary>
		/// Verifies if the client has a cached copy of the resource.
		/// Sets up HttpResponse appropriately.
		/// Returns true if cached.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cacheability"></param>
		/// <param name="forceRefresh"></param>
		/// <returns>true if is cached</returns>
		public bool HandleETag(HttpContext context, HttpCacheability cacheability, bool forceRefresh)
		{
			if (context == null)
			{
				throw new ArgumentNullException("request");
			}
			HttpRequest request = context.Request;
			if (request == null)
			{
				throw new ArgumentNullException("context.Request");
			}
			HttpResponse response = context.Response;
			if (response == null)
			{
				throw new ArgumentNullException("context.Response");
			}

			// this is needed otherwise other headers aren't set
			context.Response.Cache.SetCacheability(cacheability);
			// this is needed otherwise other settings are ignored
			context.Response.Cache.SetOmitVaryStar(true);

			// check request ETag
			bool isCached = false;
			string requestETag = request.Headers[ETag.RequestETagHeader];
			if (!forceRefresh && !String.IsNullOrEmpty(requestETag))
			{
				string[] etags = requestETag.Split(',');
				foreach (string etag in etags)
				{
					// Value is case-sensitive
					if (this.ETagsEqual(this.Value, etag))
					{
						isCached = true;
						break;
					}
				}
			}

			// check request last-modified date
			DateTime lastModified = this.LastModifiedUtc;
			string entityDate = this.FormatTimeHeader(lastModified);
			if (!forceRefresh && !isCached && !String.IsNullOrEmpty(entityDate))
			{
				// do exact string comparison first
				string clientDate = request.Headers[ETag.RequestDateHeader];
				if (entityDate.Equals(clientDate))
				{
					isCached = true;
				}
				else
				{
					// compare as DateTimes
					DateTime clientDateTime;
					if (DateTime.TryParse(clientDate, CultureInfo.InvariantCulture,
						DateTimeStyles.AdjustToUniversal, out clientDateTime))
					{
						isCached = (lastModified <= clientDateTime);
					}
				}
			}

			// specify ETag header
			response.Cache.SetETag(this.Value);
			response.AppendHeader(ETag.ResponseETagHeader, this.Value);

			// setup response
			if (isCached)
			{
				response.ClearContent();
				response.StatusCode = ETag.NotModified;

				// this safely ends request without causing "Transfer-Encoding: Chunked" which chokes IE6
				context.ApplicationInstance.CompleteRequest();
			}
			else
			{
				if (!String.IsNullOrEmpty(entityDate))
				{
					// specify Last-Modified header
					response.Cache.SetLastModified(lastModified);
					response.AddHeader(ETag.ResponseDateHeader, entityDate);
				}
			}

			return isCached;
		}

		#endregion Public Methods

		#region Methods

		/// <summary>
		/// Provides an algorithm for generating an HTTP/1.1 Cache header Entity Tag (ETag)
		/// </summary>
		/// <returns>the value used to generate the ETag</returns>
		/// <remarks>
		/// GetMetaData must return String, Byte[], or Stream
		/// </remarks>
		protected abstract object GetMetaData(out bool isHash);

		/// <summary>
		/// 
		/// </summary>
		/// <returns>DateTime.MinValue if does not apply</returns>
		protected virtual DateTime GetLastModified()
		{
			return DateTime.MinValue;
		}

		/// <summary>
		/// Sets ETag.Value
		/// </summary>
		/// <param name="Entity"></param>
		protected virtual string CalculateETag()
		{
			bool isHash;
			object metaData = this.GetMetaData(out isHash);

			string etag;
			if (metaData is Guid)
			{
				etag = ((Guid)metaData).ToString("N");
			}
			else if (metaData is string)
			{
				if (isHash)
				{
					etag = (string)metaData;
				}
				else
				{
					etag = ETag.ComputeHash((string)metaData);
				}
			}
			else if (metaData is byte[])
			{
				if (isHash)
				{
					etag = ETag.FormatBytes((byte[])metaData);
				}
				else
				{
					etag = ETag.ComputeHash((byte[])metaData);
				}
			}
			else if (metaData is Stream)
			{
				etag = ETag.ComputeHash((Stream)metaData);
			}
			else
			{
				throw new NotSupportedException("GetMetaData must return Guid, String, Byte[], or Stream");
			}

			return "\""+etag+"\"";
		}

		#endregion Methods

		#region Utility Methods

		/// <summary>
		/// Converts a DateTime to a valid header string
		/// </summary>
		/// <returns>null if DateTime.MinValue</returns>
		private string FormatTimeHeader(DateTime time)
		{
			if (DateTime.MinValue.Equals(time))
			{
				return null;
			}
			return time.ToString("R");
		}

		/// <summary>
		/// see System.Web.StaticFileHandler
		/// </summary>
		/// <param name="etag1"></param>
		/// <param name="etag2"></param>
		/// <returns></returns>
		private bool ETagsEqual(string etag1, string etag2)
		{
			if (String.IsNullOrEmpty(etag1) || String.IsNullOrEmpty(etag2))
			{
				return false;
			}
			etag1 = etag1.Trim();
			etag2 = etag2.Trim();

			if (etag1.Equals("*") || etag2.Equals("*"))
				return true;

			if (etag1.StartsWith("W/"))
				etag1 = etag1.Substring(2);

			if (etag2.StartsWith("W/"))
				etag2 = etag2.Substring(2);

			return etag2.Equals(etag1);
		}

		/// <summary>
		/// Generates a unique hash from string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static string ComputeHash(string value)
		{
			// get String as a Byte[]
			byte[] buffer = Encoding.Unicode.GetBytes(value);

			return ETag.ComputeHash(buffer);
		}

		/// <summary>
		/// Generates a unique hash from Stream
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static string ComputeHash(Stream value)
		{
			byte[] hash;

			lock (HashProvider)
			{
				// generate hash
				hash = HashProvider.ComputeHash(value);
			}

			// convert hash to string
			return ETag.FormatBytes(hash);
		}

		/// <summary>
		/// Generates a unique hash from byte[]
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		protected static string ComputeHash(byte[] value)
		{
			byte[] hash;
			lock (HashProvider)
			{
				// generate hash
				hash = HashProvider.ComputeHash(value);
			}

			// convert hash to string
			return ETag.FormatBytes(hash);
		}

		/// <summary>
		/// Gets the hex digits for the given bytes
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string FormatBytes(byte[] value)
		{
			if (value == null || value.Length == 0)
			{
				return String.Empty;
			}

			StringBuilder builder = new StringBuilder();

			// Loop through each byte of the binary data 
			// and format each one as a hexadecimal string
			for (int i=0; i<value.Length; i++)
			{
				builder.Append(value[i].ToString("x2"));
			}

			// the hexadecimal string
			return builder.ToString();
		}

		#endregion Utility Methods
	}

	/// <summary>
	/// Generates an ETag for a specific Guid.
	/// </summary>
	public class HashETag : ETag
	{
		#region Fields

		private readonly string Hash;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="hash"></param>
		public HashETag(Guid hash)
		{
			this.Hash = hash.ToString("N");
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="hash"></param>
		public HashETag(string hash)
		{
			this.Hash = hash;
		}

		#endregion Init

		#region ETag Members

		/// <summary>
		/// Generates a unique ETag which changes when the Content changes
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		protected override object GetMetaData(out bool isHash)
		{
			isHash = true;
			return this.Hash;
		}

		#endregion ETag Members
	}

	/// <summary>
	/// Generates an ETag for an arbitrary string.
	/// </summary>
	public class StringETag : ETag
	{
		#region Fields

		private readonly string Content;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="Content"></param>
		public StringETag(string content)
		{
			this.Content = content;
		}

		#endregion Init

		#region ETag Members

		/// <summary>
		/// Generates a unique ETag which changes when the Content changes
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		protected override object GetMetaData(out bool isHash)
		{
			isHash = false;
			return this.Content;
		}

		#endregion ETag Members
	}

	/// <summary>
	/// Represents an ETag for a file on disk
	/// </summary>
	/// <remarks>
	/// Generates a unique ETag which changes when the file changes
	/// </remarks>
	public class FileETag : ETag
	{
		#region Fields

		private readonly FileInfo info;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="fileName"></param>
		public FileETag(string fileName)
		{
			if (String.IsNullOrEmpty(fileName) || !File.Exists(fileName))
			{
				throw new FileNotFoundException("ETag cannot be created for missing file", fileName);
			}

			this.info = new FileInfo(fileName);
		}

		#endregion Init

		#region ETag Members

		/// <summary>
		/// Generates a unique ETag which changes when the file metadata changes
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		protected override object GetMetaData(out bool isHash)
		{
			isHash = false;

			string value = this.info.FullName.ToLowerInvariant();
			value += ";"+this.info.Length.ToString();
			value += ";"+this.info.CreationTimeUtc.Ticks.ToString();
			value += ";"+this.info.LastWriteTimeUtc.Ticks.ToString();

			return value;
		}

		/// <summary>
		/// Gets the LastWriteTimeUtc time associated with the file
		/// </summary>
		protected override DateTime GetLastModified()
		{
			return this.info.LastWriteTimeUtc;
		}

		#endregion ETag Members
	}

	/// <summary>
	/// Represents an ETag for a file on disk
	/// </summary>
	/// <remarks>
	/// Generates a unique ETag which changes when the file changes
	/// </remarks>
	public class EmbeddedResourceETag : ETag
	{
		#region Fields

		private readonly Assembly Assembly;
		private readonly string ResourceName;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="fileName"></param>
		public EmbeddedResourceETag(Assembly assembly, string resourceName)
		{
			this.Assembly = assembly;
			this.ResourceName = resourceName;
		}

		#endregion Init

		#region ETag Members

		/// <summary>
		/// Generates a unique ETag which changes when the assembly changes
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		protected override object GetMetaData(out bool isHash)
		{

			if (this.Assembly == null)
			{
				throw new NullReferenceException("ETag cannot be created for null Assembly");
			}

			if (String.IsNullOrEmpty(this.ResourceName))
			{
				throw new NullReferenceException("ETag cannot be created for empty ResourceName");
			}

			isHash = true;
			Hash hash = new Hash(this.Assembly);
			return hash.SHA1;
		}

		#endregion ETag Members
	}
}
