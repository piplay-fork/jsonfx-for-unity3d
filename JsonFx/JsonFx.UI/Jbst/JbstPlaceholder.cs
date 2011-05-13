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
	/// Internal representation of a JBST placholder
	/// </summary>
	internal class JbstPlaceholder : JbstCommandBase
	{
		#region Constants

		public const string PlaceholderCommand = "placeholder";

		private const string PlaceholderStatementStart =
			@"function() {
				var inline = ";

		private const string PlaceholderStatementEndFormat =
			@",
					parts = this.args;

				if (parts && parts[inline]) {{
					return JsonML.BST(parts[inline]).dataBind({0}, {1}, {2}, parts);
				}}
			}}";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public JbstPlaceholder()
			: base(JbstPlaceholder.PlaceholderCommand)
		{
		}

		#endregion Init

		#region Render Methods

		protected override void Render(JsonWriter writer)
		{
			writer.TextWriter.Write(JbstPlaceholder.PlaceholderStatementStart);

			writer.Write(JbstInline.InlinePrefix+this.NameExpr);

			writer.TextWriter.Write(
				JbstPlaceholder.PlaceholderStatementEndFormat,
				this.DataExpr,
				this.IndexExpr,
				this.CountExpr);
		}

		#endregion Render Methods
	}
}
