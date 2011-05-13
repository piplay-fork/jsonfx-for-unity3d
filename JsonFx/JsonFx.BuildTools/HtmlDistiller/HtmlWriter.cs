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
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace JsonFx.BuildTools.HtmlDistiller.Writers
{
	public interface IHtmlWriter
	{
		#region Methods

		void WriteLiteral(string value);

		void WriteTag(HtmlTag tag);

		#endregion Methods
	}

	public interface IReversePeek
	{
		char PrevChar(int peek);
	}

	public class HtmlWriter : IHtmlWriter, IReversePeek, IDisposable
	{
		#region Fields

		private readonly TextWriter writer;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public HtmlWriter()
			: this((TextWriter)null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="writer">the underlying Stream</param>
		public HtmlWriter(Stream stream)
			: this((stream != null) ? new StreamWriter(stream) : null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="writer">the underlying TextWriter</param>
		public HtmlWriter(TextWriter writer)
		{
			this.writer = (writer != null) ?
				writer : new StringWriter();
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the underlying TextWriter.
		/// </summary>
		public TextWriter TextWriter
		{
			get { return this.writer; }
		}

		#endregion Properties

		#region IHtmlWriter Members

		public virtual void WriteLiteral(string value)
		{
			this.writer.Write(value);
		}

		/// <summary>
		/// Renders the tag to the output
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="filter"></param>
		/// <returns>true if rendered, false if not</returns>
		public virtual void WriteTag(HtmlTag tag)
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
					this.writer.Write('<');
					this.writer.Write(tag.RawName);

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
						this.writer.Write(" /");
					}
					this.writer.Write('>');
					break;
				}
				case HtmlTagType.EndTag:
				{
					this.writer.Write("</");
					this.writer.Write(tag.RawName);
					this.writer.Write('>');
					break;
				}
			}
		}

		#endregion IHtmlWriter Members

		#region Methods

		private void WriteUnparsedTag(HtmlTag tag)
		{
			this.writer.Write('<');
			this.writer.Write(tag.RawName);
			this.writer.Write(tag.Content);
			this.writer.Write(tag.EndDelim);
			this.writer.Write('>');
		}

		/// <summary>
		/// Renders the style property
		/// </summary>
		/// <param name="tag"></param>
		private void WriteAttributes(HtmlTag tag)
		{
			foreach (KeyValuePair<string, object> attribute in tag.FilteredAttributes)
			{
				if (attribute.Value is HtmlTag)
				{
					// HTML doesn't allow tags in attributes unlike code markup
					continue;
				}
				string value = attribute.Value as string;

				this.writer.Write(' ');
				if (String.IsNullOrEmpty(value))
				{
					HtmlDistiller.HtmlAttributeEncode(attribute.Key, this.writer);
				}
				else if (String.IsNullOrEmpty(attribute.Key))
				{
					HtmlDistiller.HtmlAttributeEncode(value, this.writer);
				}
				else
				{
					HtmlDistiller.HtmlAttributeEncode(attribute.Key, this.writer);
					this.writer.Write("=\"");
					HtmlDistiller.HtmlAttributeEncode(value, this.writer);
					this.writer.Write("\"");
				}
			}
		}

		/// <summary>
		/// Renders the style property
		/// </summary>
		/// <param name="output"></param>
		private void WriteStyles(HtmlTag tag)
		{
			this.writer.Write(" style=\"");

			foreach (KeyValuePair<string, string> style in tag.FilteredStyles)
			{
				HtmlDistiller.HtmlAttributeEncode(style.Key, this.writer);
				this.writer.Write(':');
				HtmlDistiller.HtmlAttributeEncode(style.Value, this.writer);
				this.writer.Write(';');
			}

			this.writer.Write('\"');
		}

		#endregion Methods

		#region Object Overrides

		/// <summary>
		/// Returns a System.String that represents the current TextWriter.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.TextWriter.ToString();
		}

		#endregion Object Overrides

		#region IDisposable Members

		/// <summary>
		/// Releases all resources used by the System.IO.TextWriter object.
		/// </summary>
		void IDisposable.Dispose()
		{
			this.writer.Flush();
			this.writer.Close();
			this.writer.Dispose();
		}

		#endregion IDisposable Members

		#region IReversePeek Members

		char IReversePeek.PrevChar(int peek)
		{
			// TODO: determine if there is a better way for this

			if (!(this.writer is StringWriter))
			{
				return HtmlDistiller.NullChar;
			}

			StringBuilder builder = ((StringWriter)this.writer).GetStringBuilder();

			if (builder.Length < peek)
			{
				return HtmlDistiller.NullChar;
			}

			return builder[builder.Length-peek];
		}

		#endregion IReversePeek Members
	}
}
