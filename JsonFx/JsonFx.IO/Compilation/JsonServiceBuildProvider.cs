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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using System.Web;
using System.Web.Compilation;

using JsonFx.Handlers;
using JsonFx.JsonRpc;
using JsonFx.JsonRpc.Discovery;
using JsonFx.JsonRpc.Proxy;

namespace JsonFx.Compilation
{
	/// <summary>
	/// BuildProvider for JSON-RPC services.
	/// </summary>
	[PermissionSet(SecurityAction.Demand, Unrestricted=true)]
	public class JsonServiceBuildProvider : JsonFx.Compilation.ResourceBuildProvider
	{
		#region Constants

		private const string DefaultDirectiveName = "JsonService";
		private const string ErrorMissingDirective = "The service must have a <%@ {0} class=\"MyNamespace.MyClass\" ... %> directive.";
		private const string ErrorCouldNotCreateType = "Could not create type \"{0}\".";
		private const string ErrorAmbiguousType = "The type \"{0}\" is ambiguous: it could come from assembly \"{1}\" or from assembly \"{2}\". Please specify the assembly explicitly in the type name.";
		private const string ErrorMultipleDirectives = "There can be only one \"{0}\" directive.";
		private const string ErrorUnkownDirective = "The directive \"{0}\" is unknown.";
		private const string ErrorMissingAttrib = "The directive is missing a '{0}' attribute.";

		#endregion Constants

		#region Fields

		private string sourceText;
		private CompilerType compilerType;
		private string serviceTypeName;
		private bool directiveParsed;
		private int lineNumber = 1;
		private bool foundDirective;

		#endregion Fields

		#region BuildProvider Methods

		public override void GenerateCode(AssemblyBuilder assemblyBuilder)
		{
			try
			{
				this.EnsureDirective();

				if (String.IsNullOrEmpty(this.serviceTypeName))
				{
					return;
				}

				Assembly tempAssembly = null;
				if (!String.IsNullOrEmpty(this.sourceText))
				{
					// generate a code snippet for any inline source
					CodeSnippetCompileUnit unit = new CodeSnippetCompileUnit(this.sourceText);
					unit.LinePragma = new CodeLinePragma(base.VirtualPath, this.lineNumber);

					// add known assembly references
					foreach (Assembly assembly in this.ReferencedAssemblies)
					{
						assemblyBuilder.AddAssemblyReference(assembly);
						if (!String.IsNullOrEmpty(assembly.Location) &&
							!unit.ReferencedAssemblies.Contains(assembly.Location))
						{
							unit.ReferencedAssemblies.Add(assembly.Location);
						}
					}

					// compile once so we can reflect and build proxy, etc.
					assemblyBuilder.AddCodeCompileUnit(this, unit);
					CompilerResults results = assemblyBuilder.CodeDomProvider.CompileAssemblyFromDom(new CompilerParameters(), unit);
					if (results.Errors.HasErrors)
					{
						CompilerError error = results.Errors[0];
						throw new HttpParseException(error.ErrorText, null, error.FileName, "", error.Line);
					}
					tempAssembly = results.CompiledAssembly;
				}

				Type serviceType = this.GetTypeToCache(this.serviceTypeName, tempAssembly);
				this.GenerateServiceProxyCode(assemblyBuilder, serviceType);
			}
			catch (HttpParseException ex)
			{
				Console.Error.WriteLine(ex);
				throw;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				throw new HttpParseException("GenerateCode: "+ex.Message, ex, base.VirtualPath, this.sourceText, this.lineNumber);
			}
		}

