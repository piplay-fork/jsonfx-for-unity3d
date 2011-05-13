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
using System.Threading;
using System.Web;

using JsonFx.Compilation;
using JsonFx.Handlers;
using System.Web.Compilation;

namespace JsonFx.Client
{
	public enum CssIncludeType
	{
		Link,
		Import
	}

	public class CssBuildResult : ResourceBuildResult
	{
		#region Constants

		private const string LinkStart = "<link";
		private const string LinkEnd = " />";

		private const string ImportStart = "<style";
		private const string ImportMiddle = ">@import url(";
		private const string ImportEnd = ");</style>";

		#endregion Constants

		#region Fields

		private CssIncludeType styleFormat = CssIncludeType.Link;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="sourceUrl"></param>
		public CssBuildResult(string sourceUrl)
			: this(sourceUrl, null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="sourceUrl"></param>
		public CssBuildResult(string sourceUrl, IBuildResult buildResult)
			: base(sourceUrl, buildResult)
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets if page determines the culture or
		/// if uses CurrentUICulture
		/// </summary>
		public CssIncludeType StyleFormat
		{
			get { return this.styleFormat; }
			set { this.styleFormat = value; }
		}

		#endregion Properties

		#region Render Methods

		public override void Write(TextWriter writer)
		{
			bool customType = this.Attributes.ContainsKey("type");

			try
			{
				if (!customType)
				{
					IBuildResult result = this.BuildResult;

					string mimeType;
					if (result == null)
					{
						mimeType = CssResourceCodeProvider.MimeType;
					}
					else
					{
						mimeType = result.ContentType;
					}
					this.Attributes["type"] = mimeType;
				}

				string url = this.ResolveUrl();
				if (this.StyleFormat == CssIncludeType.Import)
				{
					this.WriteImport(writer, url);
				}
				else
				{
					this.WriteLink(writer, url);
				}
			}
			finally
			{
				if (!customType)
				{
					this.Attributes.Remove("type");
				}
				this.Attributes.Remove("href");
				this.Attributes.Remove("rel");
			}
		}

		private void WriteLink(TextWriter writer, string url)
		{
			this.Attributes["href"] = url;
			if (!this.Attributes.ContainsKey("rel"))
			{
				this.Attributes["rel"] = "stylesheet";
			}

			writer.Write(CssBuildResult.LinkStart);
			this.WriteAttributes(writer);
			writer.Write(CssBuildResult.LinkEnd);
		}

		private void WriteImport(TextWriter writer, string url)
		{
			writer.Write(CssBuildResult.ImportStart);
			this.WriteAttributes(writer);
			writer.Write(CssBuildResult.ImportMiddle);
			writer.Write(url);
			writer.Write(CssBuildResult.ImportEnd);
		}

		#endregion Render Methods
	}

	public class ScriptBuildResult : ResourceBuildResult
	{
		#region Constants

		private const string ScriptStart = "<script";
		private const string ScriptEnd = "></script>";

		#endregion Constants

		#region Fields

		private bool suppressLocalization;
		private bool usePageCulture = true;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="sourceUrl"></param>
		public ScriptBuildResult(string sourceUrl)
			: this(sourceUrl, null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="sourceUrl"></param>
		public ScriptBuildResult(string sourceUrl, IBuildResult buildResult)
			: base(sourceUrl, buildResult)
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets if will be manually emitting localization values
		/// </summary>
		public bool SuppressLocalization
		{
			get { return this.suppressLocalization; }
			set { this.suppressLocalization = value; }
		}

		/// <summary>
		/// Gets and sets if page determines the culture or
		/// if uses CurrentUICulture
		/// </summary>
		public bool UsePageCulture
		{
			get { return this.usePageCulture; }
			set { this.usePageCulture = value; }
		}

		#endregion Properties

		#region Render Methods

		public override void Write(TextWriter writer)
		{
			IBuildResult result = this.BuildResult;
			bool customType = this.Attributes.ContainsKey("type");

			try
			{
				if (!customType)
				{
					string mimeType;
					if (result == null)
					{
						mimeType = ScriptResourceCodeProvider.MimeType;
					}
					else
					{
						mimeType = result.ContentType;
					}
					this.Attributes["type"] = mimeType;
				}
				string url = this.ResolveUrl();
				this.Attributes["src"] = url;

				writer.Write(ScriptBuildResult.ScriptStart);

				this.WriteAttributes(writer);

				writer.Write(ScriptBuildResult.ScriptEnd);

				if (!this.SuppressLocalization &&
				this is IGlobalizedBuildResult)
				{
					string culture = this.UsePageCulture ?
					Thread.CurrentThread.CurrentCulture.Name :
					String.Empty;

					this.Attributes["src"] = ResourceHandler.GetLocalizationUrl(url, culture);

					writer.Write(ScriptBuildResult.ScriptStart);
					this.WriteAttributes(writer);
					writer.Write(ScriptBuildResult.ScriptEnd);
				}
			}
			finally
			{
				if (!customType)
				{
					this.Attributes.Remove("type");
				}
				this.Attributes.Remove("src");
			}
		}

		#endregion Render Methods
	}

