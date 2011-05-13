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
using System.Collections.Generic;

using JsonFx.Json;

namespace JsonFx.JsonRpc
{
	#region Error Codes

	/// <summary>
	/// The error-codes -32768 .. -32000 (inclusive) are reserved for pre-defined errors.
	/// Any error-code within this range not defined explicitly below is reserved for future use.
	/// </summary>
	public enum JsonRpcErrors : int
	{
		Unknown = 0x0,

		/// <summary>
		/// Invalid JSON. An error occurred on the server while parsing the JSON text.
		/// </summary>
		ParseError = -32700,

		/// <summary>
		/// The received JSON not a valid JSON-RPC Request.
		/// </summary>
		InvalidRequest = -32600,

		/// <summary>
		/// The requested remote-procedure does not exist / is not available.
		/// </summary>
		MethodNotFound = -32601,

		/// <summary>
		/// Invalid method parameters.
		/// </summary>
		InvalidParams = -32602,

		/// <summary>
		/// Internal JSON-RPC error.
		/// </summary>
		InternalError = -32603,

		/// <summary>
		/// Reserved for implementation-defined server-errors.
		/// </summary>
		ServerErrorStart = -32099,
		ServerErrorEnd = -32000
	}

	#endregion Error Codes

	public class JsonError
	{
		#region Fields

		private int code = 0x0;
		private string message = null;
		private object data = null;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor.
		/// </summary>
		public JsonError()
			: this(null, JsonRpcErrors.Unknown)
		{
		}

		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="ex"></param>
		public JsonError(Exception ex)
			: this(ex, JsonRpcErrors.Unknown)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="code"></param>
		public JsonError(Exception ex, JsonRpcErrors code)
		{
			if (ex != null)
			{
				this.Message = ex.Message;
				IDictionary<String, Object> data = this.DataDictionary;
				data["Type"] = ex.GetType().Name;
#if DEBUG
				bool showStackTrace = true;
#else
				bool showStackTrace = false;
				HttpContext context = HttpContext.Current;
				if (context != null)
				{
					showStackTrace = context.IsDebuggingEnabled;
				}
#endif
				if (showStackTrace)
				{
					data["StackTrace"] = ex.StackTrace;
				}
			}

			this.code = (int)code;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets a Number value that indicates the actual error that occurred.
		/// </summary>
		/// <remarks>
		/// A Number that indicates the actual error that occurred. This MUST be an integer.
		/// </remarks>
		[JsonName("code")]
		public int Code
		{
			get { return this.code; }
			set { this.code = value; }
		}

		/// <summary>
		/// Gets and sets a short description of the error.
		/// </summary>
		/// <remarks>
		/// A String providing a short description of the error.
		/// The message SHOULD be limited to a concise single sentence.
		/// </remarks>
		[JsonName("message")]
		public string Message
		{
			get { return this.message; }
			set { this.message = value; }
		}

		/// <summary>
		/// Gets and sets data about the error.
		/// </summary>
		/// <remarks>
		/// Additional information, may be omitted. Its contents is entirely defined by
		/// the application (e.g. detailed error information, nested errors etc.).
		/// </remarks>
		[JsonName("data")]
		public object Data
		{
			get { return this.data; }
			set { this.data = value; }
		}

		/// <summary>
		/// Convenience property which gets the Data object as a typed dictionary.
		/// </summary>
		[JsonIgnore]
		public IDictionary<String, Object> DataDictionary
		{
			get
			{
				if (this.data == null)
				{
					this.data = new Dictionary<String, Object>();
				}

				// this will throw an exception if data is assigned some other value
				return (IDictionary<String, Object>)this.data;
			}
		}

		#endregion Properties
	}
}
