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
using System.Text;
using System.Collections.Generic;

using JsonFx.BuildTools.HtmlDistiller.Filters;
using JsonFx.BuildTools.HtmlDistiller.Writers;

namespace JsonFx.BuildTools.HtmlDistiller
{
	#region Enums

	/// <summary>
	/// Defines the type of tag
	/// </summary>
	public enum HtmlTagType
	{
		/// <summary>
		/// Not defined
		/// </summary>
		Unknown,

		/// <summary>
		/// Unparsed block
		/// </summary>
		Unparsed,

		/// <summary>
		/// Opening tag
		/// </summary>
		BeginTag,

		/// <summary>
		/// Closing tag
		/// </summary>
		EndTag,

		/// <summary>
		/// Empty tag
		/// </summary>
		FullTag
	}

	/// <summary>
	/// Defines a general taxonomy of tags
	/// </summary>
	/// <remarks>
	/// http://www.w3.org/TR/xhtml-modularization/abstract_modules.html#sec_5.2.
	/// </remarks>
	[Flags]
	public enum HtmlTaxonomy
	{
		/// <summary>
		/// Plain text, no tag
		/// </summary>
		None = 0x0000,

		/// <summary>
		/// HTML comments
		/// </summary>
		Comment = 0x0001,

		/// <summary>
		/// textual elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/text.html
		/// </remarks>
		Text = 0x0002,

		/// <summary>
		/// character level elements and text strings
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/text.html
		/// </remarks>
		Inline = 0x0004,

		/// <summary>
		/// block-like elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/text.html
		/// </remarks>
		Block = 0x0008,

		/// <summary>
		/// list elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/lists.html
		/// </remarks>
		List = 0x0010,

		/// <summary>
		/// tabular elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/tables.html
		/// </remarks>
		Table = 0x0020,

		/// <summary>
		/// style elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/present/styles.html
		/// http://www.w3.org/TR/html401/present/graphics.html
		/// http://www.w3.org/TR/xhtml-modularization/abstract_modules.html#s_presentationmodule
		/// </remarks>
		Style = 0x0040,

		/// <summary>
		/// form elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/xhtml-modularization/abstract_modules.html#s_forms
		/// </remarks>
		Form = 0x0080,

		/// <summary>
		/// script elements
		/// </summary>
		Script = 0x0100,

		/// <summary>
		/// embedded elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/objects.html
		/// </remarks>
		Embeded = 0x0200,

		/// <summary>
		/// document elements
		/// </summary>
		/// <remarks>
		/// http://www.w3.org/TR/html401/struct/global.html
		/// </remarks>
		Document = 0x0400,

		/// <summary>
		/// unknown elements
		/// </summary>
		Unknown = 0x8000
	}

	#endregion Enums

	/// <summary>
	/// Represents an HTML/XHTML tag
	/// </summary>
	/// <remarks>
	/// http://www.w3.org/TR/html401/
	/// http://www.w3.org/TR/xhtml1/
	/// </remarks>
	public sealed class HtmlTag
	{
		#region Constants

		private const int DefaultAttributeCapacity = 3;
		private const int DefaultStyleCapacity = 3;
		internal const string StyleAttrib = "style";

		#endregion Constants

		#region Fields

