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
using System.ComponentModel;
using System.IO;
using System.Web.UI;

using JsonFx.Client;
using JsonFx.Compilation;
using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Convenience control for combining JBST controls and JSON data on an ASP.NET page.
	/// </summary>
	[ToolboxData("<{0}:Control runat=\"server\" Name=\"\"></{0}:Control>")]
	public class Control : AutoDataBindControl
	{
		#region Fields

		private bool isDebug;
		private AutoMarkupType autoMarkup = AutoMarkupType.Auto;
		private EcmaScriptIdentifier name;
		private object data;
		private int? index;
		private int? count;
		private IDictionary<string, object> dataItems;
		private JbstBuildResult jbst;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public Control()
		{
			this.isDebug = this.Context.IsDebuggingEnabled;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the script variable name of the JBST control to be bound.
		/// </summary>
		[DefaultValue("")]
		public virtual EcmaScriptIdentifier Name
		{
			get { return this.name; }
			set
			{
				this.name = value;
				this.jbst = null;
			}
		}

		/// <summary>
		/// Gets and sets data to be bound as JavaScript variable reference.
		/// </summary>
		[DefaultValue("")]
		public virtual string Data
		{
			get
			{
				if (this.data is EcmaScriptIdentifier)
				{
					return (EcmaScriptIdentifier)this.data;
				}
				else
				{
					return String.Empty;
				}
			}
			set { this.data = new EcmaScriptIdentifier(value); }
		}

		/// <summary>
		/// Gets and sets data to be bound as an object which will be serialized.
		/// </summary>
		[DefaultValue(null)]
		public virtual object InlineData
		{
			get { return this.data; }
			set { this.data = value; }
		}

		/// <summary>
		/// Gets and sets the data index, passed in when binding the data and JBST.
		/// </summary>
		[DefaultValue(-1)]
		public virtual int Index
		{
			get
			{
				if (!this.index.HasValue)
				{
					return -1;
				}
				return this.index.Value;
			}
			set
			{
				if (value < 0)
				{
					this.index = null;
					return;
				}
				this.index = value;
			}
		}

		/// <summary>
		/// Gets and sets the total data count, passed in when binding the data and JBST.
		/// </summary>
		[DefaultValue(-1)]
		public virtual int Count
		{
			get
			{
				if (!this.count.HasValue)
				{
					return -1;
				}
				return this.count.Value;
			}
			set
			{
				if (value < 0)
				{
					this.count = null;
					return;
				}
				this.count = value;
			}
		}

		/// <summary>
		/// Gets a dictionary of Data to emit to the page.
		/// </summary>
		public IDictionary<string, object> DataItems
		{
			get
			{
				if (this.dataItems == null)
				{
					this.dataItems = new Dictionary<string, object>();
				}
				return this.dataItems;
			}
		}

		/// <summary>
		/// Gets and sets if should render as a debuggable ("Pretty-Print") block.
		/// </summary>
		[DefaultValue(false)]
		public bool IsDebug
		{
			get { return this.isDebug; }
			set { this.isDebug = value; }
		}

		/// <summary>
		/// Gets and sets if should also render the data as markup for noscript clients.
		/// </summary>
		[DefaultValue(AutoMarkupType.Auto)]
		public AutoMarkupType AutoMarkup
		{
			get { return this.autoMarkup; }
			set { this.autoMarkup = value; }
		}

		protected JbstBuildResult Jbst
		{
			get
			{
				if (this.jbst == null)
				{
					this.jbst = JbstBuildResult.FindJbst(this.Name);
				}

				return this.jbst;
			}
		}

		#endregion Properties

		#region Page Event Handlers

		/// <summary>
		/// Renders the JBST control reference and any stored data to be used.
		/// </summary>
		/// <param name="writer"></param>
		protected override void Render(HtmlTextWriter writer)
		{
			writer.BeginRender();
			try
			{
				// render any named data items
				if (this.dataItems != null && this.dataItems.Count > 0)
				{
					new DataBlockWriter(this.EnsureAutoMarkup()).Write(writer, this.dataItems);
				}

				// generate an ID for controls which do not have explicitly set
				this.EnsureID();
				this.Jbst.ID = this.ClientID;
				this.Jbst.IsDebug = this.IsDebug;

				// render JBST
				if (this.HasControls())
				{
					this.Jbst.Write(writer, this.InlineData, this.Index, this.Count, this.RenderChildrenCallback);
				}
				else
				{
					this.Jbst.Write(writer, this.InlineData, this.Index, this.Count);
				}
			}
			finally
			{
				writer.EndRender();
			}
		}

		private void RenderChildrenCallback(TextWriter writer)
		{
			bool flush = false;
			HtmlTextWriter htmlWriter = writer as HtmlTextWriter;
			if (htmlWriter == null)
			{
				htmlWriter = new XhtmlTextWriter(writer);
				flush = true;
			}

			this.RenderChildren(htmlWriter);

			if (flush)
			{
				htmlWriter = new HtmlTextWriter(writer);
				htmlWriter.Flush();
			}
		}

		#endregion Page Event Handlers

		#region Utility Methods

		private AutoMarkupType EnsureAutoMarkup()
		{
			if (this.AutoMarkup != AutoMarkupType.Auto)
			{
				return this.AutoMarkup;
			}

			// get AutoMarkup setting from JBST
			if (this.Jbst.AutoMarkup != AutoMarkupType.None)
			{
				this.AutoMarkup = AutoMarkupType.Data;
			}
			else
			{
				this.AutoMarkup = AutoMarkupType.None;
			}

			return this.AutoMarkup;
		}

		#endregion Utility Methods
	}
}
