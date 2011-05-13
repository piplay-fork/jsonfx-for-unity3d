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

using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Internal representation of a JBST element.
	/// </summary>
	internal class JbstContainerControl : JbstControl, IEnumerable
	{
		#region Fields

		private string prefix;
		private string tagName;
		private IDictionary<String, Object> attributes;
		private JbstControlCollection childControls;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public JbstContainerControl()
			: this(String.Empty, String.Empty)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="tagName"></param>
		public JbstContainerControl(string prefix, string tagName)
		{
			if (tagName == null)
			{
				tagName = String.Empty;
			}

			this.prefix = prefix;
			this.tagName = tagName;
		}

		#endregion Init

		#region Properties

		[JsonName("tagName")]
		public string TagName
		{
			get { return this.tagName; }
			set { this.tagName = value; }
		}

		[JsonName("prefix")]
		public string Prefix
		{
			get { return this.prefix; }
			set { this.prefix = value; }
		}

		[JsonName("rawName")]
		public virtual string RawName
		{
			get
			{
				if (String.IsNullOrEmpty(this.prefix))
				{
					return this.TagName;
				}
				return this.Prefix + this.TagName;
			}
		}

		[JsonName("attributes")]
		[JsonSpecifiedProperty("AttributesSpecified")]
		public IDictionary<String, Object> Attributes
		{
			get
			{
				if (this.attributes == null)
				{
					this.attributes = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
				}
				return this.attributes;
			}
			set { this.attributes = value; }
		}

		[JsonIgnore]
		public bool AttributesSpecified
		{
			get { return (this.attributes != null) && (this.attributes.Keys.Count > 0); }
			set { }
		}

		[JsonName("children")]
		[JsonSpecifiedProperty("ChildControlsSpecified")]
		public virtual JbstControlCollection ChildControls
		{
			get
			{
				if (this.childControls == null)
				{
					this.childControls = new JbstControlCollection(this);
				}
				return this.childControls;
			}
			set { this.childControls = value; }
		}

		[JsonIgnore]
		public bool ChildControlsSpecified
		{
			get { return this.childControls != null && this.childControls.Count > 0; }
			set { }
		}

		#endregion Properties

		#region IEnumerable Members

		/// <summary>
		/// Enumerates the control as JsonML.
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			yield return this.TagName;

			if (this.AttributesSpecified)
			{
				yield return this.Attributes;
			}

			if (this.ChildControlsSpecified)
			{
				foreach (JbstControl childControl in this.ChildControls)
				{
					yield return childControl;
				}
			}
		}

		#endregion IEnumerable Members
	}
}
