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
using System.Web;
using System.Web.Compilation;

using JsonFx.JsonRpc;

namespace JsonFx.Handlers
{
	public class JsonServiceHandlerFactory : System.Web.IHttpHandlerFactory
	{
		#region IHttpHandlerFactory Methods

		public virtual IHttpHandler GetHandler(HttpContext context, string verb, string url, string path)
		{
			if ("GET".Equals(verb, StringComparison.OrdinalIgnoreCase) &&
				String.IsNullOrEmpty(context.Request.PathInfo))
			{
				// output service javascript proxy
				return new ResourceHandler(context);
			}

			// handle service requests
			string appUrl = context.Request.AppRelativeCurrentExecutionFilePath;
			IJsonServiceInfo serviceInfo = ResourceHandler.Create<IJsonServiceInfo>(appUrl);

			if (!context.IsDebuggingEnabled && !Settings.DisableStreamCompression)
			{
				ResourceHandler.EnableStreamCompression(context);
			}

			return new JsonServiceHandler(serviceInfo, url);
		}

		public virtual void ReleaseHandler(IHttpHandler handler)
		{
		}

		#endregion IHttpHandlerFactory Methods
	}
}
