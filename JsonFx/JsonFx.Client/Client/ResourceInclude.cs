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
using System.Web.UI;

namespace JsonFx.Client
{
	/// <summary>
	/// Control for referencing resources
	/// </summary>
	[ToolboxData("<{0}:ResourceInclude runat=\"server\"></{0}:ResourceInclude>")]
	public class ResourceInclude : Control, IAttributeAccessor
	{
		#region Fields

		private bool isDebug;
		private string sourceUrl;
		private bool usePageCulture = true;
		private bool suppressLocalization;
		private CssIncludeType styleFormat = CssIncludeType.Link;
		private IDictionary<string, string> attributes;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ResourceInclude()
		{
			this.isDebug = this.Context.IsDebuggingEnabled;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets if should render a debuggable ("Pretty-Print") reference.
		/// </summary>
		[DefaultValue(false)]
		public bool IsDebug
		{
			get { return this.isDebug; }
			set { this.isDebug = value; }
		}

		/// <summary>
		/// Gets and sets resource url.
		/// </summary>
		[DefaultValue("")]
		public string SourceUrl
		{
			get
			{
				if (this.sourceUrl == null)
				{
					return String.Empty;
				}
				return this.sourceUrl;
			}
			set { this.sourceUrl = value; }
		}

		/// <summary>
		/// Gets and sets if page determines the culture or
		/// if uses CurrentUICulture
		/// </summary>
		[DefaultValue(true)]
		public bool UsePageCulture
		{
			get { return this.usePageCulture; }
			set { this.usePageCulture = value; }
		}

		/// <summary>
		/// Gets and sets if will be manually emitting localization values
		/// </summary>
		[DefaultValue(false)]
		public bool SuppressLocalization
		{
			get { return this.suppressLocalization; }
			set { this.suppressLocalization = value; }
		}

		/// <summary>
		/// Gets and sets if page determines the culture or
		/// if uses CurrentUICulture
		/// </summary>
		[DefaultValue(CssIncludeType.Link)]
		public CssIncludeType StyleFormat
		{
			get { return this.styleFormat; }
			set { this.styleFormat = value; }
		}

		/// <summary>
		/// Gets the collection of custom attributes
		/// </summary>
		public IDictionary<string, string> Attributes
		{
			get
			{
				if (this.attributes == null)
				{
					this.attributes = new Dictionary<string, string>(4, StringComparer.OrdinalIgnoreCase);
				}
				return this.attributes;
			}
		}

		#endregion Properties

		#region Page Event Handlers

		protected override void Render(HtmlTextWriter writer)
		{
			ResourceBuildResult result = this.GetResource();

			result.Attributes = this.attributes;
			result.IsDebug = this.IsDebug;

			if (result is ScriptBuildResult)
			{
				((ScriptBuildResult)result).SuppressLocalization = this.SuppressLocalization;
				((ScriptBuildResult)result).UsePageCulture = this.UsePageCulture;
			}
			else if (result is CssBuildResult)
			{
				((CssBuildResult)result).StyleFormat = this.StyleFormat;
			}

			result.Write(writer);
		}

		protected virtual ResourceBuildResult GetResource()
		{
			return ResourceBuildResult.FindResource(this.SourceUrl);
		}

		#endregion Page Event Handlers

		#region IAttributeAccessor Members

		string IAttributeAccessor.GetAttribute(string key)
		{
			if (this.attributes == null || !this.attributes.ContainsKey(key))
			{
				return null;
			}

			return this.attributes[key];
		}

		void IAttributeAccessor.SetAttribute(string key, string value)
		{
			this.Attributes[key] = value;
		}

		#endregion IAttributeAccessor Members
	}
}
