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
using System.Reflection;

using JsonFx.Json;

namespace JsonFx.JsonRpc.Discovery
{
	public class JsonParameterDescription : JsonDescriptionBase
	{
		#region Fields

		private JsonParameterType type = JsonParameterType.Any;

		#endregion Fields

		#region Init

		public JsonParameterDescription() { }

		/// <summary>
		/// Ctor.
		/// </summary>
		internal JsonParameterDescription(ParameterInfo param)
		{
			if (param == null)
				return;

			this.Type = this.GetJsonParameterType(param.ParameterType);
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets a summary of the purpose of the service.
		/// </summary>
		/// <remarks>
		/// OPTIONAL. A String value that denotes the expected value type for the
		/// parameter. If this member is not supplied or is the Null value then
		/// the type is defined "any".
		/// </remarks>
		[JsonName("type")]
		public JsonParameterType Type
		{
			get { return this.type; }
			set { this.type = value; }
		}

		#endregion Properties

		#region Methods

		protected internal JsonParameterType GetJsonParameterType(Type type)
		{
			if (type == null)
				return JsonParameterType.None;

			if (type.IsEnum)
				return JsonParameterType.String;

			if (type.IsSubclassOf(typeof(System.Collections.IEnumerable)))
				return JsonParameterType.Array;

			switch (type.FullName)
			{
				case "System.String":
				case "System.Char":
				{
					return JsonParameterType.String;
				}
				case "System.Double":
				case "System.Single":
				case "System.Decimal":
				case "System.Int16":
				case "System.Int32":
				case "System.Int64":
				case "System.UInt16":
				case "System.UInt32":
				case "System.UInt64":
				case "System.Byte":
				case "System.SByte":
				{
					return JsonParameterType.Number;
				}
				case "System.Object":
				{
					return JsonParameterType.Any;
				}
				case "System.Boolean":
				{
					return JsonParameterType.Boolean;
				}
				case "System.Void":
				{
					return JsonParameterType.None;
				}
				default:
				{
					return JsonParameterType.Object;
				}
			}
		}

		#endregion Methods
	}
}