		private void GenerateServiceProxyCode(AssemblyBuilder assemblyBuilder, Type serviceType)
		{
			IResourceNameGenerator nameGenerator = assemblyBuilder.CodeDomProvider as IResourceNameGenerator;
			if (nameGenerator != null)
			{
				this.ResourceFullName = nameGenerator.GenerateResourceName(base.VirtualPath);
			}
			else
			{
				this.ResourceFullName = ResourceBuildProvider.GenerateTypeNameFromPath(base.VirtualPath);
			}

			// TODO: consolidate app relative path conversion
			// calculate the service end-point path
			string proxyPath = ResourceHandler.EnsureAppRelative(base.VirtualPath).TrimStart('~');

			// build proxy from main service type
			JsonServiceDescription desc = new JsonServiceDescription(serviceType, proxyPath);
			JsonServiceProxyGenerator proxy = new JsonServiceProxyGenerator(desc);

			string proxyOutput = proxy.OutputProxy(false);
			proxyOutput = ScriptResourceCodeProvider.FirewallScript(proxyPath, proxyOutput, true);

			string debugProxyOutput = proxy.OutputProxy(true);
			debugProxyOutput = ScriptResourceCodeProvider.FirewallScript(proxyPath, debugProxyOutput, false);

			byte[] gzippedBytes, deflatedBytes;
			ResourceBuildProvider.Compress(proxyOutput, out gzippedBytes, out deflatedBytes);
			string hash = ResourceBuildProvider.ComputeHash(proxyOutput);

			// generate a service factory
			CodeCompileUnit generatedUnit = new CodeCompileUnit();

			#region namespace ResourceNamespace

			CodeNamespace ns = new CodeNamespace(this.ResourceNamespace);
			generatedUnit.Namespaces.Add(ns);

			#endregion namespace ResourceNamespace

			#region public sealed class ResourceTypeName : JsonServiceInfo

			CodeTypeDeclaration resourceType = new CodeTypeDeclaration();
			resourceType.IsClass = true;
			resourceType.Name = this.ResourceTypeName;
			resourceType.Attributes = MemberAttributes.Public|MemberAttributes.Final;
			resourceType.BaseTypes.Add(typeof(IJsonServiceInfo));
			resourceType.BaseTypes.Add(typeof(IOptimizedResult));
			ns.Types.Add(resourceType);

			#endregion public sealed class ResourceTypeName : CompiledBuildResult

			#region [BuildPath(virtualPath)]

			string virtualPath = base.VirtualPath;
			if (HttpRuntime.AppDomainAppVirtualPath.Length > 1)
			{
				virtualPath = virtualPath.Substring(HttpRuntime.AppDomainAppVirtualPath.Length);
			}
			virtualPath = "~"+virtualPath;

			CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(BuildPathAttribute)),
				new CodeAttributeArgument(new CodePrimitiveExpression(virtualPath)));
			resourceType.CustomAttributes.Add(attribute);

			#endregion [BuildPath(virtualPath)]

			#region private static readonly byte[] GzippedBytes

			CodeMemberField field = new CodeMemberField();
			field.Name = "GzippedBytes";
			field.Type = new CodeTypeReference(typeof(byte[]));
			field.Attributes = MemberAttributes.Private|MemberAttributes.Static|MemberAttributes.Final;

			CodeArrayCreateExpression arrayInit = new CodeArrayCreateExpression(field.Type, gzippedBytes.Length);
			foreach (byte b in gzippedBytes)
			{
				arrayInit.Initializers.Add(new CodePrimitiveExpression(b));
			}
			field.InitExpression = arrayInit;

			resourceType.Members.Add(field);

			#endregion private static static readonly byte[] GzippedBytes

			#region private static readonly byte[] DeflatedBytes

			field = new CodeMemberField();
			field.Name = "DeflatedBytes";
			field.Type = new CodeTypeReference(typeof(byte[]));
			field.Attributes = MemberAttributes.Private|MemberAttributes.Static|MemberAttributes.Final;

			arrayInit = new CodeArrayCreateExpression(field.Type, deflatedBytes.Length);
			foreach (byte b in deflatedBytes)
			{
				arrayInit.Initializers.Add(new CodePrimitiveExpression(b));
			}
			field.InitExpression = arrayInit;

			resourceType.Members.Add(field);

			#endregion private static readonly byte[] DeflatedBytes

			#region string IOptimizedResult.Source { get; }

			// add a readonly property with the original resource source
			CodeMemberProperty property = new CodeMemberProperty();
			property.Name = "Source";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;

