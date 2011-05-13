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
using System.Collections.Generic;

using JsonFx.Json;

namespace JsonFx.JsonRpc
{
	/// <summary>
	/// The message that frames a call and its parameters.
	/// </summary>
	public class JsonRequest : JsonMessage
	{
		#region Fields

		private string method = null;
		private Object parameters = null;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets the named operation on a service that is the target of this remote procedure call.
		/// </summary>
		/// <remarks>
		/// A String containing the name of the procedure to be invoked.
		/// 
		/// Procedure names that begin with the word system followed by a period character (U+002E or ASCII 46)
		/// are reserved for system description / introspection.
		/// </remarks>
		[JsonName("method")]
		public virtual string Method
		{
			get { return this.method; }
			set { this.method = value; }
		}

		/// <summary>
		/// Gets and sets the actual parameter values for the invocation of the procedure.
		/// </summary>
		/// <remarks>
		/// An Array or Object, that holds the actual parameter values for the invocation of
		/// the procedure. Can be omitted if empty.
		/// </remarks>
		[JsonName("params")]
		public virtual Object Params
		{
			get { return this.parameters; }
			set { this.parameters = value; }
		}

		/// <summary>
		/// Gets and sets the named parameter values for this remote procedure call.
		/// Mutually exclusive with <see cref="PositionalParams">PositionalParams</see>.
		/// </summary>
		/// <remarks>
		/// MUST be an Object, containing the parameter-names and its values.
		/// The names MUST match exactly (including case) the names defined by the formal arguments.
		/// The order of the name/value-pairs is insignificant.
		/// </remarks>
		[JsonIgnore]
		public virtual Dictionary<String, Object> NamedParams
		{
			get { return this.Params as Dictionary<String, Object>; }
			set { this.Params = value; }
		}

		/// <summary>
		/// Gets and sets the positional parameter values for this remote procedure call.
		/// Mutually exclusive with <see cref="NamedParams">NamedParams</see>.
		/// </summary>
		/// <remarks>
		/// MUST be an Array, containing the parameters in the right order (like in JSON-RPC 1.0).
		/// </remarks>
		[JsonIgnore]
		public virtual Array PositionalParams
		{
			get { return this.Params as Array; }
			set { this.Params = value; }
		}

		/// <summary>
		/// Gets if this request is a notification.
		/// </summary>
		/// <remarks>
		/// A Notification is a special Request, without id and without Response.
		/// The server MUST NOT reply to a Notification.
		/// 
		/// Note that Notifications are unreliable by definition, since they do not have a Response,
		/// and so you cannot detect errors (like e.g. "Invalid params.", "Internal error.",
		/// timeouts or maybe even lost packets on the wire).
		/// </remarks>
		[JsonIgnore]
		public virtual bool IsNotification
		{
			get { return this.ID == null; }
		}

		#endregion Properties
	}
}
