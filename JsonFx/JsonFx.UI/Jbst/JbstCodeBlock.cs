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
using System.Text;

using JsonFx.Json;
using JsonFx.UI.Jbst.Extensions;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Internal representation of a JBST code block.
	/// </summary>
	internal abstract class JbstCodeBlock : JbstControl, IJsonSerializable
	{
		#region Constants

		protected const string Noop = "null";

		#endregion Constants

		#region Fields

		private readonly string code;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="type"></param>
		/// <param name="code"></param>
		/// <param name="path"></param>
		protected JbstCodeBlock(string code)
		{
			this.code = (code == null) ? String.Empty : code;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the code block content
		/// </summary>
		public virtual string Code
		{
			get { return this.code; }
		}

		#endregion Properties

		#region Methods

		protected abstract string GetCodeBlock();

		#endregion Methods

		#region IJsonSerializable Members

		void JsonFx.Json.IJsonSerializable.WriteJson(JsonWriter writer)
		{
			string codeBlock = this.GetCodeBlock();

			if (String.IsNullOrEmpty(codeBlock))
			{
				codeBlock = JbstCodeBlock.Noop; 
			}

			writer.TextWriter.Write(codeBlock);
		}

		void JsonFx.Json.IJsonSerializable.ReadJson(JsonFx.Json.JsonReader reader)
		{
			throw new NotImplementedException("JbstCodeBlock deserialization is not implemented.");
		}

		#endregion IJsonSerializable Members
	}

	internal class JbstCommentBlock : JbstCodeBlock
	{
		#region Constants

		private const string CommentFormat = "\"\"/* {0} */";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="code"></param>
		/// <param name="path"></param>
		public JbstCommentBlock(string code)
			: base(code)
		{
		}

		#endregion Init

		#region JbstCodeBlock Members

		protected override string GetCodeBlock()
		{
			string code = this.Code.Trim();
			if (String.IsNullOrEmpty(code))
			{
				return JbstCodeBlock.Noop;
			}

			return String.Format(CommentFormat, code.Replace("*/", "* /"));
		}

		#endregion JbstCodeBlock Members
	}

	internal class JbstDeclarationBlock : JbstCodeBlock
	{
		#region Constants

		private const string DeclarationFormat =
@"// initialize template in the context of ""this""
(function() {{
	{1}
}}).call({0});";

		#endregion Constants

		#region Fields

		private readonly StringBuilder Content = new StringBuilder();
		private string ownerName;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="code"></param>
		public JbstDeclarationBlock()
			: base(String.Empty)
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the declaration code block content
		/// </summary>
		public override string Code
		{
			get { return this.Content.ToString(); }
		}

		/// <summary>
		/// Gets an indication if any code has been appended
		/// </summary>
		public bool HasCode
		{
			get { return this.Content.Length > 0; }
		}

		/// <summary>
		/// Gets the name of the owning JBST
		/// </summary>
		public string OwnerName
		{
			get
			{
				if (String.IsNullOrEmpty(this.ownerName))
				{
					return "this";
				}
				return this.ownerName;
			}
			set { this.ownerName = value; }
		}

		#endregion Properties

		#region JbstCodeBlock Members

		protected override string GetCodeBlock()
		{
			string code = this.Code.Trim();
			if (String.IsNullOrEmpty(code))
			{
				return JbstCodeBlock.Noop;
			}

			// output expressions are the core of the syntax
			return String.Format(DeclarationFormat, this.OwnerName, code);
		}

		#endregion JbstCodeBlock Members

		#region Methods

		/// <summary>
		/// Append another code block onto declaration block
		/// </summary>
		/// <param name="control"></param>
		public void Append(JbstControl control)
		{
			if (control is JbstCodeBlock)
			{
				this.Content.Append( ((JbstCodeBlock)control).Code );
			}
			else if (control is JbstLiteral)
			{
				this.Content.Append( ((JbstLiteral)control).Text );
			}
			else if (control is JbstContainerControl)
			{
				JbstContainerControl parent = (JbstContainerControl)control;

				while (parent.ChildControlsSpecified)
				{
					// this will remove it from the collection
					this.Append(parent.ChildControls[0]);
				}
			}

			// remove from the parse tree since is pre-aggregated
			control.Parent.ChildControls.Remove(control);
		}

		/// <summary>
		/// Append another code block onto declaration block
		/// </summary>
		/// <param name="control"></param>
		public void Append(string code)
		{
			this.Content.Append(code);
		}

		#endregion Methods
	}

	internal class JbstExpressionBlock : JbstCodeBlock
	{
		#region Constants

		private const string ExpressionFormat =
@"function() {{
	return {0};
}}";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="code"></param>
		public JbstExpressionBlock(string code)
			: base(code)
		{
		}

		#endregion Init

		#region JbstCodeBlock Members

		protected override string GetCodeBlock()
		{
			string code = this.Code.Trim();
			if (String.IsNullOrEmpty(code))
			{
				return JbstCodeBlock.Noop;
			}

			// output expressions are the core of the syntax
			return String.Format(ExpressionFormat, code);
		}

		#endregion JbstCodeBlock Members
	}

	internal class JbstUnparsedBlock : JbstCodeBlock
	{
		#region Constants

		private const string UnparsedFormat =
@"function() {{
	return JsonML.raw({0});
}}";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="code"></param>
		public JbstUnparsedBlock(string code)
			: base(code)
		{
		}

		#endregion Init

		#region JbstCodeBlock Members

		protected override string GetCodeBlock()
		{
			string code = this.Code;
			if (String.IsNullOrEmpty(code))
			{
				return JbstCodeBlock.Noop;
			}

			return String.Format(UnparsedFormat, code);
		}

		#endregion JbstCodeBlock Members
	}

	internal class JbstStatementBlock : JbstCodeBlock
	{
		#region Constants

		private const string StatementFormat =
			@"function() {{
				{0}
			}}";

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="code"></param>
		/// <param name="path"></param>
		public JbstStatementBlock(string code)
			: base(code)
		{
		}

		#endregion Init

		#region JbstCodeBlock Members

		protected override string GetCodeBlock()
		{
			string code = this.Code.Trim();
			if (String.IsNullOrEmpty(code))
			{
				return JbstCodeBlock.Noop;
			}

			// analogous to instance code, or JSP scriptlets
			// executed each time template is bound
			return String.Format(StatementFormat, code);
		}

		#endregion JbstCodeBlock Members
	}

	internal class JbstExtensionBlock : JbstCodeBlock
	{
		#region Constants

		private static readonly char[] PrefixDelim = { ':' };

		#endregion Constants

		#region Fields

		private readonly JbstExtension extension = null;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="code"></param>
		/// <param name="path"></param>
		public JbstExtensionBlock(string code, string path)
			: base(code)
		{
			KeyValuePair<string, string> expr = JbstExtensionBlock.ParseExpression(code);

			// TODO: lookup the corresponding extension evaluator from web.config
			switch (expr.Key)
			{
				case "AppSettings":
				{
					this.extension = new AppSettingsJbstExtension(expr.Value, path);
					break;
				}
				case "Resources":
				{
					this.extension = new ResourceJbstExtension(expr.Value, path);
					break;
				}
				default:
				{
					this.extension = new JbstExtension(code, path);
					break;
				}
			}
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the extension represented by this block
		/// </summary>
		public JbstExtension Extension
		{
			get { return this.extension; }
		}

		#endregion Properties

		#region JbstCodeBlock Members

		protected override string GetCodeBlock()
		{
			// execute the corresponding extension evaluator
			return this.Extension.Eval();
		}

		#endregion JbstCodeBlock Members

		#region Utility Methods

		private static KeyValuePair<string, string> ParseExpression(string extension)
		{
			string key = String.Empty;
			string value = String.Empty;

			if (!String.IsNullOrEmpty(extension))
			{
				// split on first ':'
				string[] parts = extension.Split(JbstExtensionBlock.PrefixDelim, 2, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length > 1)
				{
					value = parts[1].Trim();
				}

				key = parts[0].Trim();
			}

			return new KeyValuePair<string, string>(key, value);
		}

		#endregion Utility Methods
	}
}
