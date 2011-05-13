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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Web;
using System.Web.Compilation;

using JsonFx.Handlers;

namespace JsonFx.Compilation
{
	public interface IResourceNameGenerator
	{
		string GenerateResourceName(string virtualPath);
	}

	public interface IResourceBuildHelper
	{
		string VirtualPath { get; }
		void AddVirtualPathDependency(string virtualPath);
		void AddAssemblyDependency(Assembly assembly);
		TextReader OpenReader(string virtualPath);
		CompilerType GetDefaultCompilerTypeForLanguage(string language);
	}

	/// <summary>
	/// The BuildProvider for all build-time resource compaction implementations.
	/// This provider processes the source storing a debug and a release output.
	/// The compilation result is a CompiledBuildResult class which has references
	/// to both resources.
	/// </summary>
	[PermissionSet(SecurityAction.Demand, Unrestricted=true)]
	public class ResourceBuildProvider :
		System.Web.Compilation.BuildProvider,
		IResourceBuildHelper
	{
		#region Fields

		private List<string> pathDependencies;
		private List<Assembly> assemblyDependencies;
		private string resourceFullName;
		private string resourceTypeName;
		private string resourceNamespace;

		#endregion Fields

		#region Properties

		protected virtual string ResourceFullName
		{
			get
			{
				if (String.IsNullOrEmpty(this.resourceFullName))
				{
					throw new InvalidOperationException("ResourceFullName is empty");
				}
				return this.resourceFullName;
			}
			set { this.resourceFullName = value; }
		}

		protected string ResourceNamespace
		{
			get
			{
				if (String.IsNullOrEmpty(this.resourceNamespace))
				{
					string type = this.ResourceFullName;
					int dot = type.LastIndexOf('.');
					if (dot > 0)
					{
						this.resourceNamespace = type.Substring(0, dot);
					}
					else
					{
						this.resourceNamespace = String.Empty;
					}
				}
				return this.resourceNamespace;
			}
		}

		protected string ResourceTypeName
		{
			get
			{
				if (String.IsNullOrEmpty(this.resourceTypeName))
				{
					string type = this.ResourceFullName;
					int dot = type.LastIndexOf('.');
					this.resourceTypeName = type.Substring(dot+1);
				}
				return this.resourceTypeName;
			}
		}

		#endregion Properties

		#region BuildProvider Methods

		public override ICollection VirtualPathDependencies
		{
			get
			{
				this.EnsureDependencies();

				return this.pathDependencies;
			}
		}

		protected new ICollection ReferencedAssemblies
		{
			get
			{
				if (this.assemblyDependencies == null)
				{
					this.EnsureAssemblyDependencies();
				}
				return this.assemblyDependencies;
			}
		}

		public override Type GetGeneratedType(CompilerResults results)
		{
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(this.ResourceFullName));

			return results.CompiledAssembly.GetType(this.ResourceFullName);
		}

