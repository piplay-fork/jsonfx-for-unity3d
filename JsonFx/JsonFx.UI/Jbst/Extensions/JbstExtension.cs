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

using JsonFx.Handlers;

namespace JsonFx.UI.Jbst.Extensions
{
	/// <summary>
	/// Base class for extending JBST with custom declarations
	/// </summary>
	public class JbstExtension
	{
		#region Constants

		private const string PrefixNotFoundError =
			@"alert(""Error in {0}:\nUnknown extension \""{1}\"""")";

		#endregion Constants

		#region Fields

		private readonly string value;
		private readonly string path;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="value"></param>
		/// <param name="virtualPath"></param>
		protected internal JbstExtension(string value, string path)
		{
			this.value = value;
			this.path = path;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the extension content
		/// </summary>
		protected string Value
		{
			get { return this.value; }
		}

		/// <summary>
		/// Gets the virtual path
		/// </summary>
		protected string Path
		{
			get { return this.path; }
		}

		#endregion Properties

		#region JbstExtension Members

		protected internal virtual string Eval()
		{
			// output error
			string path = ResourceHandler.EnsureAppRelative(this.Path);
			return String.Format(JbstExtension.PrefixNotFoundError, path, this.Value);
		}

		#endregion JbstExtension Members
	}
}