	public abstract class ResourceBuildResult
	{
		#region Fields

		private readonly string sourceUrl;
		private IDictionary<string, string> attributes;
		private bool isDebug;
		private readonly IBuildResult buildResult;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="sourceUrl"></param>
		public ResourceBuildResult(string sourceUrl, IBuildResult buildResult)
		{
			if (sourceUrl == null)
			{
				sourceUrl = String.Empty;
			}

			this.sourceUrl = sourceUrl;
			this.buildResult = buildResult != null ? buildResult : this as IBuildResult;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the resource url
		/// </summary>
		public string SourceUrl
		{
			get { return this.sourceUrl; }
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
			internal set { this.attributes = value; }
		}

		/// <summary>
		/// Gets and sets if should render a debuggable ("Pretty-Print") reference.
		/// </summary>
		public bool IsDebug
		{
			get { return this.isDebug; }
			set { this.isDebug = value; }
		}

		/// <summary>
		/// Gets the referenced build result
		/// </summary>
		protected IBuildResult BuildResult
		{
			get { return this.buildResult; }
		}

		#endregion Properties

		#region Rendering Methods

		public abstract void Write(TextWriter writer);

		protected void WriteAttributes(TextWriter writer)
		{
			if (this.attributes == null || this.attributes.Count == 0)
			{
				return;
			}

			foreach (string key in this.attributes.Keys)
			{
				writer.Write(' ');
				HttpUtility.HtmlAttributeEncode(key.ToLowerInvariant(), writer);
				writer.Write("=\"");
				HttpUtility.HtmlAttributeEncode(this.attributes[key], writer);
				writer.Write('"');
			}
		}

		#endregion Rendering Methods

		#region Utility Methods

		public static ResourceBuildResult FindResource(string url)
		{
			bool isExternal = (url != null) && (url.IndexOf("://") > 0);

			object buildResult;
			if (isExternal)
			{
				buildResult = null;
			}
			else
			{
				buildResult = BuildManager.CreateInstanceFromVirtualPath(url, typeof(object));
			}

			ResourceBuildResult resource;

			if (buildResult is ResourceBuildResult)
			{
				resource = (ResourceBuildResult)buildResult;
			}
			else if (isExternal || buildResult is IBuildResult)
			{
				resource = new ScriptBuildResult(url, buildResult as IBuildResult);
			}
			else
			{
				throw new ArgumentException(String.Format(
					"Error loading resources for \"{0}\".\r\n"+
					"This can be caused by an invalid path, build errors, or incorrect configuration.\r\n"+
					"Check http://help.jsonfx.net/instructions for troubleshooting.",
					url));
			}

			return resource;
		}

		protected string ResolveUrl()
		{
			string url = ResourceHandler.GetResourceUrl(this.BuildResult, this.SourceUrl, this.IsDebug);

			if (url != null && url.StartsWith("~/"))
			{
				// resolve app relative URLs
				if (HttpRuntime.AppDomainAppVirtualPath.Length > 1)
				{
					url = HttpRuntime.AppDomainAppVirtualPath + url.TrimStart('~');
				}
				else
				{
					url = url.TrimStart('~');
				}
			}

			return url;
		}

		#endregion Utility Methods
	}
}
