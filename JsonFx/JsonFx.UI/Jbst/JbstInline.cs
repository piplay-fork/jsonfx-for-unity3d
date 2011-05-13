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

using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Internal representation of named JBST placholder inline content
	/// </summary>
	internal class JbstInline : JbstCommandBase
	{
		#region Constants

		public const string InlineCommand = "inline";
		public const string InlinePrefix = "$";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public JbstInline()
			: base(JbstInline.InlineCommand)
		{
		}

		#endregion Init

		#region Properties

		public override JbstContainerControl Parent
		{
			get { return base.Parent; }
			set
			{
				if (value == null ||
					value is JbstControlReference)
				{
					base.Parent = value;
				}
				else
				{
					throw new InvalidOperationException("jbst:inline may only be a direct child of jbst:control");
				}
			}
		}

		public bool IsAnonymous
		{
			get { return String.IsNullOrEmpty(this.NameExpr); }
		}

		#endregion Properties

		#region Render Methods

		protected override void Render(JsonWriter writer)
		{
			writer.Write(new EnumerableAdapter(this));
		}

		#endregion Render Methods
	}
}
