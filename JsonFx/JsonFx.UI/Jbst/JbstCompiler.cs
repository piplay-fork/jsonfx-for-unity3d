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
using System.Collections.Generic;
using System.IO;

using JsonFx.BuildTools;
using JsonFx.BuildTools.HtmlDistiller;
using JsonFx.BuildTools.HtmlDistiller.Filters;
using JsonFx.Compilation;
using JsonFx.Handlers;

namespace JsonFx.UI.Jbst
{
	/// <summary>
	/// Simple wrapper for compiling JBSTs
	/// </summary>
	public class JbstCompiler
	{
		#region Compiler Methods

		/// <summary>
		/// Compiles the provided input
		/// </summary>
		/// <param name="input"></param>
		/// <param name="filename"></param>
		/// <param name="compilationErrors"></param>
		/// <param name="compactionErrors"></param>
		/// <returns></returns>
		public IOptimizedResult Compile(TextReader input, string filename, List<ParseException> compilationErrors, List<ParseException> compactionErrors)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}

			// verify, compact and write out results
			// parse JBST markup
			JbstWriter writer = new JbstWriter(filename);

			StringWriter sw = new StringWriter();

			string source = input.ReadToEnd();
			try
			{
				HtmlDistiller parser = new HtmlDistiller();
				parser.EncodeNonAscii = false;
				parser.BalanceTags = false;
				parser.NormalizeWhitespace = false;
				parser.HtmlWriter = writer;
				parser.HtmlFilter = new NullHtmlFilter();
				parser.Parse(source);

				// render the pretty-printed version
				writer.Render(sw);
			}
			catch (ParseException ex)
			{
				compilationErrors.Add(ex);
			}
			catch (Exception ex)
			{
				compilationErrors.Add(new ParseError(ex.Message, filename, 0, 0, ex));
			}

			SimpleJbstBuildResult result = new SimpleJbstBuildResult(writer.JbstName, writer.AutoMarkup);

			result.Source = source;
			result.PrettyPrinted = sw.GetStringBuilder().ToString();
			result.Compacted = this.Compact(result.PrettyPrinted, filename, compactionErrors);
			result.ContentType = "text/javascript";
			result.FileExtension = ".jbst.js";
			result.Hash = this.ComputeHash(result.Source);

			// return any errors
			return result;
		}

		private string Compact(
			string script,
			string filename,
			List<ParseException> errors)
		{
			using (StringWriter writer = new StringWriter())
			{
				IList<ParseException> parseErrors;
				try
				{
					parseErrors = ScriptCompactionAdapter.Compact(
						filename,
						script,
						writer);
				}
				catch (ParseException ex)
				{
					errors.Add(ex);
					parseErrors = null;
				}
				catch (Exception ex)
				{
					errors.Add(new ParseError(ex.Message, filename, 0, 0, ex));
					parseErrors = null;
				}

				if (parseErrors != null && parseErrors.Count > 0)
				{
					errors.AddRange(parseErrors);
				}

				writer.Flush();

				return writer.GetStringBuilder().ToString();
			}
		}

		private string ComputeHash(string value)
		{
			return ResourceBuildProvider.ComputeHash(value);
		}

		//public void WriteHeader(TextWriter writer, string copyright, string timeStamp)
		//{
		//    if (!String.IsNullOrEmpty(copyright) || !String.IsNullOrEmpty(timeStamp))
		//    {
		//        int width = 6;
		//        if (!String.IsNullOrEmpty(copyright))
		//        {
		//            copyright = copyright.Replace("*/", "");// make sure not to nest commments
		//            width = Math.Max(copyright.Length+6, width);
		//        }
		//        if (!String.IsNullOrEmpty(timeStamp))
		//        {
		//            timeStamp = DateTime.Now.ToString(timeStamp).Replace("*/", "");// make sure not to nest commments
		//            width = Math.Max(timeStamp.Length+6, width);
		//        }

		//        writer.WriteLine("/*".PadRight(width, '-')+"*\\");

		//        if (!String.IsNullOrEmpty(copyright))
		//        {
		//            writer.WriteLine("\t"+copyright);
		//        }

		//        if (!String.IsNullOrEmpty(timeStamp))
		//        {
		//            writer.WriteLine("\t"+timeStamp);
		//        }

		//        writer.WriteLine("\\*".PadRight(width, '-')+"*/");
		//    }
		//}

		#endregion Compiler Methods
	}
}
