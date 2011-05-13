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
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using JsonFx.Json;

namespace JsonFx.History
{
	[ToolboxData("<{0}:HistoryManager runat=\"server\" />")]
	public class HistoryManager : WebControl
	{
		#region Constants

		private const string DefaultHistoryUrl = "~/robots.txt";
		private const string DefaultCallback = "null";

		#endregion Constants

		#region Fields

		private object startState = null;
		private string callback = null;
		private string historyUrl = null;
		private bool isDebugMode = false;
		private bool? usePhysicalUrl = null;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <remarks>
		/// Uses iframe as tag name.
		/// </remarks>
		public HistoryManager() : base(HtmlTextWriterTag.Iframe)
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the initial state object which represents this page request
		/// </summary>
		[DefaultValue(null)]
		public object StartState
		{
			get { return this.startState; }
			set { this.startState = value; }
		}

		/// <summary>
		/// Gets and sets the function name to be used as a callback when the history changes
		/// </summary>
		[DefaultValue(DefaultCallback)]
		public string Callback
		{
			get { return String.IsNullOrEmpty(this.callback) ? DefaultCallback : this.callback; }
			set { this.callback = value; }
		}

		/// <summary>
		/// Gets and sets the URL to be used when the history changes
		/// for browsers that do not accept virtually built documents
		/// </summary>
		/// <remarks>Defaults to "~/robots.txt"</remarks>
		[DefaultValue(DefaultHistoryUrl)]
		public virtual string HistoryUrl
		{
			get
			{
				if (String.IsNullOrEmpty(this.historyUrl))
				{
					return DefaultHistoryUrl;
				}
				return this.historyUrl;
			}
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					int query = value.IndexOf('?');
					if (query >= 0)
					{
						value = value.Substring(0, query);
					}
				}

				this.historyUrl = value;
			}
		}

		/// <summary>
		/// Gets and sets a value which shows or hides the history iframe.
		/// </summary>
		[Browsable(true)]
		[DefaultValue(false)]
		[Description("Gets and sets a value which shows or hides the history iframe.")]
		public bool IsDebugMode
		{
			get { return this.isDebugMode; }
			set { this.isDebugMode = value; }
		}

		/// <summary>
		/// Gets a value which indicates if should use physical or virtual documents
		/// </summary>
		private bool UsePhysicalUrl
		{
			get
			{
				if (!this.usePhysicalUrl.HasValue)
				{
					HttpBrowserCapabilities browser = this.Page.Request.Browser;
					if (browser.Browser == null ||
						browser.Browser.IndexOf("safari", StringComparison.OrdinalIgnoreCase) < 0)
					{
						// currently Safari seems to be the browser with virtual document issues
						this.usePhysicalUrl = false;
					}
					else
					{
						this.usePhysicalUrl = true;
					}
				}

				return this.usePhysicalUrl.Value;
			}
		}

		#endregion Properties

		#region Page Events

		protected override void AddAttributesToRender(HtmlTextWriter writer)
		{
			base.AddAttributesToRender(writer);

			string onload = String.Format(
				"JsonFx.History.load(this,{0},{1},\"{2}\");",
				this.Callback,
				JsonEncode(this.StartState),
				this.UsePhysicalUrl ? this.ResolveUrl(this.HistoryUrl) : "");
			writer.AddAttribute("onload", onload, true);

			if (this.IsDebugMode)
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
				writer.AddStyleAttribute(HtmlTextWriterStyle.BackgroundColor, "white");
			}
			else
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");

				// it is rumored that "display:none" breaks some browsers but I haven't seen it
				// this could be used instead to not affect the layout
				//writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
				//writer.AddStyleAttribute(HtmlTextWriterStyle.Visibility, "hidden");
			}
		}

		#endregion Page Events

		#region Utility Methods

		private static string JsonEncode(object state)
		{
			StringBuilder builder = new StringBuilder();
			using (JsonWriter jsonWriter = new JsonWriter(builder))
			{
				jsonWriter.Write(state);
			}
			return builder.ToString();
		}

		#endregion Utility Methods
	}
}
