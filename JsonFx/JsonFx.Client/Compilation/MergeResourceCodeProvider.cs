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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Compilation;

using JsonFx.BuildTools;
using JsonFx.Client;
using JsonFx.Handlers;

namespace JsonFx.Compilation
{
	public class MergeResourceCodeProvider : JsonFx.Compilation.ResourceCodeProvider
	{
		#region Constants

		private static readonly char[] LineDelims = { '\r', '\n' };
		private static readonly char[] AltDelims = { '|' };
		private static readonly char[] TypeDelims = { ',' };

		#endregion Constants

		#region Fields

		private string contentType;
		private string fileExtension;
		private bool isMimeSet;

		#endregion Fields

		#region ResourceCodeProvider Properties

		public override string FileExtension
		{
			get { return this.fileExtension; }
		}

		public override string ContentType
		{
			get { return this.contentType; }
		}

		#endregion ResourceCodeProvider Properties

		#region ResourceCodeProvider Methods

		protected internal override void SetBaseClass(CodeTypeDeclaration resourceType)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(this.contentType, CssResourceCodeProvider.MimeType))
			{
				resourceType.BaseTypes.Add(typeof(CssBuildResult));
			}
			else
			{
				resourceType.BaseTypes.Add(typeof(ScriptBuildResult));
			}
		}

		protected internal override void GenerateCodeExtensions(IResourceBuildHelper helper, CodeTypeDeclaration resourceType)
		{
			base.GenerateCodeExtensions(helper, resourceType);

			#region public ResourceType() : base(virtualPath) {}

			CodeConstructor ctor = new CodeConstructor();
			ctor.Attributes = MemberAttributes.Public;
			ctor.BaseConstructorArgs.Add(new CodePrimitiveExpression(helper.VirtualPath));
			resourceType.Members.Add(ctor);

			#endregion public ResourceType() : base(virtualPath) {}
		}


		protected override void ResetCodeProvider()
		{
			base.ResetCodeProvider();

			this.contentType = "text/plain";
			this.fileExtension = "txt";
			this.isMimeSet = false;
		}

		protected internal override void ProcessResource(
			IResourceBuildHelper helper,
			string virtualPath,
			string sourceText,
			out string resource,
			out string compacted,
			List<ParseException> errors)
		{
			if (String.IsNullOrEmpty(sourceText))
			{
				resource = null;
				compacted = null;
				return;
			}

			StringBuilder resources = new StringBuilder();
			StringBuilder compacts = new StringBuilder();
			string[] files = sourceText.Split(LineDelims, StringSplitOptions.RemoveEmptyEntries);

			for (int i=0; i<files.Length; i++)
			{
				try
				{
					string file = files[i],
						compactAlt = null;

					if (file != null)
					{
						file = file.Trim();
					}

					// skip blank and comment lines
					if (String.IsNullOrEmpty(file) ||
						file.StartsWith("//") ||
						file.StartsWith("#"))
					{
						continue;
					}

					MergeResourceCodeProvider.SplitAlternates(file, out file, out compactAlt);

					if (file.IndexOf("://") >= 0)
					{
						string preProcessed, compact;

						this.ProcessExternalResource(helper, file, out preProcessed, out compact, errors);

						if (!String.IsNullOrEmpty(compactAlt))
						{
							this.ProcessExternalResource(helper, compactAlt, out compactAlt, out compact, errors);
						}

						compacts.Append(compact);
						resources.Append(preProcessed);
						continue;
					}

					// process embedded resource
					if (file.IndexOf(',') >= 0)
					{
						string preProcessed, compact;

						this.ProcessEmbeddedResource(helper, file, out preProcessed, out compact, errors);

						if (!String.IsNullOrEmpty(compactAlt))
						{
							this.ProcessEmbeddedResource(helper, compactAlt, out compactAlt, out compact, errors);
						}

						compacts.Append(compact);
						resources.Append(preProcessed);
						continue;
					}

					file = ResourceHandler.EnsureAppRelative(file);
					if (!String.IsNullOrEmpty(compactAlt))
					{
						compactAlt = ResourceHandler.EnsureAppRelative(compactAlt);
					}

					// try to get as a IOptimizedResult
					IOptimizedResult result = this.ProcessPrecompiled(helper, file);
					if (result != null)
					{
						resources.Append(result.PrettyPrinted);

						if (String.IsNullOrEmpty(compactAlt))
						{
							compacts.Append(result.Compacted);
						}
						else
						{
							IOptimizedResult result2 = this.ProcessPrecompiled(helper, compactAlt);
							if (result2 != null)
							{
								compacts.Append(result2.Compacted);
							}
						}
						continue;
					}

					// ask BuildManager if compiles down to a string
					string text = BuildManager.GetCompiledCustomString(file);
					if (String.IsNullOrEmpty(text))
					{
						// use the raw contents of the virtual path
						text = helper.OpenReader(file).ReadToEnd();
					}

					if (!String.IsNullOrEmpty(text))
					{
						helper.AddVirtualPathDependency(file);

						resources.Append(text);

						if (String.IsNullOrEmpty(compactAlt))
						{
							compacts.Append(text);
						}
						else
						{
							helper.AddVirtualPathDependency(compactAlt);

							string text2 = BuildManager.GetCompiledCustomString(compactAlt);
							compacts.Append(text2);
						}
						continue;
					}
				}
				catch (ParseException ex)
				{
					errors.Add(ex);
				}
				catch (Exception ex)
				{
					errors.Add(new ParseError(ex.Message, virtualPath, i+1, 1, ex));
				}
			}

			resources.AppendLine();
			resources.AppendFormat("/* JsonFx v{0} */", JsonFx.About.Fx.Version);

			resource = resources.ToString();
			compacted = compacts.ToString();
		}

		public static string JoinAlternates(string full, string compact)
		{
			if (String.IsNullOrEmpty(compact))
			{
				return full;
			}

			if (String.IsNullOrEmpty(full))
			{
				return compact;
			}

			return full + MergeResourceCodeProvider.AltDelims[0] + compact;
		}

		public static void SplitAlternates(string original, out string full, out string compact)
		{
			string[] alts = original.Split(MergeResourceCodeProvider.AltDelims, 2, StringSplitOptions.RemoveEmptyEntries);
			if (alts.Length > 1)
			{
				full = alts[0].Trim();
				compact = alts[1].Trim();
			}
			else
			{
				full = compact = original;
			}
		}

		private IOptimizedResult ProcessPrecompiled(IResourceBuildHelper helper, string file)
		{
			IOptimizedResult result = ResourceHandler.Create<IOptimizedResult>(file);
			if (result != null)
			{
				if (!this.isMimeSet &&
					!String.IsNullOrEmpty(result.ContentType) &&
					!String.IsNullOrEmpty(result.FileExtension))
				{
					this.contentType = result.ContentType;
					this.fileExtension = result.FileExtension;
					this.isMimeSet = true;
				}

				if (result is IGlobalizedBuildResult)
				{
					this.GlobalizationKeys.AddRange(((IGlobalizedBuildResult)result).GlobalizationKeys);
				}

				helper.AddVirtualPathDependency(file);

				ICollection dependencies = BuildManager.GetVirtualPathDependencies(file);
				if (dependencies != null)
				{
					foreach (string dependency in dependencies)
					{
						helper.AddVirtualPathDependency(dependency);
					}
				}

				if (result is IDependentResult)
				{
					foreach (string dependency in ((IDependentResult)result).VirtualPathDependencies)
					{
						helper.AddVirtualPathDependency(dependency);
					}
				}
			}

			return result;
		}

		private void ProcessEmbeddedResource(
			IResourceBuildHelper helper,
			string source,
			out string preProcessed,
			out string compacted,
			List<ParseException> errors)
		{
			preProcessed = source.Replace(" ", "");
			string[] parts = preProcessed.Split(TypeDelims, 2, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 2 ||
				String.IsNullOrEmpty(parts[0]) ||
				String.IsNullOrEmpty(parts[1]))
			{
				compacted = preProcessed = null;
				return;
			}

			parts[0] = MergeResourceCodeProvider.ScrubResourceName(parts[0]);

			// load resources from Assembly
			Assembly assembly = Assembly.Load(parts[1]);
			helper.AddAssemblyDependency(assembly);

			ManifestResourceInfo info = assembly.GetManifestResourceInfo(parts[0]);
			if (info == null)
			{
				compacted = preProcessed = null;
				return;
			}

			using (Stream stream = assembly.GetManifestResourceStream(parts[0]))
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					preProcessed = reader.ReadToEnd();
					compacted = null;
				}
			}

			string ext = Path.GetExtension(parts[0]).Trim('.');
			CompilerType compiler = helper.GetDefaultCompilerTypeForLanguage(ext);
			if (!typeof(ResourceCodeProvider).IsAssignableFrom(compiler.CodeDomProviderType))
			{
				// don't know how to process any further
				compacted = preProcessed;
				return;
			}

			ResourceCodeProvider provider = (ResourceCodeProvider)Activator.CreateInstance(compiler.CodeDomProviderType);

			try
			{
				// concatenate the preprocessed source for current merge phase
				provider.ProcessResource(
					helper,
					parts[0],
					preProcessed,
					out preProcessed,
					out compacted,
					errors);
			}
			catch (ParseException ex)
			{
				errors.Add(ex);
			}
			catch (Exception ex)
			{
				errors.Add(new ParseError(ex.Message, parts[0], 0, 0, ex));
			}

			if (!this.isMimeSet &&
				!String.IsNullOrEmpty(provider.ContentType) &&
				!String.IsNullOrEmpty(provider.FileExtension))
			{
				this.contentType = provider.ContentType;
				this.fileExtension = provider.FileExtension;
				this.isMimeSet = true;
			}
		}

		protected internal override void ProcessExternalResource(
			IResourceBuildHelper helper,
			string url,
			out string preProcessed,
			out string compacted,
			List<ParseException> errors)
		{
			compacted = preProcessed = String.Empty;

			Uri uri;
			if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
			{
				throw new ArgumentException("Invalid external URL");
			}

			string ext = Path.GetExtension(uri.AbsolutePath).Trim('.');
			CompilerType compiler = helper.GetDefaultCompilerTypeForLanguage(ext);
			if (!typeof(ResourceCodeProvider).IsAssignableFrom(compiler.CodeDomProviderType))
			{
				// don't know how to process any further
				return;
			}

			ResourceCodeProvider provider = (ResourceCodeProvider)Activator.CreateInstance(compiler.CodeDomProviderType);

			try
			{
				// concatenate the preprocessed source for current merge phase
				provider.ProcessExternalResource(
					helper,
					url,
					out preProcessed,
					out compacted,
					errors);
			}
			catch (ParseException ex)
			{
				errors.Add(ex);
			}
			catch (Exception ex)
			{
				errors.Add(new ParseError(ex.Message, url, 0, 0, ex));
			}

			if (!this.isMimeSet &&
				!String.IsNullOrEmpty(provider.ContentType) &&
				!String.IsNullOrEmpty(provider.FileExtension))
			{
				this.contentType = provider.ContentType;
				this.fileExtension = provider.FileExtension;
				this.isMimeSet = true;
			}
		}

		#endregion ResourceCodeProvider Methods

		#region Utility Methods

		private static string ScrubResourceName(string resource)
		{
			if (String.IsNullOrEmpty(resource))
			{
				return resource;
			}

			StringBuilder builder = new StringBuilder(resource);
			builder.Replace('/', '.');
			builder.Replace('\\', '.');
			builder.Replace('?', '.');
			builder.Replace('*', '.');
			builder.Replace(':', '.');
			return builder.ToString().TrimStart('.');
		}

		#endregion Utility Methods
	}
}
