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
using System.Web;

using JsonFx.Json;

namespace JsonFx.Client
{
	/// <summary>
	/// Specifically for emitting runtime data to the page as JavaScript variables.
	/// </summary>
	public class DataBlockWriter
	{
		#region Constants

		private const string ScriptOpen = "<script type=\"text/javascript\">";
		private const string ScriptClose = "</script>";
		private const string NoScriptOpen = "<noscript>";
		private const string NoScriptClose = "</noscript>";
		private const string VarAssignmentDebug = "{0} = ";
		private const string VarAssignment = "{0}=";
		private const string VarAssignmentEnd = ";";

		#endregion Constants

		#region Fields

		private bool isDebug;
		private AutoMarkupType autoMarkup;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public DataBlockWriter()
			: this(AutoMarkupType.None, false)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		public DataBlockWriter(AutoMarkupType autoMarkup)
			: this(autoMarkup, false)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		public DataBlockWriter(AutoMarkupType autoMarkup, bool isDebug)
		{
			this.AutoMarkup = autoMarkup;
			this.IsDebug = isDebug;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets if should also render the data as markup for noscript clients.
		/// </summary>
		public AutoMarkupType AutoMarkup
		{
			get { return this.autoMarkup; }
			set { this.autoMarkup = value; }
		}

		/// <summary>
		/// Gets and sets if should render as a debuggable ("Pretty-Print") block.
		/// </summary>
		public bool IsDebug
		{
			get { return this.isDebug; }
			set { this.isDebug = value; }
		}

		#endregion Properties

		#region Render Methods

		/// <summary>
		/// Renders the data items as a block of JavaScript
		/// </summary>
		/// <param name="writer"></param>
		public void Write(TextWriter writer, IDictionary<string, object> data)
		{
			if (data == null || data.Count < 1)
			{
				// emit nothing when empty
				return;
			}

			List<string> namespaces = new List<string>();

			StringWriter markup;
			EcmaScriptWriter jsWriter;
			if (this.AutoMarkup == AutoMarkupType.Data)
			{
				markup = new StringWriter();
				jsWriter = new JsonMarkupWriter(writer, markup);
			}
			else
			{
				markup = null;
				jsWriter = new EcmaScriptWriter(writer);
			}

			if (this.IsDebug)
			{
				jsWriter.Settings.PrettyPrint = true;
				jsWriter.Settings.NewLine = Environment.NewLine;
				jsWriter.Settings.Tab = "\t";
			}

			writer.Write(DataBlockWriter.ScriptOpen);

			foreach (string key in data.Keys)
			{
				if (markup != null)
				{
					if (this.IsDebug)
					{
						markup.WriteLine();
					}
					markup.Write("<div title=\"");
					HttpUtility.HtmlAttributeEncode(key, markup);
					markup.Write("\">");
				}

				string declaration;
				if (!EcmaScriptWriter.WriteNamespaceDeclaration(writer, key, namespaces, this.IsDebug))
				{
					declaration = "var "+key;
				}
				else
				{
					declaration = key;
				}

				if (this.IsDebug)
				{
					writer.Write(DataBlockWriter.VarAssignmentDebug, declaration);
					if (data[key] != null &&
						data[key].GetType().IsClass)
					{
						writer.WriteLine();
					}
				}
				else
				{
					writer.Write(DataBlockWriter.VarAssignment, declaration);
				}

				// emit the value as JSON
				jsWriter.Write(data[key]);
				writer.Write(DataBlockWriter.VarAssignmentEnd);

				if (markup != null)
				{
					markup.Write("</div>");
				}

				if (this.IsDebug)
				{
					writer.WriteLine();
				}
			}

			writer.Write(DataBlockWriter.ScriptClose);

			if (markup != null)
			{
				writer.Write(DataBlockWriter.NoScriptOpen);
				writer.Write(markup.ToString());
				writer.Write(DataBlockWriter.NoScriptClose);
			}
		}

		#endregion Render Methods
	}
}
