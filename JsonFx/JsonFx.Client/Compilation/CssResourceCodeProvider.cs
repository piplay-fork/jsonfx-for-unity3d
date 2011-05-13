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
using System.Collections.Generic;
using System.IO;

using JsonFx.BuildTools;
using JsonFx.BuildTools.CssCompactor;
using JsonFx.Client;

namespace JsonFx.Compilation
{
	public class CssResourceCodeProvider : JsonFx.Compilation.ResourceCodeProvider
	{
		#region Constants

		public const string MimeType = "text/css";

		#endregion Constants

		#region ResourceCodeProvider Properties

		public override string ContentType
		{
			get { return CssResourceCodeProvider.MimeType; }
		}

		public override string FileExtension
		{
			get { return "css"; }
		}

		#endregion ResourceCodeProvider Properties

		#region ResourceCodeProvider Methods

		protected internal override void SetBaseClass(CodeTypeDeclaration resourceType)
		{
			resourceType.BaseTypes.Add(typeof(CssBuildResult));
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

		protected internal override void ProcessResource(
			IResourceBuildHelper helper,
			string virtualPath,
			string sourceText,
			out string resource,
			out string compacted,
			List<ParseException> errors)
		{
			using (StringWriter writer = new StringWriter())
			{
				IList<ParseException> parseErrors;
				try
				{
					parseErrors = CssCompactor.Compact(
						virtualPath,
						sourceText,
						writer,
						null,
						null,
						CssCompactor.Options.None);
				}
				catch (ParseException ex)
				{
					errors.Add(ex);
					parseErrors = null;
				}
				catch (Exception ex)
				{
					errors.Add(new ParseError(ex.Message, virtualPath, 0, 0, ex));
					parseErrors = null;
				}

				if (parseErrors != null && parseErrors.Count > 0)
				{
					errors.AddRange(parseErrors);
				}

				writer.Flush();

				resource = sourceText;
				compacted = writer.ToString();
			}
		}

		protected internal override void ProcessExternalResource(
			IResourceBuildHelper helper,
			string url,
			out string preProcessed,
			out string compacted,
			List<ParseException> errors)
		{
			compacted = preProcessed = String.Format("@import url('{0}');", url);
		}

		#endregion ResourceCodeProvider Members
	}
}