		private readonly IHtmlFilter filter;
		private HtmlTagType tagType = HtmlTagType.Unknown;
		private HtmlTaxonomy taxonomy = HtmlTaxonomy.None;
		private readonly string rawName;
		private string tagName;
		private Dictionary<string, object> attributes;
		private Dictionary<string, string> styles;
		private string unparsedContent;
		private string endDelim;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="name"></param>
		public HtmlTag(string name, IHtmlFilter filter)
		{
			this.filter = filter;
			if (name == null)
			{
				name = String.Empty;
			}
			this.rawName = name.Trim();

			if (this.rawName.StartsWith("!") ||
				this.rawName.StartsWith("?") ||
				this.rawName.StartsWith("%"))
			{
				this.tagType = HtmlTagType.Unparsed;
			}
			else if (this.rawName.StartsWith("/"))
			{
				this.tagType = HtmlTagType.EndTag;
				this.rawName = this.rawName.Substring(1);
			}
			else if (HtmlTag.FullTagRequired(this.TagName)) // this.TagName is lowercase
			{
				this.tagType = HtmlTagType.FullTag;
			}
			else
			{
				this.tagType = HtmlTagType.BeginTag;
			}
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the tag type
		/// </summary>
		public HtmlTagType TagType
		{
			get { return this.tagType; }
		}

		/// <summary>
		/// Gets the HTML taxonomy for the tag
		/// </summary>
		public HtmlTaxonomy Taxonomy
		{
			get
			{
				if (this.taxonomy == HtmlTaxonomy.None)
				{
					this.taxonomy = HtmlTag.GetTaxonomy(this.TagName);
				}
				return this.taxonomy;
			}
		}

		/// <summary>
		/// Gets the tag name
		/// </summary>
		/// <remarks>
		/// Note: always lowercase
		/// </remarks>
		public string TagName
		{
			get
			{
				if (this.tagName == null)
				{
					this.tagName = this.rawName.ToLowerInvariant();
				}
				return this.tagName;
			}
		}

		/// <summary>
		/// Gets the tag name in its original case
		/// </summary>
		public string RawName
		{
			get { return this.rawName; }
		}

		/// <summary>
		/// Gets the collection of HTML attributes
		/// </summary>
		/// <remarks>
		/// Note: allocates space for attributes as a side effect
		/// </remarks>
		public Dictionary<string, object> Attributes
		{
			get
			{
				if (this.attributes == null)
				{
					this.attributes = new Dictionary<string, object>(HtmlTag.DefaultAttributeCapacity, StringComparer.OrdinalIgnoreCase);
				}
				return this.attributes;
			}
		}

		/// <summary>
		/// Tests whether any attributes exist
		/// </summary>
		/// <remarks>
		/// Note: does NOT allocate space for attributes as a side effect
		/// </remarks>
		public bool HasAttributes
		{
			get { return (this.attributes != null && this.attributes.Count > 0); }
		}

		/// <summary>
		/// Gets the collection of CSS styles
		/// </summary>
		/// <remarks>
		/// Note: allocates space for styles as a side effect
		/// </remarks>
		public Dictionary<string, string> Styles
		{
			get
			{
				if (this.styles == null)
				{
					this.styles = new Dictionary<string, string>(HtmlTag.DefaultStyleCapacity, StringComparer.Ordinal);
				}
				return this.styles;
			}
		}

		/// <summary>
		/// Tests whether any styles exist
		/// </summary>
		/// <remarks>
		/// Note: does NOT allocate space for styles as a side effect
		/// </remarks>
		public bool HasStyles
		{
			get
			{
				if (this.HasAttributes &&
					this.Attributes.ContainsKey(HtmlTag.StyleAttrib) &&
					this.Attributes[HtmlTag.StyleAttrib] != null)
				{
					return true;
				}
				return (this.styles != null && this.styles.Count > 0);
			}
		}

		/// <summary>
		/// Gets the content of unparsed blocks (e.g. comments)
		/// </summary>
		public string Content
		{
			get
			{
				if (this.unparsedContent == null)
				{
					return String.Empty;
				}
				return this.unparsedContent;
			}
			set { this.unparsedContent = value; }
		}

		/// <summary>
		/// Gets the end delimiter of unparsed blocks (e.g. comments)
		/// </summary>
		public string EndDelim
		{
			get
			{
				if (this.endDelim == null)
				{
					return String.Empty;
				}
				return this.endDelim;
			}
			set { this.endDelim = value; }
		}

		/// <summary>
		/// Gets a sequence of attributes which have been filtered by the IHtmlFilter
		/// </summary>
		public IEnumerable<KeyValuePair<string, object>> FilteredAttributes
		{
			get
			{
				if (!this.HasAttributes)
				{
					yield break;
				}

				foreach (string key in this.Attributes.Keys)
				{
					object value = this.Attributes[key];
					if (value is HtmlTag)
					{
						if (this.filter != null && !this.filter.FilterTag((HtmlTag)value))
						{
							continue;
						}
					}
					else
					{
						string strVal = value as string;
						if (this.filter != null && !this.filter.FilterAttribute(this.TagName, key, ref strVal))
						{
							continue;
						}
						value = strVal;
					}

					yield return new KeyValuePair<string, object>(key, value);
				}
			}
		}

		/// <summary>
		/// Gets a sequence of attributes which have been filtered by the IHtmlFilter
		/// </summary>
		public IEnumerable<KeyValuePair<string, string>> FilteredStyles
		{
			get
			{
				if (!this.HasStyles)
				{
					yield break;
				}

				foreach (string key in this.Styles.Keys)
				{
					string value = this.Styles[key];
					if (this.filter == null || this.filter.FilterStyle(this.TagName, key, ref value))
					{
						if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(value))
						{
							continue;
						}

						yield return new KeyValuePair<string, string>(key, value);
					}
				}
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Changes a BeginTag to a FullTag
		/// </summary>
		internal void SetFullTag()
		{
			if (this.TagType != HtmlTagType.BeginTag)
			{
				return;
			}

			this.tagType = HtmlTagType.FullTag;
		}

		/// <summary>
		/// Generates a closing tag which matches this tag
		/// </summary>
		/// <returns></returns>
		public HtmlTag CreateCloseTag()
		{
			if (this.TagType != HtmlTagType.BeginTag &&
				HtmlTag.CloseTagRequired(this.TagName))
			{
				return null;
			}

			return new HtmlTag('/'+this.rawName, this.filter);
		}

		/// <summary>
		/// Generates an open tag which matches this tag
		/// </summary>
		/// <returns></returns>
		public HtmlTag CreateOpenTag()
		{
			if (this.TagType != HtmlTagType.EndTag &&
				HtmlTag.CloseTagRequired(this.TagName))
			{
				return null;
			}

			return new HtmlTag(this.rawName, this.filter);
		}

		#endregion Methods

		#region Object Overrides

		/// <summary>
		/// Renders the tag without filtering.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			using (HtmlWriter writer = new HtmlWriter())
			{
				writer.WriteTag(this);
				return writer.ToString();
			}
		}

		public override bool Equals(object obj)
		{
			HtmlTag that = obj as HtmlTag;
			if (that == null)
			{
				return base.Equals(obj);
			}
			return this.rawName.Equals(that.rawName) && this.tagType.Equals(that.tagType);
		}

		public override int GetHashCode()
		{
			int hashcode = this.rawName.GetHashCode();

			if (this.HasAttributes)
			{
				hashcode ^= this.attributes.GetHashCode();
			}

			if (this.HasStyles)
			{
				hashcode ^= this.styles.GetHashCode();
			}

			return hashcode;
		}

		#endregion Object Overrides

		#region Static Methods

		/// <summary>
		/// Determines if is full (i.e. empty) tag
		/// </summary>
		/// <param name="tag">lowercase tag name</param>
		/// <returns>if is a full tag</returns>
		/// <remarks>
		/// http://www.w3.org/TR/html401/index/elements.html
		/// http://www.w3.org/TR/xhtml-modularization/abstract_modules.html#sec_5.2.
		/// http://www.w3.org/TR/WD-html40-970917/index/elements.html
		/// </remarks>
		private static bool FullTagRequired(string tag)
		{
			switch (tag)
			{
				case "area":
				case "base":
				case "basefont":
				case "br":
				case "col":
				case "frame":
				case "hr":
				case "img":
				case "input":
				case "isindex":
				case "link":
				case "meta":
				case "param":
				case "wbr":
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines if the tag is required to be closed (in HTML 4.01)
		/// </summary>
		/// <param name="tag">lowercase tag name</param>
		/// <returns></returns>
		/// <remarks>
		/// http://www.w3.org/TR/html401/index/elements.html
		/// http://www.w3.org/TR/WD-html40-970917/index/elements.html
		/// </remarks>
		private static bool CloseTagRequired(string tag)
		{
			switch (tag)
			{
				case "body":
				case "colgroup":
				case "dd":
				case "dt":
				case "embed":
				case "head":
				case "html":
				case "li":
				case "option":
				case "p":
				case "tbody":
				case "td":
				case "tfoot":
				case "th":
				case "thead":
				case "tr":
				case "!--":
				case "![CDATA[":
				case "!":
				case "?":
				case "%":
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tag">lowercase tag name</param>
		/// <returns>the box type for a particular element</returns>
		private static HtmlTaxonomy GetTaxonomy(string tag)
		{
			// http://www.w3.org/TR/html401/
			// http://www.w3.org/TR/xhtml-modularization/abstract_modules.html
			// http://www.w3.org/html/wg/html5/#elements0
			// non-standard: http://www.mountaindragon.com/html/text.htm
			switch (tag)
			{
				case "!--":
				{
					return HtmlTaxonomy.Comment;
				}

				case "a":
				case "abbr":
				case "acronym":
				case "address":
				case "area":
				case "bdo":
				case "cite":
				case "code":
				case "dfn":
				case "em":
				case "img":
				case "isindex":
				case "kbd":
				case "label":
				case "legend":
				case "map":
				case "q":
				case "samp":
				case "span":
				case "strong":
				case "var":
				case "wbr":
				{
					return HtmlTaxonomy.Text|HtmlTaxonomy.Inline;
				}

				case "b":
				case "big":
				case "blink":
				case "font":
				case "i":
				case "marquee":
				case "s":
				case "small":
				case "strike":
				case "sub":
				case "sup":
				case "tt":
				case "u":
				{
					return HtmlTaxonomy.Text|HtmlTaxonomy.Style|HtmlTaxonomy.Inline;
				}

				case "blockquote":
				case "bq":
				case "br":
				case "center":
				case "del":
				case "div":
				case "fieldset":
				case "h1":
				case "h2":
				case "h3":
				case "h4":
				case "h5":
				case "h6":
				case "hr":
				case "ins":
				case "nobr":
				case "p":
				case "pre":
				{
					return HtmlTaxonomy.Text|HtmlTaxonomy.Block;
				}

				case "dl":
				case "dd":
				case "dir":
				case "dt":
				case "lh":
				case "li":
				case "menu":
				case "ol":
				case "ul":
				{
					return HtmlTaxonomy.List;
				}

				case "table":
				case "tbody":
				case "td":
				case "th":
				case "thead":
				case "tfoot":
				case "tr":
				case "caption":
				case "col":
				case "colgroup":
				{
					return HtmlTaxonomy.Table;
				}

				case "button":
				case "form":
				case "input":
				case "optgroup":
				case "option":
				case "select":
				case "textarea":
				{
					return HtmlTaxonomy.Form;
				}

				case "applet":
				case "bgsound":
				case "embed":
				case "noembed":
				case "object":
				case "param":
				case "sound":
				{
					return HtmlTaxonomy.Embeded;
				}

				case "basefont":
				case "style":
				{
					return HtmlTaxonomy.Style|HtmlTaxonomy.Document;
				}

				case "%":
				case "noscript":
				case "script":
				{
					return HtmlTaxonomy.Script|HtmlTaxonomy.Document;
				}

				case "!":
				case "?":
				case "![cdata[":
				case "base":
				case "body":
				case "head":
				case "html":
				case "frameset":
				case "frame":
				case "iframe":
				case "link":
				case "meta":
				case "noframes":
				case "title":
				{
					return HtmlTaxonomy.Document;
				}
			}
			return HtmlTaxonomy.Unknown;
		}

		#endregion Static Methods
	}
}
