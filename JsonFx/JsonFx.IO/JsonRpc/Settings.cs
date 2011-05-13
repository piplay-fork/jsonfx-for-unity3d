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
using System.IO;
using JsonFx.Json;

namespace JsonFx.JsonRpc
{
	public class RequestEventArgs : EventArgs
	{
		#region Fields

		private readonly HttpContext context;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="context"></param>
		internal RequestEventArgs(HttpContext context)
		{
			this.context = context;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the HttpContext for this request.
		/// </summary>
		public HttpContext Context
		{
			get { return this.context; }
		}

		#endregion Properties
	}

	public class JrpcEventArgs : RequestEventArgs
	{
		#region Fields

		private readonly JsonRequest request;
		private readonly JsonResponse response;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="context"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		internal JrpcEventArgs(HttpContext context, JsonRequest request, JsonResponse response)
			: base(context)
		{
			this.request = request;
			this.response = response;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the JSON-RPC Request message.
		/// </summary>
		public JsonRequest Request
		{
			get { return this.request; }
		}

		/// <summary>
		/// Gets the JSON-RPC Response message.
		/// </summary>
		/// <remarks>
		/// This can be manipulated to produce an entirely different response.
		/// </remarks>
		public JsonResponse Response
		{
			get { return this.response; }
		}

		#endregion Properties
	}

	public class JrpcErrorEventArgs : JrpcEventArgs
	{
		#region Fields

		private readonly Exception exception;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="context"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="exception"></param>
		internal JrpcErrorEventArgs(HttpContext context, JsonRequest request, JsonResponse response, Exception exception)
			: base(context, request, response)
		{
			this.exception = exception;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the exception that raised the error.
		/// </summary>
		public Exception Exception
		{
			get { return this.exception; }
		}

		#endregion Properties
	}

	public delegate void SerializeJsonRpcDelegate(TextWriter output, JsonResponse response);

	public delegate JsonRequest DeserializeJsonRpcDelegate(Stream input);

	public static class Settings
	{
		#region Fields

		private static bool allowGetMethod;
		private static bool disableStreamCompression;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets if GET requests are allowed for JSON-RPC Services.
		/// Default is false (GET HTTP Method forbidden).
		/// </summary>
		/// <remarks>
		/// For better security leave this false, as it is harder to produce
		/// valid POST requests via script injection / cross-site forgery.
		/// </remarks>
		public static bool AllowGetMethod
		{
			get { return Settings.allowGetMethod; }
			set { Settings.allowGetMethod = value; }
		}

		/// <summary>
		/// Gets and sets if non-debug responses are compressed.
		/// Default is false (compression enabled).
		/// </summary>
		public static bool DisableStreamCompression
		{
			get { return Settings.disableStreamCompression; }
			set { Settings.disableStreamCompression = value; }
		}

		public static SerializeJsonRpcDelegate Serialize = SerializeJsonRpc;
		public static DeserializeJsonRpcDelegate Deserialize = DeserializeJsonRpc;

		public static event EventHandler<RequestEventArgs> Init;
		public static event EventHandler<JrpcEventArgs> PreExecute;
		public static event EventHandler<JrpcEventArgs> PostExecute;
		public static event EventHandler<RequestEventArgs> Unload;
		public static event EventHandler<JrpcErrorEventArgs> Error;

		#endregion Properties

		#region Event Methods

		internal static void OnInit(object sender, HttpContext context)
		{
			if (Settings.Init != null)
			{
				Settings.Init(sender, new RequestEventArgs(context));
			}
		}

		internal static void OnPreExecute(object sender, HttpContext context, JsonRequest request, JsonResponse response)
		{
			if (Settings.PreExecute != null)
			{
				Settings.PreExecute(sender, new JrpcEventArgs(context, request, response));
			}
		}

		internal static void OnPostExecute(object sender, HttpContext context, JsonRequest request, JsonResponse response)
		{
			if (Settings.PostExecute != null)
			{
				Settings.PostExecute(sender, new JrpcEventArgs(context, request, response));
			}
		}

		internal static void OnUnload(object sender, HttpContext context)
		{
			if (Settings.Unload != null)
			{
				Settings.Unload(sender, new RequestEventArgs(context));
			}
		}

		internal static void OnError(object sender, HttpContext context, JsonRequest request, JsonResponse response, Exception exception)
		{
			if (Settings.Error != null)
			{
				try
				{
					Settings.Error(sender, new JrpcErrorEventArgs(context, request, response, exception));
				}
				catch
				{
					// don't allow error handler to generate additional errors or messages break down
				}
			}
		}

		#endregion Event Methods

		#region Serialization Methods

		/// <summary>
		/// Parses an incoming JSON-RPC request object
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static JsonRequest DeserializeJsonRpc(Stream input)
		{
			return new JsonReader(input).Deserialize<JsonRequest>();
		}

		/// <summary>
		/// Serializes an outgoing JSON-RPC response object
		/// </summary>
		/// <param name="output"></param>
		/// <param name="response"></param>
		public static void SerializeJsonRpc(TextWriter output, JsonResponse response)
		{
			using (JsonWriter writer = new JsonWriter(output))
			{
				writer.Write(response);
			}
		}

		#endregion Serialization Methods
	}
}
