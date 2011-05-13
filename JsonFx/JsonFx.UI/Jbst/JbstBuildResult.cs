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
using System.IO;
using System.Text;

using JsonFx.Client;
using JsonFx.Compilation;
using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Encapsulates the binding logic for instantiating a JBST on a page.
	/// </summary>
	public class JbstBuildResult
	{
		#region Fields

		private readonly EcmaScriptIdentifier jbstName;
		private AutoMarkupType autoMarkup;
		private string id;
		private bool isDebug;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="jbstName"></param>
		public JbstBuildResult(EcmaScriptIdentifier jbstName)
			: this(jbstName, AutoMarkupType.None)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="jbstName"></param>
		/// <param name="autoMarkup"></param>
		public JbstBuildResult(EcmaScriptIdentifier jbstName, AutoMarkupType autoMarkup)
		{
			this.jbstName = jbstName;
			this.autoMarkup = autoMarkup;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the name of the JBST
		/// </summary>
		public EcmaScriptIdentifier JbstName
		{
			get { return this.jbstName; }
		}

		/// <summary>
		/// Gets the AutoMarkup setting from the JBST
		/// </summary>
		public virtual AutoMarkupType AutoMarkup
		{
			get { return this.autoMarkup; }
			set { this.autoMarkup = value; }
		}

		/// <summary>
		/// Gets and sets the ID of the placeholder element
		/// </summary>
		public virtual string ID
		{
			get
			{
				if (this.id == null)
				{
					return String.Empty;
				}
				return this.id;
			}
			set { this.id = value; }
		}

		/// <summary>
		/// Gets and sets if should render "Pretty-Printed".
		/// </summary>
		public virtual bool IsDebug
		{
			get { return this.isDebug; }
			set { this.isDebug = value; }
		}

		#endregion Properties

		#region Render Methods

		internal delegate void InnerCallback(TextWriter writer);

		/// <summary>
		/// Renders the JBST control reference and any stored data to be used.
		/// </summary>
		/// <param name="writer">output</param>
		/// <param name="data">data to be bound as an object which will be serialized</param>
		public void Write(TextWriter writer, object data)
		{
			this.Write(writer, data, -1, -1);
		}

		/// <summary>
		/// Renders the JBST control reference and any stored data to be used.
		/// </summary>
		/// <param name="writer">output</param>
		/// <param name="data">data to be bound as an object which will be serialized</param>
		/// <param name="index">the data index</param>
		/// <param name="count">the total data count</param>
		public void Write(TextWriter writer, object data, int index, int count)
		{
			this.Write(writer, data, index, count, null);
		}

		/// <summary>
		/// Renders the JBST control reference and any stored data to be used.
		/// </summary>
		/// <param name="writer">output</param>
		/// <param name="data">data to be bound as an object which will be serialized</param>
		/// <param name="index">the data index</param>
		/// <param name="count">the total data count</param>
		/// <param name="inner">a callback for writing inner placeholder content</param>
		internal void Write(TextWriter writer, object data, int index, int count, InnerCallback inner)
		{
			if (String.IsNullOrEmpty(this.JbstName))
			{
				throw new ArgumentNullException("JBST Name must be specified.");
			}

			// generate an ID for controls which do not have explicit
			if (String.IsNullOrEmpty(this.ID))
			{
				// happens with no parents
				this.ID = "_"+Guid.NewGuid().ToString("n");
			}

			bool hasInner = (inner != null);
			string placeholder = hasInner ? "div" : "noscript";

			// render the placeholder hook
			writer.Write('<');
			writer.Write(placeholder);
			writer.Write(" id=\"");
			writer.Write(this.ID);
			writer.Write("\">");

			if (hasInner)
			{
				// render inner as loading/error markup
				inner(writer);
			}

			string inlineData = null;
			if (data != null && !(data is EcmaScriptIdentifier) && this.AutoMarkup == AutoMarkupType.Data)
			{
				if (hasInner)
				{
					writer.Write("<noscript>");
				}

				// serialize InlineData as a JavaScript literal
				StringBuilder builder = new StringBuilder();

				JsonMarkupWriter jsWriter = new JsonMarkupWriter(builder, writer);
				if (this.IsDebug)
				{
					jsWriter.Settings.PrettyPrint = true;
					jsWriter.Settings.NewLine = Environment.NewLine;
					jsWriter.Settings.Tab = "\t";
				}
				jsWriter.Write(data);

				if (hasInner)
				{
					writer.Write("</noscript>");
				}

				inlineData = builder.ToString();
			}

			writer.Write("</");
			writer.Write(placeholder);
			writer.Write('>');

			// render the binding script
			writer.Write("<script type=\"text/javascript\">");

			writer.Write(this.JbstName);
			writer.Write(".replace(\"");
			writer.Write(this.ID);
			writer.Write("\",");

			if (!String.IsNullOrEmpty(inlineData))
			{
				writer.Write(inlineData);
			}
			else if (data != null)
			{
				// serialize InlineData as a JavaScript literal
				EcmaScriptWriter jsWriter = new EcmaScriptWriter(writer);
				if (this.IsDebug)
				{
					jsWriter.Settings.PrettyPrint = true;
					jsWriter.Settings.NewLine = Environment.NewLine;
					jsWriter.Settings.Tab = "\t";
				}
				jsWriter.Write(data);
			}
			else
			{
				// smallest most innocuous default data
				writer.Write("{}");
			}

			if (index >= 0)
			{
				writer.Write(",(");
				writer.Write(index);
				writer.Write(')');

				if (count >= 0)
				{
					writer.Write(",(");
					writer.Write(count);
					writer.Write(')');
				}
			}
			writer.Write(");");

			writer.Write("</script>");
		}

		#endregion Render Methods

		#region Factory Methods

		public static JbstBuildResult FindJbst(string jbstName)
		{
			string className = JbstCodeProvider.GetClassForJbst(jbstName);

			JbstBuildResult jbst = BuildCache.Instance.Create<JbstBuildResult>(className);
			if (jbst == null)
			{
				// use a simple value rather than code generated
				jbst = new JbstBuildResult(jbstName);
			}

			return jbst;
		}

		#endregion Factory Methods
	}
}
