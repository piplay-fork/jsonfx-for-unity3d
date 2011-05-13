#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2010 Stephen M. McKamey

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
using System.IO.Compression;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;

using JsonFx.Compilation;

namespace JsonFx.Handlers
{
	/// <remarks>
	/// The possible encoding methods for build results
	/// </remarks>
	internal enum BuildResultType
	{
		PrettyPrint,
		Compact,
		Gzip,
		Deflate
	}

	/// <summary>
	/// general HTTP handler for external page resources
	/// </summary>
	public class ResourceHandler : IHttpHandler
	{
		#region Constants

		internal const string GlobalizationQuery = "lang";

		private const string GzipContentEncoding = "gzip";
		private const string DeflateContentEncoding = "deflate";

		private const string HeaderAcceptEncoding = "Accept-Encoding";
		private const string HeaderContentEncoding = "Content-Encoding";

		#endregion Constants

		#region Fields

		private readonly bool IsDebug;
		private readonly string CacheKey;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="context"></param>
		public ResourceHandler(HttpContext context)
			: this(context.IsDebuggingEnabled, context.Request.QueryString[null])
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="isDebug"></param>
		/// <param name="cacheKey"></param>
		public ResourceHandler(bool isDebug, string cacheKey)
		{
			this.IsDebug = isDebug;
			this.CacheKey = cacheKey;
		}

		#endregion Init

		#region IHttpHandler Members

		void IHttpHandler.ProcessRequest(HttpContext context)
		{
			HttpResponse response = context.Response;

			try
			{
				response.ClearHeaders();
				response.ClearContent();
			}
			catch { }

			response.TrySkipIisCustomErrors = true;
			response.BufferOutput = true;

			// specifying "DEBUG" in the query string gets the non-compacted form
			IOptimizedResult info = this.GetResourceInfo(context, this.IsDebug);
			if (info == null)
			{
				// either eTag 304 was sent or no resource found
				return;
			}

			bool isCached = StringComparer.OrdinalIgnoreCase.Equals(info.Hash, this.CacheKey);
			if (isCached)
			{
				// if the content changes then so will the hash
				// so we can effectively cache this forever
				response.ExpiresAbsolute = DateTime.UtcNow.AddYears(1);
			}

			response.ContentType = info.ContentType;

			string filename = Path.GetFileNameWithoutExtension(context.Request.FilePath)+'.'+info.FileExtension;

			// this helps IE determine the Content-Type
			// http://tools.ietf.org/html/rfc2183#section-2.3
			ContentDisposition disposition = new ContentDisposition
			{
				Inline = true,
				FileName = filename
			};
			response.AddHeader("Content-Disposition", disposition.ToString());

			ResourceHandler.WriteResponse(context, info, this.IsDebug);
		}

		bool IHttpHandler.IsReusable
		{
			get { return true; }
		}

		#endregion IHttpHandler Members

		#region ResourceHandler Members

		protected internal static string GetLocalizationUrl(string path, string culture)
		{
			string query = ResourceHandler.GlobalizationQuery+'='+culture;

			int index = path.IndexOf('?');
			if (index < 0)
			{
				return path+'?'+query;
			}
			else
			{
				return path+'&'+query;
			}
		}

		protected internal static string GetResourceUrl(IBuildResult info, string path, bool isDebug)
		{
			if (info == null)
			{
				if ((path == null) || (path.IndexOf("://") < 0))
				{
					return path;
				}

				string alt;
				MergeResourceCodeProvider.SplitAlternates(path, out path, out alt);
				return isDebug ? path : alt;
			}

			string cache;
			if (isDebug)
			{
				cache = '?'+DebugResourceHandlerFactory.DebugFlag;
			}
			else if (!String.IsNullOrEmpty(info.Hash))
			{
				cache = '?'+info.Hash;
			}
			else
			{
				cache = "";
			}

			int index = path.IndexOf('?');
			if (index >= 0)
			{
				path = path.Substring(0, index);
			}

			return path+cache;
		}

