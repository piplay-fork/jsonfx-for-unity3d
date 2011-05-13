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
	/// <summary>
	/// The message that frames the result of a successful call or the error in the case of a failed call.
	/// </summary>
	public class JsonResponse : JsonMessage
	{
		#region Fields

		private object result = null;
		private JsonError error = null;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets the answer for a successful call.
		/// </summary>
		/// <remarks>
		/// Required on success, omitted on failure.
		/// The Value that was returned by the procedure. Its contents is entirely defined by the procedure.
		/// This member MUST be entirely omitted if there was an error invoking the procedure.
		/// 
		/// Exactly one of result or error MUST be specified. It's not allowed to specify both or none.
		/// </remarks>
		[JsonName("result")]
		[JsonSpecifiedProperty("ResultSpecified")]
		public virtual object Result
		{
			get { return this.result; }
			set { this.result = value; }
		}

		[JsonIgnore]
		public virtual bool ResultSpecified
		{
			get { return (this.error == null); }
		}

		/// <summary>
		/// Gets and sets the answer for a failed call.
		/// </summary>
		/// <remarks>
		/// Required on error, omitted on success.
		/// An Object containing error information about the fault that occurred before, during or after the call.
		/// This member MUST be entirely omitted if there was no such fault.
		/// 
		/// Exactly one of result or error MUST be specified. It's not allowed to specify both or none.
		/// </remarks>
		[JsonName("error")]
		[JsonSpecifiedProperty("ErrorSpecified")]
		public virtual JsonError Error
		{
			get { return this.error; }
			set { this.error = value; }
		}

		[JsonIgnore]
		public virtual bool ErrorSpecified
		{
			get { return (this.error != null); }
		}

		#endregion Properties
	}
}
