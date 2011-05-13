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
using System.Collections.Specialized;

using EcmaScript.NET;
using JsonFx.BuildTools;

namespace JsonFx.Compilation
{
    internal class BuildErrorReporter : ErrorReporter
	{
		#region Fields

		private readonly string sourceName;
		private readonly IList<ParseException> errors;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public BuildErrorReporter(string sourceName)
			: this(sourceName, new List<ParseException>())
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <remarks></remarks>
		public BuildErrorReporter(string sourceName, IList<ParseException> errors)
		{
			this.sourceName = sourceName;
			this.errors = errors;
		}

		#endregion Init

		#region Properties

		public IList<ParseException> Errors
		{
			get { return this.errors; }
		}

		#endregion Properties

		#region ErrorReporter Members

		public virtual void Warning(
			string message,
            string sourceName,
            int line,
            string lineSource,
            int column)
        {
			this.errors.Add(new ParseWarning(message, sourceName??this.sourceName, line, column));
        }

        public virtual void Error(
			string message,
            string sourceName,
            int line,
            string lineSource,
            int column)
        {
			this.errors.Add(new ParseError(message, sourceName??this.sourceName, line, column));
        }

        public virtual EcmaScriptRuntimeException RuntimeError(
			string message,
            string sourceName, 
            int line,
            string lineSource, 
            int column)
        {
			return new EcmaScriptRuntimeException(message, sourceName, line, lineSource, column);
		}

		#endregion ErrorReporter Members
	}
}