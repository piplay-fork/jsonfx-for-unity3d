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

namespace JsonFx.JsonRpc
{
	/// <summary>
	/// Gets the help url for use in Json service description.
	/// </summary>
	public abstract class JsonDocsAttribute : JsonFx.Json.JsonNameAttribute
	{
		#region Fields

		private string helpUrl = null;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor.
		/// </summary>
		public JsonDocsAttribute()
		{
		}

		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="jsonName"></param>
		public JsonDocsAttribute(string jsonName) : base(jsonName)
		{
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the URL which points to help documentation
		/// </summary>
		public string HelpUrl
		{
			get { return this.helpUrl; }
			set { this.helpUrl = value; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Gets the help url for use in Json service description.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GetHelpUrl(object value)
		{
			if (value == null)
				return null;

			Type type = value.GetType();
			System.Reflection.MemberInfo memberInfo = null;

			if (type.IsEnum)
			{
				string name = Enum.GetName(type, value);
				if (String.IsNullOrEmpty(name))
					return null;
				memberInfo = type.GetField(name);
			}
			else
			{
				memberInfo = value as System.Reflection.MemberInfo;
			}

			if (memberInfo == null)
			{
				throw new NotImplementedException();
			}

			if (!JsonDocsAttribute.IsDefined(memberInfo, typeof(JsonDocsAttribute)))
				return null;
			JsonDocsAttribute attribute = (JsonDocsAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(JsonDocsAttribute));

			return attribute.HelpUrl;
		}

		#endregion Methods
	}
}
