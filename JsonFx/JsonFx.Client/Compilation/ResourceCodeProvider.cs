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
using System.Collections.Generic;
using System.IO;

using JsonFx.BuildTools;

namespace JsonFx.Compilation
{
	/// <summary>
	/// Base class for all build-time resource compaction implementations.
	/// </summary>
	/// <remarks>
	/// This was implemented as a CodeProvider rather than a BuildProvider
	/// in order to gain access to the CompilerResults object.  This enables
	/// a custom compiler to correctly report its errors in the Visual Studio
	/// Error List.  Double clicking these errors takes the user to the actual
	/// source at the point where the error occurred.
	/// 
	/// Unfortunately, in Web Application Projects (WAP) the compilation happens
	/// outside of Visual Studio leaving little or no trace of these errors.
	/// The output of the resource will now also show an error listing.
	/// </remarks>
	public abstract class ResourceCodeProvider : Microsoft.CSharp.CSharpCodeProvider
	{
		#region Fields

		private readonly List<ParseException> errors = new List<ParseException>();
		private readonly List<string> g11nKeys = new List<string>();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ResourceCodeProvider()
		{
			this.ResetCodeProvider();
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the MIME type of the output.
		/// </summary>
		public abstract string ContentType { get; }

		/// <summary>
		/// Gets the file extension of the output.
		/// </summary>
		public override string FileExtension
		{
			get { return base.FileExtension; }
		}

		/// <summary>
		/// Gets the list of globalization keys used by this resource
		/// </summary>
		protected List<string> GlobalizationKeys
		{
			get { return this.g11nKeys; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Delegates compilation to the compiler implementation
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="virtualPath"></param>
		/// <param name="preProcessed"></param>
		/// <param name="compacted"></param>
		/// <returns>original source</returns>
		protected internal string CompileResource(
			IResourceBuildHelper helper,
			string virtualPath,
			out string preProcessed,
			out string compacted)
		{
			// clear any previously stored state (leave errors for later reporting)
			this.ResetCodeProvider();

			string source;

			// read the resource contents
			using (TextReader reader = helper.OpenReader(virtualPath))
			{
				source = reader.ReadToEnd();
			}

			// preprocess the resource
			this.ProcessResource(
				helper,
				virtualPath,
				source,
				out preProcessed,
				out compacted,
				this.errors);

			return source;
		}

		/// <summary>
		/// Adds any existing errors to the CompilerResults
		/// </summary>
		/// <param name="results"></param>
		private void ReportErrors(CompilerResults results)
		{
			foreach (ParseException ex in this.errors)
			{
				CompilerError error = new CompilerError(ex.File, ex.Line, ex.Column, ex.ErrorCode, ex.Message);
				error.IsWarning = (ex is ParseWarning);
				results.Errors.Add(error);
			}
			this.errors.Clear();
		}

		#endregion Methods

		#region CodeDomProvider Methods

		protected internal virtual void SetBaseClass(CodeTypeDeclaration resourceType)
		{
		}

		protected internal virtual void GenerateCodeExtensions(IResourceBuildHelper helper, CodeTypeDeclaration resourceType)
		{
			if (this.g11nKeys.Count <= 0)
			{
				// no globalization strings were needed so don't gen code for the property
				return;
			}

			resourceType.BaseTypes.Add(typeof(IGlobalizedBuildResult));

			#region private static readonly string[] g11nKeys

			CodeMemberField field = new CodeMemberField();
			field.Name = "g11nKeys";
			field.Type = new CodeTypeReference(typeof(string[]));
			field.Attributes = MemberAttributes.Private|MemberAttributes.Static|MemberAttributes.Final;

			CodeArrayCreateExpression arrayInit = new CodeArrayCreateExpression(field.Type, this.g11nKeys.Count);
			foreach (string key in this.g11nKeys)
			{
				arrayInit.Initializers.Add(new CodePrimitiveExpression(key));
			}
			field.InitExpression = arrayInit;

			resourceType.Members.Add(field);

			#endregion private static readonly string[] g11nKeys

			#region IEnumerable<string> IGlobalizedBuildResult.GlobalizationKeys { get; }

			// add a readonly property returning the static data
			CodeMemberProperty property = new CodeMemberProperty();
			property.Name = "GlobalizationKeys";
			property.Type = new CodeTypeReference(typeof(IEnumerable<string>));
			property.PrivateImplementationType = new CodeTypeReference(typeof(IGlobalizedBuildResult));
			property.HasGet = true;
			// get { return g11nKeys; }
			property.GetStatements.Add(new CodeMethodReturnStatement(
				new CodeFieldReferenceExpression(
					new CodeTypeReferenceExpression(resourceType.Name),
					field.Name)));
			resourceType.Members.Add(property);

			#endregion IEnumerable<string> IGlobalizedBuildResult.GlobalizationKeys { get; }
		}

		public override CompilerResults CompileAssemblyFromFile(CompilerParameters options, params string[] fileNames)
		{
			CompilerResults results = base.CompileAssemblyFromFile(options, fileNames);

			this.ReportErrors(results);

			return results;
		}

		public override CompilerResults CompileAssemblyFromDom(CompilerParameters options, params CodeCompileUnit[] compilationUnits)
		{
			CompilerResults results = base.CompileAssemblyFromDom(options, compilationUnits);

			this.ReportErrors(results);

			return results;
		}

		public override CompilerResults CompileAssemblyFromSource(CompilerParameters options, params string[] sources)
		{
			CompilerResults results = base.CompileAssemblyFromSource(options, sources);

			this.ReportErrors(results);

			return results;
		}

		#endregion CodeDomProvider Methods

		#region Compaction Methods

		/// <summary>
		/// Processes the source.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="virtualPath"></param>
		/// <param name="sourceText"></param>
		/// <param name="resource"></param>
		/// <param name="compacted"></param>
		/// <param name="errors"></param>
		protected internal abstract void ProcessResource(
			IResourceBuildHelper helper,
			string virtualPath,
			string source,
			out string resource,
			out string compacted,
			List<ParseException> errors);

		/// <summary>
		/// Process as external resources
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="url"></param>
		/// <param name="preProcessed"></param>
		/// <param name="compacted"></param>
		/// <param name="errors"></param>
		protected internal abstract void ProcessExternalResource(
			IResourceBuildHelper helper,
			string url,
			out string preProcessed,
			out string compacted,
			List<ParseException> errors);

		/// <summary>
		/// Clear any state because code providers get reused by BuildManager
		/// </summary>
		protected virtual void ResetCodeProvider()
		{
			this.g11nKeys.Clear();
		}

		#endregion Compaction Methods
	}
}