		/// <summary>
		/// Determines the appropriate source for the incomming request
		/// </summary>
		/// <param name="context"></param>
		/// <param name="isDebug"></param>
		/// <returns>CompiledBuildResult</returns>
		protected virtual IOptimizedResult GetResourceInfo(HttpContext context, bool isDebug)
		{
			string virtualPath = context.Request.AppRelativeCurrentExecutionFilePath;
			IOptimizedResult info = ResourceHandler.Create<IOptimizedResult>(virtualPath);
			if (info == null)
			{
				throw new HttpException(404, "Resource not found: "+virtualPath);
			}

			// check if client has cached copy
			ETag etag = new HashETag(info.Hash);
			if (etag.HandleETag(context, HttpCacheability.ServerAndPrivate, isDebug))
			{
				return null;
			}

			return info;
		}

		#endregion ResourceHandler Members

		#region Utility Methods

		/// <summary>
		/// Writes the infor object to the HttpResult stream
		/// </summary>
		/// <param name="context"></param>
		/// <param name="info"></param>
		/// <param name="isDebug"></param>
		public static void WriteResponse(HttpContext context, IOptimizedResult info, bool isDebug)
		{
			HttpResponse response = context.Response;

			switch (ResourceHandler.GetOutputEncoding(info, context, isDebug))
			{
				case BuildResultType.PrettyPrint:
				{
					response.ContentEncoding = Encoding.UTF8;
					response.Output.Write(info.PrettyPrinted);
					break;
				}
				case BuildResultType.Gzip:
				{
					response.AppendHeader(ResourceHandler.HeaderContentEncoding, ResourceHandler.GzipContentEncoding);
					response.OutputStream.Write(info.Gzipped, 0, info.Gzipped.Length);
					break;
				}
				case BuildResultType.Deflate:
				{
					response.AppendHeader(ResourceHandler.HeaderContentEncoding, ResourceHandler.DeflateContentEncoding);
					response.OutputStream.Write(info.Deflated, 0, info.Deflated.Length);
					break;
				}
				case BuildResultType.Compact:
				default:
				{
					response.ContentEncoding = Encoding.UTF8;
					response.Output.Write(info.Compacted);
					break;
				}
			}
		}

		/// <summary>
		/// If supported, adds a runtime compression filter to the response output.
		/// </summary>
		/// <param name="context"></param>
		public static void EnableStreamCompression(HttpContext context)
		{
			// Good request compression summary: http://www.west-wind.com/WebLog/posts/102969.aspx
			switch (ResourceHandler.GetOutputEncoding(context))
			{
				case BuildResultType.Gzip:
				{
					context.Response.AppendHeader(ResourceHandler.HeaderContentEncoding, ResourceHandler.GzipContentEncoding);
					context.Response.Filter = new GZipStream(context.Response.Filter, CompressionMode.Compress, true);
					break;
				}
				case BuildResultType.Deflate:
				{
					context.Response.AppendHeader(ResourceHandler.HeaderContentEncoding, ResourceHandler.DeflateContentEncoding);
					context.Response.Filter = new DeflateStream(context.Response.Filter, CompressionMode.Compress, true);
					break;
				}
			}
		}

		/// <summary>
		/// If supported, removes a runtime compression filter from the response output.
		/// </summary>
		/// <param name="context"></param>
		public static void DisableStreamCompression(HttpContext context)
		{
			switch (ResourceHandler.GetOutputEncoding(context))
			{
				case BuildResultType.Gzip:
				{
#if !__MonoCS__
// remove for Mono Framework
					try
					{
						if (ResourceHandler.GzipContentEncoding.Equals(context.Response.Headers[ResourceHandler.HeaderContentEncoding], StringComparison.OrdinalIgnoreCase))
						{
							context.Response.Headers.Remove(ResourceHandler.HeaderContentEncoding);
						}
					}
					catch (PlatformNotSupportedException) { }
#endif

					if (context.Response.Filter is GZipStream)
					{
						context.Response.Filter = null;
					}
					break;
				}
				case BuildResultType.Deflate:
				{
#if !__MonoCS__
// remove for Mono Framework
					try
					{
						if (ResourceHandler.DeflateContentEncoding.Equals(context.Response.Headers[ResourceHandler.HeaderContentEncoding], StringComparison.OrdinalIgnoreCase))
						{
							context.Response.Headers.Remove(ResourceHandler.HeaderContentEncoding);
						}
					}
					catch (PlatformNotSupportedException) { }
#endif

					if (context.Response.Filter is DeflateStream)
					{
						context.Response.Filter = null;
					}
					break;
				}
			}
		}

