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

using JsonFx.Json;

namespace JsonFx.JsonRpc
{
	public abstract class JsonMessage
	{
		#region Constants

		public const string JsonRpcVersion = "2.0";

		#endregion Constants

		#region Fields

		private object id = null;
		private string version = JsonMessage.JsonRpcVersion;

		#endregion Constants

		#region Properties

		/// <summary>
		/// Gets and sets an identifier which may be used to correlate a response with its request.
		/// </summary>
		/// <remarks>
		/// A Request identifier that SHOULD be a JSON scalar (String, Number, True, False),
		/// but SHOULD normally not be Null. If omitted, the Request is a Notification.
		/// 
		/// This id can be used to correlate a Response with its Request. The server MUST
		/// repeat it verbatim on the Response.
		/// </remarks>
		[JsonName("id")]
		public virtual object ID
		{
			get { return this.id; }
			set { this.id = value; }
		}

		/// <summary>
		/// Gets and sets the version of the JSON-RPC specification to which this conforms.
		/// </summary>
		/// <remarks>
		/// A String specifying the version of the JSON-RPC protocol. MUST be exactly "2.0". 
		/// 
		/// If jsonrpc is missing, the server MAY handle the Request as JSON-RPC V1.0-Request.
		/// </remarks>
		[JsonName("jsonrpc")]
		public virtual string Version
		{
			get { return this.version; }
			set { this.version = value; }
		}

		/// <summary>
		/// Gets if the version is missing indicating this is a v1.0 message.
		/// </summary>
		[JsonIgnore]
		public virtual bool IsJsonRpc10
		{
			get { return String.IsNullOrEmpty(this.version); }
		}

		#endregion Properties
	}
}
