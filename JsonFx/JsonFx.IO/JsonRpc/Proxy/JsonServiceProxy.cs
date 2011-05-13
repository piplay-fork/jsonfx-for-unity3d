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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using JsonFx.Json;
using JsonFx.JsonRpc.Discovery;

namespace JsonFx.JsonRpc.Proxy
{
	/// <summary>
	/// Generates a JavaScript proxy class for communicating with a JsonService.
	/// </summary>
	internal class JsonServiceProxyGenerator
	{
		#region Constants

		private readonly object SyncLock = new object();

		#endregion Constants

		#region Fields

		private JsonServiceProxyFormat formatter = null;
		private readonly JsonServiceDescription service;
		private readonly string serviceProxyName = null;

		#endregion Fields

		#region Init

		internal JsonServiceProxyGenerator(JsonServiceDescription service)
		{
			this.service = service;
			if (!String.IsNullOrEmpty(service.Namespace))
			{
				this.serviceProxyName = service.Namespace+'.'+service.Name;
			}
		}

		#endregion Init

		#region Properties

		public JsonServiceDescription Service
		{
			get { return this.service; }
		}

		public string ProxyNamespace
		{
			get { return this.serviceProxyName; }
		}

		#endregion Properties

		#region Public Methods

		public string OutputProxy(bool prettyPrint)
		{
			using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
			{
				this.OutputProxy(writer, prettyPrint);
				return writer.GetStringBuilder().ToString();
			}
		}

		public void OutputProxy(Stream output, bool prettyPrint)
		{
			using (TextWriter writer = new StreamWriter(output, Encoding.UTF8))
			{
				this.OutputProxy(writer, prettyPrint);
			}
		}

		public void OutputProxy(TextWriter writer, bool prettyPrint)
		{
			lock (this.SyncLock)
			{
				// locking because changing Formatter based upon debug switch

				if (prettyPrint)
				{
					this.formatter = new DebugJsonServiceProxyFormat();
				}
				else
				{
					this.formatter = new JsonServiceProxyFormat();
				}

				this.WriteNamespaces(writer);

				writer.Write(this.formatter.ProxyInstantiationFormat, this.ProxyNamespace, EcmaScriptWriter.Serialize(this.service.Address));

				foreach (JsonMethodDescription method in this.Service.Methods)
				{
					this.WriteMethod(writer, method);
				}

				if (prettyPrint)
				{
					this.WriteProperty(writer, "isDebug", true);
				}
			}
		}

		#endregion Public Methods

		#region Methods

		private void WriteNamespaces(TextWriter writer)
		{
			writer.Write(this.formatter.GlobalsFormat);

			if (!String.IsNullOrEmpty(this.ProxyNamespace))
			{
				EcmaScriptWriter.WriteNamespaceDeclaration(writer, this.ProxyNamespace, null, this.formatter.IsDebug);
			}
		}

		private void WriteProperty(TextWriter writer, string name, object value)
		{
			if (EcmaScriptIdentifier.IsValidIdentifier(name, false))
			{
				writer.Write(this.formatter.PropertyFormat, this.ProxyNamespace, name, EcmaScriptWriter.Serialize(value));
			}
			else
			{
				writer.Write(this.formatter.SafePropertyFormat, this.ProxyNamespace, EcmaScriptWriter.Serialize(name), EcmaScriptWriter.Serialize(value));
			}
		}

