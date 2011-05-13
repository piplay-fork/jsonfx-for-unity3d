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
using System.IO;
using System.Text;

using JsonFx.BuildTools.HtmlDistiller;
using JsonFx.BuildTools.HtmlDistiller.Writers;
using JsonFx.Client;
using JsonFx.Compilation;
using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// JBST Writer
	/// </summary>
	public class JbstWriter : IHtmlWriter
	{
		#region Constants

		private static readonly char[] ImportDelim = { ' ', ',' };

		internal const string PrefixDelim = ":";

		private const string ScriptTagName = "script";

		private const string AnonymousPrefix = "$";

		#endregion Constants

		#region Fields

		private readonly string path;
		private readonly StringBuilder Directives = new StringBuilder();
		private readonly JbstDeclarationBlock Declarations = new JbstDeclarationBlock();
		private readonly List<string> Imports = new List<string>();
		private readonly JbstContainerControl document = new JbstContainerControl();

		private JbstContainerControl current;
		private string jbstName;
		private AutoMarkupType autoMarkup;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public JbstWriter()
			: this(String.Empty)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="virtualPath">path used for resolving App_LocalResources</param>
		public JbstWriter(string virtualPath)
		{
			this.path = virtualPath;
		}

		#endregion Init

		#region Properties

		public string JbstName
		{
			get
			{
				if (String.IsNullOrEmpty(this.jbstName))
				{
					this.jbstName = AnonymousPrefix+Guid.NewGuid().ToString("n");
				}
				return this.jbstName;
			}
			set { this.jbstName = value; }
		}

		public AutoMarkupType AutoMarkup
		{
			get { return this.autoMarkup; }
			set { this.autoMarkup = value; }
		}

		/// <summary>
		/// Gets the internal parse tree representation
		/// </summary>
		public object JbstParseTree
		{
			get
			{
				JbstControl control = null;
				foreach (JbstControl child in this.document.ChildControls)
				{
					// tally non-whitespace controls
					JbstLiteral lit = child as JbstLiteral;
					if (lit != null && lit.IsWhitespace)
					{
						continue;
					}

					if (control != null)
					{
						// found 2 or more in root
						// render with full document wrapper
						control = this.document;
						break;
					}

					control = child;
				}
				return control;
			}
		}

		#endregion Properties

		#region Parse Methods

		private void AppendChild(string text)
		{
			if (String.IsNullOrEmpty(text))
			{
				return;
			}

			if (this.current == null)
			{
				this.current = this.document;
			}

			// this allows HTML entities to be encoded as unicode
			text = HtmlDistiller.DecodeHtmlEntities(text);

			JbstLiteral literal = new JbstLiteral(text, true);
			this.current.ChildControls.Add(literal);
		}

		private void AppendChild(JbstControl child)
		{
			if (child == null)
			{
				return;
			}

			if (this.current == null)
			{
				this.current = this.document;
			}

			this.current.ChildControls.Add(child);
		}

		private void PushTag(string rawName)
		{
			string tagName;
			string prefix = JbstWriter.SplitPrefix(rawName, out tagName);

			JbstContainerControl control;
			if (JbstCommandBase.JbstPrefix.Equals(prefix, StringComparison.OrdinalIgnoreCase))
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(JbstControlReference.ControlCommand, tagName))
				{
					control = new JbstControlReference();
				}
				else if (StringComparer.OrdinalIgnoreCase.Equals(JbstPlaceholder.PlaceholderCommand, tagName))
				{
					control = new JbstPlaceholder();
				}
				else if (StringComparer.OrdinalIgnoreCase.Equals(JbstInline.InlineCommand, tagName))
				{
					control = new JbstInline();
				}
				else
				{
					throw new InvalidOperationException("Unknown JBST command ('"+rawName+"')");
				}
			}
			else
			{
				control = new JbstContainerControl(prefix, tagName);
			}

			if (this.current == null)
			{
				this.current = this.document;
			}

			this.current.ChildControls.Add(control);
			this.current = control;
		}

		private void PopTag(string tagName)
		{
			if (tagName == null)
			{
				tagName = String.Empty;
			}

			if (this.current == null)
			{
				throw new InvalidOperationException("Push/Pop mismatch? (current tag is null)");
			}

			if (!String.IsNullOrEmpty(tagName) &&
				!tagName.Equals(this.current.RawName, StringComparison.OrdinalIgnoreCase))
			{
				//throw new InvalidOperationException("Push/Pop mismatch? (tag names do not match)");
				return;
			}

			if (JbstWriter.ScriptTagName.Equals(this.current.RawName, StringComparison.OrdinalIgnoreCase))
			{
				// script tags get converted once fully parsed
				this.Declarations.Append(this.current);
			}

			JbstInline inline = this.current as JbstInline;

			this.current = this.current.Parent;

			if (inline != null && inline.IsAnonymous)
			{
				// consolidate anonymous inline templates directly into body
				this.current.ChildControls.Remove(inline);
				if (inline.ChildControlsSpecified)
				{
					this.current.ChildControls.AddRange(inline.ChildControls);
				}
			}
		}

		private void AddAttribute(string name, string value)
		{
			if (this.current == null)
			{
				throw new InvalidOperationException("Unexpected attribute");
			}

			this.SetAttribute(this.current, name, value);
		}

		private void AddAttribute(string name, JbstControl value)
		{
			if (this.current == null)
			{
				throw new InvalidOperationException("Unexpected attribute");
			}

			this.current.Attributes[name] = value;
		}

		private void SetAttribute(JbstContainerControl target, string name, string value)
		{
			value = HtmlDistiller.DecodeHtmlEntities(value);
			if ("style".Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				this.SetStyle(target, null, value);
			}
			else
			{
				target.Attributes[name] = value;
			}
		}

		private void AddStyle(string name, string value)
		{
			if (this.current == null)
			{
				throw new InvalidOperationException("Unexpected attribute");
			}

			this.SetStyle(this.current, name, value);
		}

		private void SetStyle(JbstContainerControl target, string name, string value)
		{
			if (String.IsNullOrEmpty(name) && String.IsNullOrEmpty(value))
			{
				return;
			}

			if (target == null)
			{
				throw new NullReferenceException("target is null");
			}

			string style =
				target.Attributes.ContainsKey("style") ?
				target.Attributes["style"] as String :
				null;

			if (style != null && !style.EndsWith(";"))
			{
				style += ";";
			}

			if (String.IsNullOrEmpty(name))
			{
				style += value;
			}
			else
			{
				style += name+':'+value;
			}

			target.Attributes["style"] = style;
		}

		public void Clear()
		{
			this.document.ChildControls.Clear();
		}

		#endregion Parse Methods

		#region Render Methods

		public void Render(TextWriter writer)
		{
			this.ProcessDirectives();

			// add JSLINT directives
			string globals = this.GetGlobals();
			if (!String.IsNullOrEmpty(globals))
			{
				writer.WriteLine("/*global {0} */", globals);
			}

			if (!EcmaScriptWriter.WriteNamespaceDeclaration(writer, this.JbstName, null, true))
			{
				writer.Write("var ");
			}

			// wrap with ctor and assign
			writer.Write(this.JbstName);
			writer.WriteLine(" = JsonML.BST(");

			// render root node of content (null is OK)
			EcmaScriptWriter jsWriter = new EcmaScriptWriter(writer);
			jsWriter.Settings.PrettyPrint = true;
			jsWriter.Write(this.JbstParseTree);

			writer.WriteLine(");");

			// render any declarations
			if (this.Declarations.HasCode)
			{
				this.Declarations.OwnerName = this.JbstName;
				jsWriter.Write(this.Declarations);
			}
		}

		#endregion Render Methods

		#region IHtmlWriter Members

		void IHtmlWriter.WriteLiteral(string value)
		{
			this.AppendChild(value);
		}

		void IHtmlWriter.WriteTag(HtmlTag tag)
		{
			switch (tag.TagType)
			{
				case HtmlTagType.Unparsed:
				{
					this.WriteUnparsedTag(tag);
					break;
				}
				case HtmlTagType.FullTag:
				case HtmlTagType.BeginTag:
				{
					this.PushTag(tag.RawName);

					if (tag.HasAttributes)
					{
						this.WriteAttributes(tag);
					}

					if (tag.HasStyles)
					{
						this.WriteStyles(tag);
					}

					if (tag.TagType == HtmlTagType.FullTag)
					{
						this.PopTag(tag.RawName);
					}
					break;
				}
				case HtmlTagType.EndTag:
				{
					this.PopTag(tag.RawName);
					break;
				}
			}
		}

		private void WriteUnparsedTag(HtmlTag tag)
		{
			switch (tag.TagName)
			{
				case "%@":
				{
					// store directive for specialized parsing
					this.Directives.Append(tag.ToString());
					break;
				}
				case "%!":
				{
					// analogous to static code, or JSP declarations
					// executed only on initialization of template
					// output from declarations are appended after the template
					this.Declarations.Append(tag.Content);
					break;
				}
				case "%#": // databinding expression
				{
					// unparsed expressions are emitted directly into JBST
					JbstUnparsedBlock code = new JbstUnparsedBlock(tag.Content);
					this.AppendChild(code);
					break;
				}
				case "%=": // inline expression
				{
					// expressions are emitted directly into JBST
					JbstExpressionBlock code = new JbstExpressionBlock(tag.Content);
					this.AppendChild(code);
					break;
				}
				case "%$":
				{
					// expressions are emitted directly into JBST
					JbstExtensionBlock code = new JbstExtensionBlock(tag.Content, this.path);
					this.AppendChild(code);
					break;
				}
				case "%":
				{
					// statements are emitted directly into JBST
					JbstStatementBlock code = new JbstStatementBlock(tag.Content);
					this.AppendChild(code);
					break;
				}
				case "%--":
				{
					// server-side comments are omitted even for debug
					break;
				}
				case "!--":
				{
					// HTML Comments are emitted directly into JBST
					JbstCommentBlock code = new JbstCommentBlock(tag.Content);
					this.AppendChild(code);
					break;
				}
				default:
				{
					// unrecognized sequences get emitted as encoded text
					this.AppendChild(tag.ToString());
					break;
				}
			}
		}

		private void WriteStyles(HtmlTag tag)
		{
			foreach (KeyValuePair<string, string> style in tag.FilteredStyles)
			{
				this.AddStyle(style.Key, style.Value);
			}
		}

		private void WriteAttributes(HtmlTag tag)
		{
			foreach (KeyValuePair<string, object> attrib in tag.FilteredAttributes)
			{
				// normalize JBST command names
				string key = attrib.Key.StartsWith(JbstCommandBase.JbstPrefix, StringComparison.OrdinalIgnoreCase) ?
					attrib.Key.ToLowerInvariant() : attrib.Key;

				if (attrib.Value is string)
				{
					this.AddAttribute(attrib.Key, (string)attrib.Value);
				}
				else if (attrib.Value is HtmlTag)
				{
					HtmlTag codeVal = (HtmlTag)attrib.Value;
					switch (codeVal.TagName)
					{
						case "%@":
						{
							// store directive for specialized parsing
							this.Directives.Append(codeVal.ToString());
							break;
						}
						case "%!":
						{
							// analogous to static code, or JSP declarations
							// executed only on initialization of template
							// output from declarations are appended after the template
							this.Declarations.Append(codeVal.Content);
							break;
						}
						case "%#": // databinding expression
						//{
						//    // unparsed expressions are emitted directly into JBST
						//    JbstUnparsedBlock code = new JbstUnparsedBlock(codeVal.Content);
						//    this.AddAttribute(key, code);
						//    break;
						//}
						case "%=": // inline expression
						{
							// expressions are emitted directly into JBST
							JbstExpressionBlock code = new JbstExpressionBlock(codeVal.Content);
							this.AddAttribute(key, code);
							break;
						}
						case "%$":
						{
							// expressions are emitted directly into JBST
							JbstExtensionBlock code = new JbstExtensionBlock(codeVal.Content, this.path);
							this.AddAttribute(key, code);
							break;
						}
						case "%":
						{
							// statements are emitted directly into JBST
							JbstStatementBlock code = new JbstStatementBlock(codeVal.Content);
							this.AddAttribute(key, code);
							break;
						}
						case "%--":
						{
							// server-side comments are omitted even for debug
							break;
						}
						case "!--":
						{
							// HTML Comments are emitted directly into JBST
							// but get removed when minified
							JbstCommentBlock code = new JbstCommentBlock(codeVal.Content);
							this.AddAttribute(key, code);
							break;
						}
						default:
						{
							// unrecognized sequences get emitted as encoded text
							this.AddAttribute(key, codeVal.ToString());
							break;
						}
					}
				}
			}
		}

		#endregion IHtmlWriter Members

		#region Directive Methods

		private void ProcessDirectives()
		{
			DirectiveParser parser = new DirectiveParser(this.Directives.ToString(), String.Empty);
			parser.ProcessDirective += this.ProcessDirective;

			int index = 0;
			parser.ParseDirectives(out index);
		}

		private void ProcessDirective(string directiveName, IDictionary<string, string> attribs, int lineNumber)
		{
			if (String.IsNullOrEmpty(directiveName))
			{
				return;
			}

			switch (directiveName.ToLowerInvariant())
			{
				case "page":
				case "control":
				{
					this.JbstName = attribs.ContainsKey("name") ?
						EcmaScriptIdentifier.EnsureValidIdentifier(attribs["name"], true) :
						null;

					if (attribs.ContainsKey("AutoMarkup"))
					{
						try
						{
							this.AutoMarkup = (AutoMarkupType)Enum.Parse(typeof(AutoMarkupType), attribs["AutoMarkup"], true);
						}
						catch
						{
							throw new ArgumentException("\""+attribs["AutoMarkup"]+"\" is an invalid value for AutoMarkup.");
						}
					}
					else
					{
						this.AutoMarkup = AutoMarkupType.None;
					}

					string package = attribs.ContainsKey("import") ? attribs["import"] : null;
					if (!String.IsNullOrEmpty(package))
					{
						string[] packages = package.Split(ImportDelim, StringSplitOptions.RemoveEmptyEntries);
						this.Imports.AddRange(packages);
					}
					break;
				}
				case "import":
				{
					string package = attribs.ContainsKey("namespace") ? attribs["namespace"] : null;
					if (!String.IsNullOrEmpty(package))
					{
						this.Imports.Add(package);
					}
					break;
				}
				default:
				{
					// not implemented
					break;
				}
			}
		}

		/// <summary>
		/// Generates a globals list from import directives
		/// </summary>
		/// <returns></returns>
		private string GetGlobals()
		{
			StringBuilder globals = new StringBuilder();

			this.Imports.Insert(0, "JsonML.BST");

			foreach (string import in this.Imports)
			{
				string ident = EcmaScriptIdentifier.EnsureValidIdentifier(import, true);

				if (String.IsNullOrEmpty(ident))
				{
					continue;
				}

				if (globals.Length > 0)
				{
					globals.Append(", ");
				}

				int dot = ident.IndexOf('.');
				globals.Append((dot < 0) ? ident : ident.Substring(0, dot));
			}

			return globals.ToString();
		}

		#endregion Directive Methods

		#region Utility Methods

		/// <summary>
		/// Splits the prefix and tag name
		/// </summary>
		/// <param name="rawName"></param>
		/// <param name="tagName"></param>
		/// <returns></returns>
		private static string SplitPrefix(string rawName, out string tagName)
		{
			int index = String.IsNullOrEmpty(rawName) ?
				-1 : rawName.IndexOf(PrefixDelim);

			if (index < 0)
			{
				tagName = rawName;
				return String.Empty;
			}

			tagName = rawName.Substring(index+1);
			return rawName.Substring(0, index+1);
		}

		#endregion Utility Methods
	}
}
