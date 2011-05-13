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
using System.Web;
using System.Web.Compilation;

namespace JsonFx.Handlers
{
	/// <summary>
	/// Allows forcing the resource to pretty-print with "?debug"
	/// </summary>
	public class DebugResourceHandlerFactory : ResourceHandlerFactory
	{
		#region Constants

		internal const string DebugFlag = "debug";

		#endregion Constants

		#region Methods

		protected override bool IsDebuggingEnabled(HttpContext context, string cacheKey)
		{
			if (String.IsNullOrEmpty(cacheKey))
			{
				return context.IsDebuggingEnabled;
			}

			return StringComparer.OrdinalIgnoreCase.Equals(DebugResourceHandlerFactory.DebugFlag, cacheKey);
		}

		#endregion Methods
	}

	/// <summary>
	/// ResourceHandler Factory
	/// </summary>
	public class ResourceHandlerFactory : IHttpHandlerFactory
	{
		#region IHttpHandlerFactory Methods

		public virtual IHttpHandler GetHandler(HttpContext context, string verb, string url, string path)
		{
			string cacheKey = context.Request.QueryString[null];
			bool isDebug = this.IsDebuggingEnabled(context, cacheKey);

			if (context.Request.QueryString[ResourceHandler.GlobalizationQuery] != null)
			{
				// output resource strings used by the handler
				return new GlobalizedResourceHandler(isDebug, cacheKey);
			}

			// output resource content
			return new ResourceHandler(isDebug, cacheKey);
		}

		public virtual void ReleaseHandler(IHttpHandler handler)
		{
		}

		#endregion IHttpHandlerFactory Methods

		#region Methods

		protected virtual bool IsDebuggingEnabled(HttpContext context, string cacheKey)
		{
			return context.IsDebuggingEnabled;
		}

		#endregion Methods
	}
}