		/// <summary>
		/// Strongly typed build result factory method
		/// </summary>
		/// <param name="virtualPath">app-relative virtual path</param>
		/// <returns>strongly typed compiled object</returns>
		public static T Create<T>(string virtualPath)
		{
			if (virtualPath.StartsWith("/"))
			{
				virtualPath = "~"+virtualPath;
			}

			return (T)BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(T));
		}

		/// <summary>
		/// Determines appropriate content-encoding.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private static BuildResultType GetOutputEncoding(HttpContext context)
		{
			if (context.IsDebuggingEnabled)
			{
				return BuildResultType.PrettyPrint;
			}

			string acceptEncoding = context.Request.Headers[ResourceHandler.HeaderAcceptEncoding];
			if (String.IsNullOrEmpty(acceptEncoding))
			{
				return BuildResultType.Compact;
			}

			acceptEncoding = acceptEncoding.ToLowerInvariant();

			if (acceptEncoding.Contains(ResourceHandler.DeflateContentEncoding))
			{
				return BuildResultType.Deflate;
			}

			if (acceptEncoding.Contains(ResourceHandler.GzipContentEncoding))
			{
				return BuildResultType.Gzip;
			}

			return BuildResultType.Compact;
		}

		/// <summary>
		/// Determines the most compact Content-Encoding supported by request.
		/// </summary>
		/// <param name="acceptEncoding"></param>
		/// <param name="isDebug"></param>
		/// <returns>optimal format</returns>
		private static BuildResultType GetOutputEncoding(IOptimizedResult result, HttpContext context, bool isDebug)
		{
			if (isDebug)
			{
				// short cut all debug builds
				return BuildResultType.PrettyPrint;
			}

			string acceptEncoding = context.Request.Headers[ResourceHandler.HeaderAcceptEncoding];
			if (String.IsNullOrEmpty(acceptEncoding))
			{
				// not compressed but fully compacted
				return BuildResultType.Compact;
			}

			acceptEncoding = acceptEncoding.ToLowerInvariant();

			if (result.Deflated != null &&
				result.Deflated.Length > 0 &&
				acceptEncoding.Contains(ResourceHandler.DeflateContentEncoding))
			{
				// compressed with Deflate
				return BuildResultType.Deflate;
			}

			if (result.Gzipped != null &&
				result.Gzipped.Length > 0 &&
				acceptEncoding.Contains(ResourceHandler.GzipContentEncoding))
			{
				// compressed with Gzip
				return BuildResultType.Gzip;
			}

			// not compressed but fully compacted
			return BuildResultType.Compact;
		}

		public static string EnsureAppRelative(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return path;
			}

			// ensure app-relative BuildManager paths
			string appRoot = HostingEnvironment.ApplicationVirtualPath;
			if (appRoot != null && appRoot.Length > 1 &&
				path.StartsWith(appRoot, StringComparison.OrdinalIgnoreCase))
			{
				path = "~"+path.Substring(appRoot.Length);
			}
			else if (path.StartsWith("/") ||
				path.StartsWith("\\"))
			{
				path = "~"+path;
			}
			return path;
		}

		public static string EnsureAppAbsolute(string path)
		{
			// TODO: improve efficiency
			path = EnsureAppRelative(path).TrimStart('~');

			return HostingEnvironment.ApplicationVirtualPath+path;
		}

		#endregion Utility Methods
	}
}