			// get { return debugProxyOutput; }
			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(debugProxyOutput)));
			resourceType.Members.Add(property);

			#endregion string IOptimizedResult.Source { get; }

			#region string IOptimizedResult.PrettyPrinted { get; }

			// add a readonly property with the debug proxy code string
			property = new CodeMemberProperty();
			property.Name = "PrettyPrinted";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;

			// get { return ((IOptimizedResult)this).Source; }
			CodeExpression thisRef = new CodeCastExpression(typeof(IOptimizedResult), new CodeThisReferenceExpression());
			CodePropertyReferenceExpression sourceProperty = new CodePropertyReferenceExpression(thisRef, "Source");
			property.GetStatements.Add(new CodeMethodReturnStatement(sourceProperty));
			resourceType.Members.Add(property);

			#endregion string IOptimizedResult.PrettyPrinted { get; }

			#region string IOptimizedResult.Compacted { get; }

			// add a readonly property with the proxy code string
			property = new CodeMemberProperty();
			property.Name = "Compacted";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;
			// get { return proxyOutput; }
			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(proxyOutput)));
			resourceType.Members.Add(property);

			#endregion string IOptimizedResult.Compacted { get; }

			#region byte[] IOptimizedResult.Gzipped { get; }

			// add a readonly property with the gzipped proxy code
			property = new CodeMemberProperty();
			property.Name = "Gzipped";
			property.Type = new CodeTypeReference(typeof(byte[]));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;
			// get { return GzippedBytes; }
			property.GetStatements.Add(new CodeMethodReturnStatement(
				new CodeFieldReferenceExpression(
					new CodeTypeReferenceExpression(this.ResourceTypeName),
					"GzippedBytes")));
			resourceType.Members.Add(property);

			#endregion byte[] IOptimizedResult.Gzipped { get; }

			#region byte[] IOptimizedResult.Deflated { get; }

			// add a readonly property with the deflated proxy code
			property = new CodeMemberProperty();
			property.Name = "Deflated";
			property.Type = new CodeTypeReference(typeof(byte[]));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;
			// get { return DeflatedBytes; }
			property.GetStatements.Add(new CodeMethodReturnStatement(
				new CodeFieldReferenceExpression(
					new CodeTypeReferenceExpression(this.ResourceTypeName),
					"DeflatedBytes")));
			resourceType.Members.Add(property);

			#endregion byte[] IOptimizedResult.Deflated { get; }

			#region string IBuildResultMeta.Hash { get; }

			// add a readonly property with the hash of the resource data
			property = new CodeMemberProperty();
			property.Name = "Hash";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IBuildResult));
			property.HasGet = true;
			// get { return hash; }

			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(hash)));
			resourceType.Members.Add(property);

			#endregion string IBuildResultMeta.Hash { get; }

			#region string IBuildResultMeta.ContentType { get; }

			// add a readonly property with the MIME of the resource data
			property = new CodeMemberProperty();
			property.Name = "ContentType";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IBuildResult));
			property.HasGet = true;
			// get { return ScriptResourceCodeProvider.MimeType; }

			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(ScriptResourceCodeProvider.MimeType)));
			resourceType.Members.Add(property);

			#endregion string IBuildResultMeta.ContentType { get; }

			#region string IBuildResultMeta.FileExtension { get; }

			// add a readonly property with the extension of the resource data
			property = new CodeMemberProperty();
			property.Name = "FileExtension";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IBuildResult));
			property.HasGet = true;
			// get { return ScriptResourceCodeProvider.FileExt; }

			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(ScriptResourceCodeProvider.FileExt)));
			resourceType.Members.Add(property);

			#endregion string IBuildResultMeta.FileExtension { get; }

			#region public Type IJrpcServiceInfo.ServiceType { get; }

			// add a static field with the service type
			property = new CodeMemberProperty();
			property.Name = "ServiceType";
			property.Type = new CodeTypeReference(typeof(Type));
			property.Attributes = MemberAttributes.Public;
			property.ImplementationTypes.Add(new CodeTypeReference(typeof(IJsonServiceInfo)));
			property.HasGet = true;
			// get { return typeof(serviceType); }
			property.GetStatements.Add(new CodeMethodReturnStatement(new CodeTypeOfExpression(serviceType.FullName)));
			resourceType.Members.Add(property);

			#endregion public Type IJrpcServiceInfo.ServiceType { get; }

			#region object IJrpcServiceInfo.CreateService();

			CodeMemberMethod codeMethod = new CodeMemberMethod();
			codeMethod.Name = "CreateService";
			codeMethod.PrivateImplementationType = new CodeTypeReference(typeof(IJsonServiceInfo));
			codeMethod.ReturnType = new CodeTypeReference(typeof(Object));
			// return new serviceType();
			codeMethod.Statements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(serviceType)));
			resourceType.Members.Add(codeMethod);

			#endregion object IJrpcServiceInfo.CreateService();

			#region MethodInfo IJrpcServiceInfo.ResolveMethodName(string name);

			codeMethod = new CodeMemberMethod();
			codeMethod.Name = "ResolveMethodName";
			codeMethod.PrivateImplementationType = new CodeTypeReference(typeof(IJsonServiceInfo));
			codeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "name"));
			codeMethod.ReturnType = new CodeTypeReference(typeof(MethodInfo));
			CodeVariableReferenceExpression nameParam = new CodeVariableReferenceExpression("name");

			// if (String.IsNullOrEmpty(name)) { return null; }
			CodeConditionStatement nullCheck = new CodeConditionStatement();
			nullCheck.Condition = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(String)), "IsNullOrEmpty", nameParam);
			nullCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
			codeMethod.Statements.Add(nullCheck);

			Dictionary<string, MethodInfo> methodMap = JsonServiceBuildProvider.CreateMethodMap(serviceType);
			foreach (string name in methodMap.Keys)
			{
				CodeConditionStatement nameTest = new CodeConditionStatement();
				// if (String.Equals(name, methodName)) { ... }
				nameTest.Condition = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(String)), "Equals", nameParam, new CodePrimitiveExpression(name));

				// this.ServiceType
				CodePropertyReferenceExpression serviceTypeRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ServiceType");

				// method name
				CodePrimitiveExpression methodNameRef = new CodePrimitiveExpression(methodMap[name].Name);

				// this.ServiceType.GetMethod(methodNameRef)
				CodeMethodInvokeExpression methodInfoRef = new CodeMethodInvokeExpression(serviceTypeRef, "GetMethod", methodNameRef);

				// return MethodInfo;
				nameTest.TrueStatements.Add(new CodeMethodReturnStatement(methodInfoRef));
				codeMethod.Statements.Add(nameTest);
			}

			codeMethod.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
			resourceType.Members.Add(codeMethod);

			#endregion MethodInfo IJrpcServiceInfo.ResolveMethodName(string name);

			#region string[] IJrpcServiceInfo.GetMethodParams(string name);

			codeMethod = new CodeMemberMethod();
			codeMethod.Name = "GetMethodParams";
			codeMethod.PrivateImplementationType = new CodeTypeReference(typeof(IJsonServiceInfo));
			codeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "name"));
			codeMethod.ReturnType = new CodeTypeReference(typeof(String[]));
			CodeVariableReferenceExpression nameParam2 = new CodeVariableReferenceExpression("name");

			// if (String.IsNullOrEmpty(name)) { return new string[0]; }
			CodeConditionStatement nullCheck2 = new CodeConditionStatement();
			nullCheck2.Condition = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(String)), "IsNullOrEmpty", nameParam);
			nullCheck2.TrueStatements.Add(new CodeMethodReturnStatement(new CodeArrayCreateExpression(typeof(String[]), 0)));
			codeMethod.Statements.Add(nullCheck2);

			foreach (MethodInfo method in methodMap.Values)
			{
				string[] paramMap = JsonServiceBuildProvider.CreateParamMap(method);

				if (paramMap.Length < 1)
					continue;

				CodeConditionStatement nameTest = new CodeConditionStatement();
				// if (String.Equals(name, method.Name)) { ... }
				nameTest.Condition = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(String)), "Equals", nameParam2, new CodePrimitiveExpression(method.Name));

				// = {...}
				CodePrimitiveExpression[] paramList = new CodePrimitiveExpression[paramMap.Length];
				for (int i=0; i<paramMap.Length; i++)
				{
					paramList[i] = new CodePrimitiveExpression(paramMap[i]);
				}

				// new string[] = {...}
				CodeArrayCreateExpression paramArray = new CodeArrayCreateExpression(typeof(String[]), paramList);

				// return string[];
				nameTest.TrueStatements.Add(new CodeMethodReturnStatement(paramArray));
				codeMethod.Statements.Add(nameTest);
			}

			codeMethod.Statements.Add(new CodeMethodReturnStatement(new CodeArrayCreateExpression(typeof(String[]), 0)));
			resourceType.Members.Add(codeMethod);

			#endregion string[] IJrpcServiceInfo.GetMethodParams(string name);

			if (this.VirtualPathDependencies.Count > 0)
			{
				resourceType.BaseTypes.Add(typeof(IDependentResult));

				#region private static readonly string[] Dependencies

				field = new CodeMemberField();
				field.Name = "Dependencies";
				field.Type = new CodeTypeReference(typeof(string[]));
				field.Attributes = MemberAttributes.Private|MemberAttributes.Static|MemberAttributes.Final;

				arrayInit = new CodeArrayCreateExpression(field.Type, this.VirtualPathDependencies.Count);
				foreach (string key in this.VirtualPathDependencies)
				{
					arrayInit.Initializers.Add(new CodePrimitiveExpression(key));
				}
				field.InitExpression = arrayInit;

				resourceType.Members.Add(field);

				#endregion private static readonly string[] Dependencies

				#region IEnumerable<string> IDependentResult.VirtualPathDependencies { get; }

				// add a readonly property returning the static data
				property = new CodeMemberProperty();
				property.Name = "VirtualPathDependencies";
				property.Type = new CodeTypeReference(typeof(IEnumerable<string>));
				property.PrivateImplementationType = new CodeTypeReference(typeof(IDependentResult));
				property.HasGet = true;
				// get { return Dependencies; }
				property.GetStatements.Add(new CodeMethodReturnStatement(
					new CodeFieldReferenceExpression(
						new CodeTypeReferenceExpression(resourceType.Name),
						"Dependencies")));
				resourceType.Members.Add(property);

				#endregion IEnumerable<string> IDependentResult.VirtualPathDependencies { get; }
			}

			// Generate _ASP FastObjectFactory
			assemblyBuilder.GenerateTypeFactory(this.ResourceFullName);

			assemblyBuilder.AddCodeCompileUnit(this, generatedUnit);
		}

		public override CompilerType CodeCompilerType
		{
			get
			{
				try
				{
					this.EnsureDirective();

					// if directive failed will be null
					return this.compilerType;
				}
				catch (HttpParseException ex)
				{
					Console.Error.WriteLine(ex);
					throw;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
					throw new HttpParseException("CodeCompilerType: "+ex.Message, ex, base.VirtualPath, this.sourceText, this.lineNumber);
				}
			}
		}

		public override Type GetGeneratedType(CompilerResults results)
		{
			try
			{
				this.EnsureDirective();

				if (results.Errors.HasErrors)
				{
					foreach (CompilerError error in results.Errors)
					{
						throw new HttpParseException(error.ErrorText, null, error.FileName, "", error.Line);
					}
				}

				if (String.IsNullOrEmpty(this.serviceTypeName))
				{
					throw new HttpParseException("GetGeneratedType: missing service type name", null, base.VirtualPath, this.sourceText, this.lineNumber);
				}

				return this.GetTypeToCache(this.ResourceFullName, results.CompiledAssembly);
			}
			catch (HttpParseException ex)
			{
				Console.Error.WriteLine(ex);
				throw;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				throw new HttpParseException("GetGeneratedType: "+ex.Message, ex, base.VirtualPath, this.sourceText, this.lineNumber);
			}
		}

		#endregion BuildProvider Methods

		#region Mapping Methods

		/// <summary>
		/// Gets a mapping of parameter position to parameter name for a given method.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		private static String[] CreateParamMap(MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			string[] paramMap = new string[parameters.Length];
			for (int i=0; i<parameters.Length; i++)
			{
				// map name to position
				paramMap[i] = parameters[i].Name;
			}
			return paramMap;
		}

		/// <summary>
		/// Gets a mapping of method JsonName to MethodInfo for a given type.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <returns></returns>
		private static Dictionary<String, MethodInfo> CreateMethodMap(Type serviceType)
		{
			Dictionary<string, MethodInfo> methodMap = new Dictionary<String, MethodInfo>();

			// load methods into method map
			foreach (MethodInfo info in serviceType.GetMethods())
			{
				if (!info.IsPublic)
				{
					continue;
				}

				if (!JsonMethodAttribute.IsJsonMethod(info))
				{
					continue;
				}

				string jsonName = JsonMethodAttribute.GetJsonName(info);
				if (String.IsNullOrEmpty(jsonName))
				{
					methodMap[info.Name] = info;
				}
				else
				{
					methodMap[jsonName] = info;
				}
			}

			return methodMap;
		}

		#endregion Mapping Methods

		#region Directive Methods

		private void EnsureDirective()
		{
			if (!this.directiveParsed)
			{
				using (System.IO.TextReader reader = base.OpenReader())
				{
					this.sourceText = reader.ReadToEnd();
				}
				if (this.sourceText == null)
				{
					this.sourceText = String.Empty;
				}

				try
				{
					DirectiveParser parser = new DirectiveParser(this.sourceText, base.VirtualPath);
					parser.ProcessDirective += this.ProcessDirective;
					int index = parser.ParseDirectives(out this.lineNumber);
					this.sourceText = this.sourceText.Substring(index).Trim();
				}
				finally
				{
					this.directiveParsed = true;
				}

				if (!this.foundDirective)
				{
					throw new HttpParseException(String.Format(ErrorMissingDirective, DefaultDirectiveName), null, base.VirtualPath, this.sourceText, this.lineNumber);
				}
			}
		}

		private void ProcessDirective(string directiveName, IDictionary<string, string> attribs, int lineNumber)
		{
			this.lineNumber = lineNumber;

			if (DefaultDirectiveName.Equals(directiveName, StringComparison.OrdinalIgnoreCase))
			{
				if (this.foundDirective)
				{
					throw new HttpParseException(String.Format(ErrorMultipleDirectives, DefaultDirectiveName), null, base.VirtualPath, this.sourceText, this.lineNumber);
				}
				this.foundDirective = true;

				// determine source language
				string language = attribs.ContainsKey("Language") ? attribs["Language"] : null;
				if (String.IsNullOrEmpty(language))
				{
					// default to C# because it does not need additional assemblies
					language = "C#";
				}

				this.compilerType = this.GetDefaultCompilerTypeForLanguage(language);

				// determine backing class
				this.serviceTypeName = attribs.ContainsKey("Class") ? attribs["Class"] : null;
			}
			else if ("Assembly".Equals(directiveName, StringComparison.OrdinalIgnoreCase))
			{
				string name = attribs.ContainsKey("Name") ? attribs["Name"] : null;
				if (String.IsNullOrEmpty(name))
				{
					throw new HttpParseException(String.Format(ErrorMissingAttrib, "Name"), null, base.VirtualPath, this.sourceText, this.lineNumber);
				}
				this.AddAssemblyDependency(name);
			}
			else
			{
				throw new HttpParseException(String.Format(ErrorUnkownDirective, directiveName), null, base.VirtualPath, this.sourceText, this.lineNumber);
			}
		}

		#endregion Directive Methods

		#region Type Methods

		private Type GetTypeToCache(string typeName, Assembly assembly)
		{
			Type type = null;
			if (assembly != null)
			{
				type = assembly.GetType(typeName);
			}
			if (type == null)
			{
				type = this.GetType(typeName);
			}
			return type;
		}

		private Type GetType(string typeName)
		{
			Type type = null;
			if (CommaIndexInTypeName(typeName) > 0)// typeName contains assembly
			{
				try
				{
					type = Type.GetType(typeName, true);
					return type;
				}
				catch { }
			}
			type = this.GetTypeFromAssemblies(this.ReferencedAssemblies, typeName, false);
			if (type == null)
			{
				throw new HttpParseException(String.Format(ErrorCouldNotCreateType, typeName), null, base.VirtualPath, this.sourceText, this.lineNumber);
			}
			return type;
		}

		[ReflectionPermission(SecurityAction.Assert, Flags=ReflectionPermissionFlag.MemberAccess)]
		private Type GetTypeFromAssemblies(ICollection assemblies, string typeName, bool ignoreCase)
		{
			if (assemblies == null)
			{
				return null;
			}

			Type type = null;
			foreach (Assembly assembly in assemblies)
			{
				Type type2 = assembly.GetType(typeName, false, ignoreCase);
				if (type2 != null)
				{
					if ((type != null) && (type2 != type))
					{
						throw new HttpParseException(String.Format(ErrorAmbiguousType, typeName, type.Assembly.FullName, type2.Assembly.FullName), null, base.VirtualPath, this.sourceText, this.lineNumber);
					}
					type = type2;
				}
			}
			return type;
		}

		private static int CommaIndexInTypeName(string typeName)
		{
			int comma = typeName.LastIndexOf(',');
			if (comma < 0)
			{
				return -1;
			}
			int bracket = typeName.LastIndexOf(']');
			if (bracket > comma)
			{
				return -1;
			}
			return typeName.IndexOf(',', bracket + 1);
		}

		#endregion Type Methods
	}
}
