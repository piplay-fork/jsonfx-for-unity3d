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
using System.Collections.Generic;

using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Internal representation of a JBST control reference
	/// </summary>
	internal class JbstControlReference : JbstCommandBase
	{
		#region Constants

		public const string ControlCommand = "control";

		private const string SimpleReferenceFormat =
			@"function() {{
				return JsonML.BST({0}).dataBind({1}, {2}, {3});
			}}";

		private const string ControlWrapperStartFormat =
			@"function() {{
				return JsonML.BST({0}).dataBind({1}, {2}, {3}, ";

		private const string ControlWrapperEnd =
			@");
			}";

		private const string InlineTemplateStart =
			@"function() {
				return JsonML.BST(";

		private const string InlineTemplateEndFormat =
			@").dataBind({0}, {1}, {2});
			}}";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public JbstControlReference()
			: base(JbstControlReference.ControlCommand)
		{
		}

		#endregion Init

		#region Render Methods

		/// <summary>
		/// Controls the control rendering style
		/// </summary>
		/// <param name="writer"></param>
		protected override void Render(JsonWriter writer)
		{
			if (!this.ChildControlsSpecified)
			{
				if (String.IsNullOrEmpty(this.NameExpr))
				{
					throw new InvalidOperationException("jbst:Control requires either a named template or anonymous inline template.");
				}

				// name without child controls
				this.RenderSimpleControlReference(writer);
			}
			else if (String.IsNullOrEmpty(this.NameExpr))
			{
				// anonymous with child controls
				this.RenderAnonymousTemplate(writer);
			}
			else
			{
				// both name and child controls
				this.RenderWrapperControlReference(writer);
			}
		}

		/// <summary>
		/// Renders a simple data binding call to a named template.
		/// </summary>
		/// <param name="writer"></param>
		private void RenderSimpleControlReference(JsonWriter writer)
		{
			writer.TextWriter.Write(
				JbstControlReference.SimpleReferenceFormat,
				this.NameExpr,
				this.DataExpr,
				this.IndexExpr,
				this.CountExpr);
		}

		/// <summary>
		/// Renders a data binding call to an inline anonymous template.
		/// </summary>
		/// <param name="writer"></param>
		private void RenderAnonymousTemplate(JsonWriter writer)
		{
			writer.TextWriter.Write(JbstControlReference.InlineTemplateStart);

			writer.Write(new EnumerableAdapter(this));

			writer.TextWriter.Write(
				JbstControlReference.InlineTemplateEndFormat,
				this.DataExpr,
				this.IndexExpr,
				this.CountExpr);
		}

		/// <summary>
		/// Renders a data binding call to a named template with a nested inline anonymous placeholder template.
		/// </summary>
		/// <param name="writer"></param>
		private void RenderWrapperControlReference(JsonWriter writer)
		{
			writer.TextWriter.Write(
				JbstControlReference.ControlWrapperStartFormat,
				this.NameExpr,
				this.DataExpr,
				this.IndexExpr,
				this.CountExpr);

			Dictionary<string, object> args = new Dictionary<string, object>();

			if (this.ChildControls.HasAnonymousInlineTemplate)
			{
				// anonymous inline template
				args[JbstInline.InlinePrefix] = new EnumerableAdapter(this);
			}

			if (this.ChildControls.InlineTemplatesSpecified)
			{
				// named inline templates
				foreach (JbstInline inline in this.ChildControls.InlineTemplates)
				{
					args[JbstInline.InlinePrefix+inline.NameExpr] = new EnumerableAdapter(inline);
				}
			}

			writer.Write(args);

			writer.TextWriter.Write(JbstControlReference.ControlWrapperEnd);
		}

		#endregion Render Methods
	}
}
