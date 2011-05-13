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
using System.Net;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

using JsonFx.Handlers;

namespace JbstOnline.Mvc.ActionResults
{
	/// <summary>
	/// ActionResult returning a build-time resource
	/// </summary>
	public class ResourceResult : ActionResult
	{
		#region Fields

		private readonly string ResourcePath;
		private readonly IOptimizedResult Resource;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="path"></param>
		public ResourceResult(string resourcePath)
		{
			this.ResourcePath = resourcePath;
			this.Resource = ResourceHandler.Create<IOptimizedResult>(resourcePath);
			this.IsDebug = HttpContext.Current.IsDebuggingEnabled;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the filename of the resource
		/// </summary>
		public string Filename
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets if result is being downloaded
		/// </summary>
		public bool IsAttachment
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets if result should be compacted
		/// </summary>
		public bool IsDebug
		{
			get;
			set;
		}

		#endregion Properties

		#region Methods

		public override void ExecuteResult(ControllerContext context)
		{
			HttpResponseBase response = context.HttpContext.Response;

			try
			{
				response.ClearHeaders();
				response.ClearContent();
			}
			catch { }

			response.TrySkipIisCustomErrors = true;

			if (this.Resource == null)
			{
				response.ContentType = "text/plain";
				response.StatusCode = (int)HttpStatusCode.NotFound;
				response.Write(response.Status);
				return;
			}

			HttpContext httpContext = HttpContext.Current;

			// check if client has cached copy
			ETag etag = new HashETag(this.Resource.Hash);
			if (etag.HandleETag(httpContext, HttpCacheability.ServerAndPrivate, this.IsDebug))
			{
				return;
			}

			if (String.IsNullOrEmpty(this.Resource.ContentType))
			{
				response.ContentType = "text/plain";
			}
			else
			{
				response.ContentType = this.Resource.ContentType;
			}

			// this helps IE determine the Content-Type
			// http://tools.ietf.org/html/rfc2183#section-2.3
			ContentDisposition disposition = new ContentDisposition
			{
				Inline = !this.IsAttachment,
				FileName =
					String.IsNullOrEmpty(this.Filename) ?
					Path.GetFileNameWithoutExtension(this.ResourcePath)+'.'+this.Resource.FileExtension :
					this.Filename
			};
			response.AddHeader("Content-Disposition", disposition.ToString());

			ResourceHandler.WriteResponse(httpContext, this.Resource, this.IsDebug);
		}

		#endregion Methods
	}
}