		public override void GenerateCode(AssemblyBuilder assemblyBuilder)
		{
			string contentType, fileExtension;
			string originalSource, prettyPrintResource, compactedResource;

			ResourceCodeProvider provider = assemblyBuilder.CodeDomProvider as ResourceCodeProvider;
			if (provider != null)
			{
				originalSource = provider.CompileResource(
					this,
					base.VirtualPath,
					out prettyPrintResource,
					out compactedResource);

				contentType = provider.ContentType;
				fileExtension = provider.FileExtension;
			}
			else
			{
				// read the resource contents
				using (TextReader reader = this.OpenReader())
				{
					originalSource = compactedResource = prettyPrintResource = reader.ReadToEnd();
				}

				contentType = "text/plain";
				fileExtension = "txt";
			}

			IResourceNameGenerator nameGenerator = assemblyBuilder.CodeDomProvider as IResourceNameGenerator;
			if (nameGenerator != null)
			{
				this.ResourceFullName = nameGenerator.GenerateResourceName(base.VirtualPath);
			}
			else
			{
				this.ResourceFullName = ResourceBuildProvider.GenerateTypeNameFromPath(base.VirtualPath);
			}

			byte[] gzippedBytes, deflatedBytes;
			ResourceBuildProvider.Compress(compactedResource, out gzippedBytes, out deflatedBytes);
			string hash = ResourceBuildProvider.ComputeHash(compactedResource);

			// generate a resource container
			CodeCompileUnit generatedUnit = new CodeCompileUnit();

			#region namespace ResourceNamespace

			CodeNamespace ns = new CodeNamespace(this.ResourceNamespace);
			generatedUnit.Namespaces.Add(ns);

			#endregion namespace ResourceNamespace

			#region public sealed class ResourceTypeName

			CodeTypeDeclaration resourceType = new CodeTypeDeclaration();
			resourceType.IsClass = true;
			resourceType.Name = this.ResourceTypeName;
			resourceType.Attributes = MemberAttributes.Public|MemberAttributes.Final;

			provider.SetBaseClass(resourceType);

			resourceType.BaseTypes.Add(typeof(IOptimizedResult));
			ns.Types.Add(resourceType);

			#endregion public sealed class ResourceTypeName

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

			#region private static readonly byte[] DeflatedBytes;

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

			#endregion private static readonly byte[] DeflatedBytes;

			#region string IOptimizedResult.Source { get; }

			// add a readonly property with the original resource source
			CodeMemberProperty property = new CodeMemberProperty();
			property.Name = "Source";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;
			// get { return originalSource; }
			if (originalSource == null || originalSource.Length <= 0x5DC)
			{
				property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(originalSource)));
			}
			else
			{
				string escaped = ResourceBuildProvider.QuoteSnippetStringCStyle(originalSource);
				property.GetStatements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression(escaped)));
			}
			resourceType.Members.Add(property);

			#endregion string IOptimizedResult.Source { get; }

			#region string IOptimizedResult.PrettyPrinted { get; }

			// add a readonly property with the resource data
			property = new CodeMemberProperty();
			property.Name = "PrettyPrinted";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;

			if (String.Equals(originalSource, prettyPrintResource))
			{
				// get { return ((IOptimizedResult)this).Source; }
				CodeExpression thisRef = new CodeCastExpression(typeof(IOptimizedResult), new CodeThisReferenceExpression());
				CodePropertyReferenceExpression sourceProperty = new CodePropertyReferenceExpression(thisRef, "Source");
				property.GetStatements.Add(new CodeMethodReturnStatement(sourceProperty));
			}
			else
			{
				// get { return prettyPrintResource; }
				if (prettyPrintResource == null || prettyPrintResource.Length <= 0x5DC)
				{
					property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(prettyPrintResource)));
				}
				else
				{
					string escaped = ResourceBuildProvider.QuoteSnippetStringCStyle(prettyPrintResource);
					property.GetStatements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression(escaped)));
				}
			}
			resourceType.Members.Add(property);

			#endregion string IOptimizedResult.PrettyPrinted { get; }

			#region string IOptimizedResult.Compacted { get; }

			// add a readonly property with the compacted resource data
			property = new CodeMemberProperty();
			property.Name = "Compacted";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IOptimizedResult));
			property.HasGet = true;
			// get { return compactedResource; }
			if (compactedResource == null || compactedResource.Length <= 0x5DC)
			{
				property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(compactedResource)));
			}
			else
			{
				string escaped = ResourceBuildProvider.QuoteSnippetStringCStyle(compactedResource);
				property.GetStatements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression(escaped)));
			}
			resourceType.Members.Add(property);

			#endregion string IOptimizedResult.Compacted { get; }

			#region byte[] IOptimizedResult.Gzipped { get; }

			// add a readonly property with the gzipped resource data
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

			// add a readonly property with the deflated resource data
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

			#region string IBuildResultMeta.ContentType { get; }

			// add a readonly property with the MIME type
			property = new CodeMemberProperty();
			property.Name = "ContentType";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IBuildResult));
			property.HasGet = true;
			// get { return contentType; }
			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(contentType)));
			resourceType.Members.Add(property);

			#endregion string IBuildResultMeta.ContentType { get; }

			#region string IBuildResultMeta.FileExtension { get; }

			// add a readonly property with the MIME type
			property = new CodeMemberProperty();
			property.Name = "FileExtension";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IBuildResult));
			property.HasGet = true;
			// get { return fileExtension; }
			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(fileExtension)));
			resourceType.Members.Add(property);

			#endregion string IBuildResultMeta.FileExtension { get; }

			#region string IBuildResultMeta.Hash { get; }

			// add a readonly property with the resource data
			property = new CodeMemberProperty();
			property.Name = "Hash";
			property.Type = new CodeTypeReference(typeof(String));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IBuildResult));
			property.HasGet = true;
			// get { return hash); }

			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(hash)));
			resourceType.Members.Add(property);

			#endregion string IBuildResultMeta.Hash { get; }

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

			// allow the code provider to extend with additional properties
			provider.GenerateCodeExtensions(this, resourceType);

			// Generate _ASP FastObjectFactory
			assemblyBuilder.GenerateTypeFactory(this.ResourceFullName);

			assemblyBuilder.AddCodeCompileUnit(this, generatedUnit);
		}

		public static void Compress(string source, out byte[] gzipped, out byte[] deflated)
		{
			if (String.IsNullOrEmpty(source))
			{
				gzipped = deflated = new byte[0];
				return;
			}

			using (MemoryStream memStream = new MemoryStream())
			{
				using (GZipStream gzipStream = new GZipStream(memStream, CompressionMode.Compress, true))
				{
					using (StreamWriter writer = new StreamWriter(gzipStream))
					{
						writer.Write(source);
						writer.Flush();
					}
				}

				memStream.Seek(0L, SeekOrigin.Begin);
				gzipped = new byte[memStream.Length];
				memStream.Read(gzipped, 0, gzipped.Length);
			}

			using (MemoryStream memStream = new MemoryStream())
			{
				using (DeflateStream gzipStream = new DeflateStream(memStream, CompressionMode.Compress, true))
				{
					using (StreamWriter writer = new StreamWriter(gzipStream))
					{
						writer.Write(source);
						writer.Flush();
					}
				}

				memStream.Seek(0L, SeekOrigin.Begin);
				deflated = new byte[memStream.Length];
				memStream.Read(deflated, 0, deflated.Length);
			}
		}

		public override CompilerType CodeCompilerType
		{
			get
			{
				string extension = Path.GetExtension(base.VirtualPath);
				if (extension == null || extension.Length < 2)
				{
					return base.CodeCompilerType;
				}
				CompilerType compilerType = base.GetDefaultCompilerTypeForLanguage(extension.Substring(1));
				// set compilerType.CompilerParameters options here
				return compilerType;
			}
		}

		#endregion BuildProvider Methods

		#region IResourceBuildHelper Members

		string IResourceBuildHelper.VirtualPath
		{
			get
			{
				string appDomainAppVirtualPath = HttpRuntime.AppDomainAppVirtualPath;
				string virtualPath = base.VirtualPath;
				if (appDomainAppVirtualPath.Length > 1)
				{
					virtualPath = virtualPath.Substring(appDomainAppVirtualPath.Length);
				}
				virtualPath = "~"+virtualPath;
				return virtualPath;
			}
		}

		void IResourceBuildHelper.AddVirtualPathDependency(string virtualPath)
		{
			this.EnsureDependencies();

			this.AddDependency(virtualPath);
		}

		private void EnsureDependencies()
		{
			if (this.pathDependencies != null)
			{
				return;
			}

			this.pathDependencies = new List<string>();

			this.AddDependency(base.VirtualPath);

			foreach (string virtualPath in base.VirtualPathDependencies)
			{
				this.AddDependency(virtualPath);
			}
		}

		private void AddDependency(string virtualPath)
		{
			if (String.IsNullOrEmpty(virtualPath))
			{
				return;
			}

			virtualPath = ResourceHandler.EnsureAppRelative(virtualPath);

			// attempt to dedup
			if (!this.pathDependencies.Contains(virtualPath))
			{
				this.pathDependencies.Add(virtualPath);
			}
		}

		void IResourceBuildHelper.AddAssemblyDependency(Assembly assembly)
		{
			this.AddAssemblyDependency(assembly);
		}

		protected void AddAssemblyDependency(string assemblyName)
		{
			Assembly assembly = Assembly.Load(assemblyName);
			this.AddAssemblyDependency(assembly);
		}

		protected void AddAssemblyDependency(Assembly assembly)
		{
			this.EnsureAssemblyDependencies();

			// attempt to dedup
			if (!this.assemblyDependencies.Contains(assembly))
			{
				this.assemblyDependencies.Add(assembly);
			}
		}

		private void EnsureAssemblyDependencies()
		{
			if (this.assemblyDependencies == null)
			{
				this.assemblyDependencies = new List<Assembly>();
				foreach (Assembly asm in base.ReferencedAssemblies)
				{
					this.assemblyDependencies.Add(asm);
				}

				if (this.assemblyDependencies.Count < 1)
				{
					// this is where the Mono issue is: no referenced assemblies are passed in
					Console.Error.WriteLine("ReferencedAssemblies were empty.");
					string[] asmList = Directory.GetFiles(HttpRuntime.CodegenDir, "*.dll", SearchOption.AllDirectories);
					foreach (string asm in asmList)
					{
						Assembly assembly = Assembly.LoadFile(asm);
						if (assembly == null)
						{
							Console.Error.WriteLine("Assembly load failed from: "+asm);
						}
						else
						{
							Console.Error.WriteLine("Loaded assembly from: "+asm);
							this.assemblyDependencies.Add(assembly);
						}
					}
				}
			}
		}

		TextReader IResourceBuildHelper.OpenReader(string virtualPath)
		{
			return this.OpenReader(virtualPath);
		}

		CompilerType IResourceBuildHelper.GetDefaultCompilerTypeForLanguage(string language)
		{
			return this.GetDefaultCompilerTypeForLanguage(language);
		}

		#endregion IResourceBuildHelper Members

		#region Utility Methods

		/// <summary>
		/// Generates a Type name for the compiled resource
		/// </summary>
		/// <param name="virtualPath"></param>
		/// <returns></returns>
		public static string GenerateTypeNameFromPath(string virtualPath)
		{
			const string rootNamespace = "ASP.";

			if (virtualPath == null)
			{
				virtualPath = String.Empty;
			}

			// skip leading path chars
			int i;
			for (i=0; i<virtualPath.Length; i++)
			{
				switch (virtualPath[i])
				{
					case '~':
					case '/':
					case '\\':
					{
						continue;
					}
				}

				// found first real char
				break;
			}

			StringBuilder builder = new StringBuilder(virtualPath, i, virtualPath.Length-i, virtualPath.Length+10);
			if (builder.Length <= 0)
			{
				return rootNamespace+"_"+Guid.NewGuid().ToString("n");
			}

			bool startChar = true;
			for (i=0; i<builder.Length; i++)
			{
				char ch = builder[i];
				if (Char.IsDigit(ch))
				{
					if (startChar)
					{
						builder.Insert(i, '_');
						startChar = false;
						i++;
					}

					// digits are only allowed after first char
					continue;
				}

				if (Char.IsLetter(ch))
				{
					startChar = false;
					continue;
				}

				switch (ch)
				{
					case '_':
					{
						startChar = false;
						break;
					}
					//case '\\':
					//case '/':
					//{
					//    builder[i] = '.';
					//    startChar = true;
					//    break;
					//}
					default:
					{
						builder[i] = '_';
						startChar = false;
						break;
					}
				}
			}

			return rootNamespace+builder.ToString().ToLowerInvariant();
		}

		/// <summary>
		/// Generates a unique hash from string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ComputeHash(string value)
		{
			// get String as a Byte[]
			byte[] buffer = Encoding.Unicode.GetBytes(value);

			return ResourceBuildProvider.ComputeHash(buffer);
		}

		/// <summary>
		/// Generates a unique hash from byte[]
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		protected static string ComputeHash(Stream value)
		{
			// generate hash
			byte[] hash = SHA1.Create().ComputeHash(value);

			// convert hash to string
			return ResourceBuildProvider.FormatBytes(hash);
		}

		/// <summary>
		/// Generates a unique hash from byte[]
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		protected static string ComputeHash(byte[] value)
		{
			// generate hash
			byte[] hash = SHA1.Create().ComputeHash(value);

			// convert hash to string
			return ResourceBuildProvider.FormatBytes(hash);
		}

		/// <summary>
		/// Gets the hex digits for the given bytes
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string FormatBytes(byte[] value)
		{
			if (value == null || value.Length == 0)
			{
				return String.Empty;
			}

			StringBuilder builder = new StringBuilder();

			// Loop through each byte of the binary data 
			// and format each one as a hexadecimal string
			for (int i=0; i<value.Length; i++)
			{
				builder.Append(value[i].ToString("x2"));
			}

			// the hexadecimal string
			return builder.ToString();
		}

		/// <summary>
		/// Escapes a C# string using C-style escape sequences.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// Adapted from Microsoft.CSharp.CSharpCodeGenerator.QuoteSnippetStringCStyle
		/// Primary difference is does not wrap at 80 chars as can cause C# compiler to fail.
		/// </remarks>
		private static string QuoteSnippetStringCStyle(string value)
		{
			// CS1647: An expression is too long or complex to compile near '...'
			// happens if line wraps too many times (335440 chars is max for x64, 926240 chars is max for x86)

			// CS1034: Compiler limit exceeded: Line cannot exceed 16777214 characters
			// theoretically every character could be escaped unicode (6 chars), plus quotes, etc.

			const int LineWrapWidth = (16777214/6)-4;
			StringBuilder b = new StringBuilder(value.Length+5);

			b.Append("\r\n\"");
			for (int i=0; i<value.Length; i++)
			{
				switch (value[i])
				{
					case '\u2028':
					case '\u2029':
					{
						int ch = (int)value[i];
						b.Append(@"\u");
						b.Append(ch.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
						break;
					}
					case '\\':
					{
						b.Append(@"\\");
						break;
					}
					case '\'':
					{
						b.Append(@"\'");
						break;
					}
					case '\t':
					{
						b.Append(@"\t");
						break;
					}
					case '\n':
					{
						b.Append(@"\n");
						break;
					}
					case '\r':
					{
						b.Append(@"\r");
						break;
					}
					case '"':
					{
						b.Append("\\\"");
						break;
					}
					case '\0':
					{
						b.Append(@"\0");
						break;
					}
					default:
					{
						b.Append(value[i]);
						break;
					}
				}

				if ((i > 0) && ((i % LineWrapWidth) == 0))
				{
					if ((Char.IsHighSurrogate(value[i]) && (i < (value.Length - 1))) && Char.IsLowSurrogate(value[i + 1]))
					{
						b.Append(value[++i]);
					}
					b.Append("\"+\r\n");
					b.Append('"');
				}
			}
			b.Append("\"");
			return b.ToString();
		}

		#endregion Utility Methods
	}
}
