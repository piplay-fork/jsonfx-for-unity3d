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

using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Internal base representation of JBST commands
	/// </summary>
	internal abstract class JbstCommandBase : JbstContainerControl, IJsonSerializable
	{
		#region Constants

		public const string JbstPrefix = "jbst"+JbstWriter.PrefixDelim;

		private const string ControlNameKey = "name";
		private const string ControlNameKeyAlt = JbstPrefix+ControlNameKey;
		private const string ControlDataKey = "data";
		private const string ControlDataKeyAlt = JbstPrefix+ControlDataKey;
		private const string ControlIndexKey = "index";
		private const string ControlIndexKeyAlt = JbstPrefix+ControlIndexKey;
		private const string ControlCountKey = "count";
		private const string ControlCountKeyAlt = JbstPrefix+ControlIndexKey;

		private const string DefaultDataExpression = "this."+ControlDataKey;
		private const string DefaultIndexExpression = "this."+ControlIndexKey;
		private const string DefaultCountExpression = "this."+ControlCountKey;

		private const string FunctionEvalExpression = "({0}).call(this)";

		#endregion Constants

		#region Fields

		private readonly string commandType;

		private bool isProcessed;
		private string nameExpr;
		private string dataExpr;
		private string indexExpr;
		private string countExpr;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="commandName"></param>
		public JbstCommandBase(string commandName)
		{
			this.commandType = (commandName == null) ?
				String.Empty :
				commandName.ToLowerInvariant();
		}

		#endregion Init

		#region Properties

		[JsonName("rawName")]
		public override string RawName
		{
			get { return JbstCommandBase.JbstPrefix + this.commandType; }
		}

		protected internal string NameExpr
		{
			get
			{
				this.EnsureControl();

				return this.nameExpr;
			}
		}

		protected string DataExpr
		{
			get
			{
				this.EnsureControl();

				return this.dataExpr;
			}
		}

		protected string IndexExpr
		{
			get
			{
				this.EnsureControl();

				return this.indexExpr;
			}
		}

		protected string CountExpr
		{
			get
			{
				this.EnsureControl();

				return this.countExpr;
			}
		}

		#endregion Properties

		#region Methods

		protected abstract void Render(JsonWriter writer);

		private void EnsureControl()
		{
			if (this.isProcessed)
			{
				return;
			}

			this.nameExpr = this.ProcessArgument(String.Empty, ControlNameKey, ControlNameKeyAlt);
			//this.nameExpr = EcmaScriptIdentifier.EnsureValidIdentifier(this.nameExpr, true, false);

			this.dataExpr = this.ProcessArgument(DefaultDataExpression, ControlDataKey, ControlDataKeyAlt);
			this.indexExpr = this.ProcessArgument(DefaultIndexExpression, ControlIndexKey, ControlIndexKeyAlt);
			this.countExpr = this.ProcessArgument(DefaultCountExpression, ControlCountKey, ControlCountKeyAlt);

			this.Attributes.Clear();
			this.isProcessed = true;
		}

		/// <summary>
		/// Processes each argument allowing string literals to code expressions to function calls.
		/// </summary>
		/// <param name="defaultValue">the default value if none was supplied</param>
		/// <param name="keys">an ordered list of keys to check</param>
		/// <returns>the resulting expression</returns>
		protected string ProcessArgument(string defaultValue, params string[] keys)
		{
			object argument = null;
			foreach (string key in keys)
			{
				if (this.Attributes.ContainsKey(key))
				{
					argument = this.Attributes[key];
					break;
				}
			}

			string value;
			if (argument == null)
			{
				value = defaultValue;
			}
			else if (argument is string)
			{
				// directly use as inline expression
				value = (string)argument;
			}
			else if (argument is JbstExpressionBlock)
			{
				// convert to inline expression
				value = ((JbstExpressionBlock)argument).Code;
			}
			else if (argument is JbstCodeBlock)
			{
				// convert to anonymous function expression
				value = String.Format(
					FunctionEvalExpression,
					EcmaScriptWriter.Serialize(argument));
			}
			else
			{
				// convert to literal expression
				value = EcmaScriptWriter.Serialize(argument);
			}

			return (value ?? String.Empty).Trim();
		}

		#endregion Methods

		#region IJsonSerializable Members

		void IJsonSerializable.WriteJson(JsonWriter writer)
		{
			this.EnsureControl();

			this.Render(writer);
		}

		void IJsonSerializable.ReadJson(JsonReader reader)
		{
			throw new NotImplementedException("JBST deserialization is not implemented.");
		}

		#endregion IJsonSerializable Members

		#region Enumerable Adapter

		/// <summary>
		/// A simple adapter for exposing the IEnumerable interface without exposing the IJsonSerializable interface
		/// </summary>
		/// <remarks>
		/// In order to wrap the output of the JbstControl IJsonSerializable was required, but this takes
		/// precedent over the IEnumerable interface which is what should be rendered inside the wrapper.
		/// </remarks>
		protected class EnumerableAdapter : IEnumerable
		{
			#region Fields

			private readonly IEnumerable enumerable;

			#endregion Fields

			#region Init

			/// <summary>
			/// Ctor
			/// </summary>
			/// <param name="enumerable"></param>
			public EnumerableAdapter(IEnumerable enumerable)
			{
				this.enumerable = enumerable;
			}

			#endregion Init

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.enumerable.GetEnumerator();
			}

			#endregion IEnumerable Members
		}

		#endregion Enumerable Adapter
	}
}
