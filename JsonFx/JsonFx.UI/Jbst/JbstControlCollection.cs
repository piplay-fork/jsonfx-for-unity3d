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
using System.Collections;
using System.Collections.Generic;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Control collection for JBST nodes.
	/// </summary>
	internal class JbstControlCollection : ICollection<JbstControl>
	{
		#region Fields

		private JbstContainerControl owner;
		private List<JbstControl> controls = new List<JbstControl>();
		private List<JbstInline> inlineTemplates;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="owner"></param>
		public JbstControlCollection(JbstContainerControl owner)
		{
			this.owner = owner;
		}

		#endregion Init

		#region Properties

		public JbstContainerControl Owner
		{
			get { return this.owner; }
		}

		public JbstControl this[int index]
		{
			get { return this.controls[index]; }
			set { this.controls[index] = value; }
		}

		public IList<JbstInline> InlineTemplates
		{
			get
			{
				if (this.inlineTemplates == null)
				{
					this.inlineTemplates = new List<JbstInline>();
				}
				return this.inlineTemplates;
			}
		}

		public bool InlineTemplatesSpecified
		{
			get { return (this.inlineTemplates != null && this.inlineTemplates.Count > 0); }
		}

		protected JbstControl Last
		{
			get
			{
				if (this.controls.Count < 1)
				{
					return null;
				}

				return this.controls[this.controls.Count-1];
			}
		}

		public bool HasAnonymousInlineTemplate
		{
			get
			{
				if (this.controls.Count == 1)
				{
					// exclude templates which are entirely whitespace
					// this happens with just whitespace between named templates
					JbstLiteral literal = this.controls[0] as JbstLiteral;
					return (literal == null) || !literal.IsWhitespace;
				}

				return (this.controls.Count > 0);
			}
		}

		#endregion Properties

		#region ICollection<JbstControlBase> Members

		public void Add(JbstControl item)
		{
			if (item is JbstLiteral)
			{
				// combine contiguous literals into single for reduced space and processing
				JbstLiteral literal = this.Last as JbstLiteral;
				if (literal != null)
				{
					literal.Text += ((JbstLiteral)item).Text;
				}
				else
				{
					this.controls.Add(item);
				}
			}
			else if (item is JbstInline)
			{
				this.InlineTemplates.Add((JbstInline)item);
			}
			else
			{
				this.controls.Add(item);
			}

			item.Parent = this.Owner;
		}

		public void AddRange(IEnumerable<JbstControl> collection)
		{
			foreach (JbstControl item in collection)
			{
				this.Add(item);
			}
		}

		public void Clear()
		{
			if (this.InlineTemplatesSpecified)
			{
				this.inlineTemplates.Clear();
			}

			this.controls.Clear();
		}

		bool ICollection<JbstControl>.Contains(JbstControl item)
		{
			if (this.InlineTemplatesSpecified && item is JbstInline)
			{
				return this.InlineTemplates.Contains((JbstInline)item);
			}

			return this.controls.Contains(item);
		}

		void ICollection<JbstControl>.CopyTo(JbstControl[] array, int arrayIndex)
		{
			this.controls.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return this.controls.Count; }
		}

		bool ICollection<JbstControl>.IsReadOnly
		{
			get { return ((ICollection<JbstControl>)this.controls).IsReadOnly; }
		}

		bool ICollection<JbstControl>.Remove(JbstControl item)
		{
			return this.Remove(item);
		}

		public bool Remove(JbstControl item)
		{
			if (this.InlineTemplatesSpecified && item is JbstInline)
			{
				return this.InlineTemplates.Remove((JbstInline)item);
			}

			return this.controls.Remove(item);
		}

		#endregion ICollection<JbstControlBase> Members

		#region IEnumerable<JbstControlBase> Members

		IEnumerator<JbstControl> IEnumerable<JbstControl>.GetEnumerator()
		{
			return this.controls.GetEnumerator();
		}

		#endregion IEnumerable<JbstControlBase> Members

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.controls.GetEnumerator();
		}

		#endregion IEnumerable Members
	}
}