		private void WriteMethod(TextWriter writer, JsonMethodDescription method)
		{
			if (EcmaScriptIdentifier.IsValidIdentifier(method.Name, false))
			{
				writer.Write(this.formatter.MethodBeginFormat, this.ProxyNamespace, method.Name, this.ConvertParamType(method.Return.Type));
			}
			else
			{
				writer.Write(this.formatter.SafeMethodBeginFormat, this.ProxyNamespace, EcmaScriptWriter.Serialize(method.Name), this.ConvertParamType(method.Return.Type));
			}

			foreach (JsonNamedParameterDescription param in method.Params)
			{
				this.WriteParameter(writer, param);
			}

			writer.Write(this.formatter.MethodMiddleFormat, EcmaScriptWriter.Serialize(method.Name));

			if (method.Params.Length > 0)
			{
				string[] args = new string[method.Params.Length];
				for (int i=0; i<method.Params.Length; i++)
				{
					args[i] = method.Params[i].Name;
				}
				writer.Write(this.formatter.ArgsFormat, String.Join(",", args));
			}
			else
			{
				writer.Write("null");
			}

			writer.Write(this.formatter.MethodEndFormat);
		}

		private void WriteParameter(TextWriter writer, JsonNamedParameterDescription param)
		{
			string paramType = this.ConvertParamType(param.Type);
			writer.Write(this.formatter.ParamFormat, param.Name, paramType);
		}

		private string ConvertParamType(JsonParameterType paramType)
		{
			switch (paramType)
			{
				case JsonParameterType.Any:
				{
					return "object";
				}
				case JsonParameterType.None:
				{
					return "void";
				}
				default:
				{
					return paramType.ToString().ToLowerInvariant();
				}
			}
		}

		#endregion Methods
	}

	internal class JsonServiceProxyFormat
	{
		#region Properties

		public virtual bool IsDebug
		{
			get { return false; }
		}

		public virtual string ArgsFormat
		{
			get { return "[{0}]"; }
		}

		public virtual string GlobalsFormat
		{
			get { return ""; }
		}

		public virtual string ProxyInstantiationFormat
		{
			get { return "{0}=new JsonFx.IO.Service({1});"; }
		}

		public virtual string PropertyFormat
		{
			get { return "{0}.{1}={2};"; }
		}

		public virtual string SafePropertyFormat
		{
			get { return "{0}[{1}]={2};"; }
		}

		public virtual string MethodBeginFormat
		{
			get { return "{0}.{1}=function("; }
		}

		public virtual string SafeMethodBeginFormat
		{
			get { return "{0}[{1}]=function("; }
		}

		public virtual string MethodMiddleFormat
		{
			get { return "opt){{this.invoke({0},"; }
		}

		public virtual string MethodEndFormat
		{
			get { return ",opt);};"; }
		}

		public virtual string ParamFormat
		{
			get { return "{0},"; }
		}

		#endregion Properties
	}

	internal class DebugJsonServiceProxyFormat : JsonServiceProxyFormat
	{
		#region Properties

		public override bool IsDebug
		{
			get { return true; }
		}

		public override string ArgsFormat
		{
			get { return "[ {0} ]"; }
		}

		public override string GlobalsFormat
		{
			get { return "/*global JsonFx */\r\n\r\n"; }
		}

		public override string ProxyInstantiationFormat
		{
			get { return "\r\n/*proxy*/ {0} = new JsonFx.IO.Service({1});\r\n\r\n"; }
		}

		public override string PropertyFormat
		{
			get { return "/*string*/ {0}.{1} = {2};\r\n\r\n"; }
		}

		public override string SafePropertyFormat
		{
			get { return "/*string*/ {0}[{1}] = {2};\r\n\r\n"; }
		}

		public override string MethodBeginFormat
		{
			get { return "/*{2}*/ {0}.{1} = function("; }
		}

		public override string SafeMethodBeginFormat
		{
			get { return "/*{2}*/ {0}[{1}] = function("; }
		}

		public override string MethodMiddleFormat
		{
			get { return "/*RequestOptions*/ options) {{\r\n\tthis.invoke({0}, "; }
		}

		public override string MethodEndFormat
		{
			get { return ", options);\r\n};\r\n\r\n"; }
		}

		public override string ParamFormat
		{
			get { return "/*{1}*/ {0}, "; }
		}

		#endregion Properties
	}
}
