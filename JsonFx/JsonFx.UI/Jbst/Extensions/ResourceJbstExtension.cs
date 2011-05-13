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
using System.Web.Compilation;

using JsonFx.Json;
using JsonFx.Handlers;

namespace JsonFx.UI.Jbst.Extensions
{
	public class ResourceJbstExtension : JbstExtension
	{
		#region Constants

		private const string ResourceLookupFormat =
			@"function() {{
				return JsonFx.Lang.get({0});
			}}";

		#endregion Constants

		#region Fields

		private readonly ResourceExpressionFields ResKey;
		private string globalizationKey = null;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="value"></param>
		/// <param name="path"></param>
		protected internal ResourceJbstExtension(string value, string path)
			: base(value, path)
		{
			this.ResKey = ResourceExpressionBuilder.ParseExpression(this.Value);
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the resource key for this expression
		/// </summary>
		public string GlobalizationKey
		{
			get
			{
				if (this.globalizationKey == null)
				{
					this.globalizationKey = GlobalizedResourceHandler.GetKey(this.ResKey, this.Path);
				}
				return this.globalizationKey;
			}
		}

		#endregion Properties

		#region JbstExtension Members

		protected internal override string Eval()
		{
			if (this.ResKey == null)
			{
				return String.Empty;
			}

			string key = this.GlobalizationKey;

			return String.Format(
				ResourceLookupFormat,
				JsonWriter.Serialize(key));
		}

		#endregion JbstExtension Members
	}
}
