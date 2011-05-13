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
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace JsonFx.Mvc
{
	/// <summary>
	/// A simple ActionResult for returning a custom HTTP status code
	/// </summary>
	public class HttpResult : ActionResult
	{
		#region Properties

		public virtual HttpStatusCode HttpStatus
		{
			get;
			set;
		}

		public virtual string ContentType
		{
			get;
			set;
		}

		public virtual string Message
		{
			get;
			set;
		}

		#endregion Properties

		#region ActionResult Members

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

			if (this.HttpStatus != default(HttpStatusCode))
			{
				response.StatusCode = (int)this.HttpStatus;
			}

			if (String.IsNullOrEmpty(this.ContentType))
			{
				response.ContentType = "text/plain";
			}
			else
			{
				response.ContentType = this.ContentType;
			}

			this.WriteMessage(response);
		}

		protected virtual void WriteMessage(HttpResponseBase response)
		{
			response.Write(response.Status);

			string message = this.Message;
			if (!String.IsNullOrEmpty(message))
			{
				response.Write(": ");
				response.Write(message);
			}
		}

		#endregion ActionResult Members
	}
}
