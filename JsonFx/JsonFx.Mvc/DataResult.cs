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
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Mvc;

using JsonFx.Json;

namespace JsonFx.Mvc
{
	/// <summary>
	/// Serializes data according to a specified format
	/// </summary>
	public class DataResult : ActionResult
	{
		#region Constants

		private const string DefaultContentType = "text/plain";

		#endregion Constants

		#region Fields

		private readonly IDataWriterProvider Provider;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="provider"></param>
		public DataResult(IDataWriterProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}

			this.Provider = provider;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the data to be serialized
		/// </summary>
		public object Data
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets the HTTP status code of the response
		/// </summary>
		public HttpStatusCode HttpStatusCode
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets a filename hint
		/// </summary>
		/// <remarks>
		/// Used in Content-Disposition header
		/// </remarks>
		public string Filename
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the underlying IDataWriter
		/// </summary>
		public IDataWriterProvider DataWriterProvider
		{
			get { return this.Provider; }
		}

		#endregion Properties

		#region ActionResult Members

		/// <summary>
		/// Serializes the data using the specified IDataWriter
		/// </summary>
		/// <param name="context">ControllerContext</param>
		public override void ExecuteResult(ControllerContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			HttpRequestBase request = context.HttpContext.Request;
			HttpResponseBase response = context.HttpContext.Response;

			IDataWriter writer = this.Provider.Find(request.Headers["Accept"], request.Headers["Content-Type"]);
			if (writer == null)
			{
				writer = this.Provider.Find(request.RawUrl);
			}
			if (writer == null)
			{
				writer = this.Provider.DefaultDataWriter;
			}
			if (writer == null)
			{
				throw new InvalidOperationException("No available IDataWriter implementations");
			}

			// need this to write out custom error objects
			response.TrySkipIisCustomErrors = true;

			if (this.HttpStatusCode != default(HttpStatusCode))
			{
				response.StatusCode = (int)this.HttpStatusCode;
			}

			if (String.IsNullOrEmpty(writer.ContentType))
			{
				// use the default content type
				response.ContentType = DataResult.DefaultContentType;
			}
			else
			{
				// set the response content type
				response.ContentType = writer.ContentType;
			}

			if (writer.ContentEncoding != null)
			{
				// set the response content encoding
				response.ContentEncoding = writer.ContentEncoding;
			}

			string ext = writer.FileExtension;
			string filename = this.Filename;
			if (!String.IsNullOrEmpty(ext) ||
				!String.IsNullOrEmpty(filename))
			{
				if (String.IsNullOrEmpty(filename))
				{
					filename = request.RawUrl;
				}
				filename = this.ScrubFilename(filename, ext??String.Empty);

				// this helps IE determine the Content-Type
				// http://tools.ietf.org/html/rfc2183#section-2.3
				ContentDisposition disposition = new ContentDisposition
				{
					Inline = true,
					FileName = filename
				};
				response.AddHeader("Content-Disposition", disposition.ToString());
			}

			if (this.Data != null)
			{
				writer.Serialize(response.Output, this.Data);
			}
		}

		#endregion ActionResult Members

		#region Utility Methods

		/// <summary>
		/// Produces a header friendly name which ends in the given extension
		/// </summary>
		/// <param name="url"></param>
		/// <param name="ext"></param>
		/// <returns></returns>
		private string ScrubFilename(string url, string ext)
		{
			int last = 0,
				length = url.Length;

			StringBuilder builder = new StringBuilder(length + ext.Length);
			for (int i=0; i<length; i++)
			{
				char ch = url[i];
				if (Char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
				{
					// skip safe chars
					continue;
				}
				if (ch == '?')
				{
					// effectively terminate string
					length = i;
				}

				if (last < i)
				{
					// write out any unwritten safe chars
					builder.Append(url, last, i-last);
				}
				// effectively skip char
				last = i+1;
			}
			if (last < length)
			{
				// write out any trailing safe chars
				builder.Append(url, last, length-last);
			}
			if (builder.Length == 0)
			{
				// no safe chars, just use simple name
				builder.Append("data");
			}

			if (ext.Length > 0 && ext[0] != '.')
			{
				builder.Append('.');
			}
			builder.Append(ext);

			return builder.ToString();
		}

		#endregion Utility Methods
	}
}
