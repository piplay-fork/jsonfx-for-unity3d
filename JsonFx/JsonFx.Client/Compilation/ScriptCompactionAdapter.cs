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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using EcmaScript.NET;
using Yahoo.Yui.Compressor;
using JsonFx.BuildTools;
using JsonFx.Configuration;

namespace JsonFx.Compilation
{
	/// <summary>
	/// A simple adapter for connecting ResourceBuildProvider to YuiCompressor.NET/EcmaScript.NET.
	/// </summary>
	public static class ScriptCompactionAdapter
	{
		// TODO: clean up all the various styles of input/output

		#region Public Methods

		public static List<ParseException> Compact(
			string virtualPath, 
			string inputSource, 
			TextWriter output)
		{
			if (output == null)
			{
				throw new NullReferenceException("Output TextWriter was null.");
			}

			List<ParseException> errors = new List<ParseException>();

			// compact and write out results
			try
			{
				if (String.IsNullOrEmpty(inputSource))
				{
					inputSource = File.ReadAllText(virtualPath);
				}

				string compacted = ScriptCompactionAdapter.Compact(virtualPath, inputSource, errors);

				output.Write(compacted);
			}
			catch (ParseError ex)
			{
				errors.Add(ex);
			}

			// return any errors
			return errors;
		}

		public static string Compact(string virtalPath, string source)
		{
			return Compact(virtalPath, source, (List<ParseException>)null);
		}

		public static string Compact(string virtalPath, string source, List<ParseException> errors)
		{
			BuildErrorReporter errorReporter = null;
			if (errors != null)
			{
				errorReporter = new BuildErrorReporter(virtalPath, errors);
			}

			ScriptCompactionSection config = ScriptCompactionSection.GetSettings();

			StringBuilder builder = new StringBuilder(source.Length);
			try
			{
				JavaScriptCompressor compressor = new JavaScriptCompressor(
					source,
					config.Verbose,						// verbose logging
					Encoding.UTF8,
					CultureInfo.InvariantCulture,
					config.IgnoreEval,					// ignore eval
					errorReporter);

				string compacted = compressor.Compress(
					config.Obfuscate,					// obfuscate
					config.PreserveSemicolons,			// preserve unneccessary semicolons
					config.DisableMicroOptimizations,	// disable micro-optimizations
					config.WordWrapWidth);				// word wrap width

				builder.Append(compacted);
			}
			catch (EcmaScriptRuntimeException ex)
			{
				if (errors.Count > 0 && String.IsNullOrEmpty(ex.SourceName))
				{
					// EcmaScript.NET provides an extra error which is a summary count of other errors
					errors.Add(new ParseWarning(ex.Message, virtalPath, ex.LineNumber, ex.ColumnNumber, ex));
				}
				else
				{
					errors.Add(new ParseError(ex.Message, ex.SourceName, ex.LineNumber, ex.ColumnNumber, ex));
				}
			}
			catch (Exception ex)
			{
				errors.Add(new ParseError(ex.Message, virtalPath, -1, -1, ex));
			}

			return builder.ToString();
		}

		#endregion Public Methods

		#region Private Methods

		private static void WriteHeader(StringBuilder builder, string copyright, string timeStamp)
		{
			if (!String.IsNullOrEmpty(copyright) || !String.IsNullOrEmpty(timeStamp))
			{
				int width = 6;
				if (!String.IsNullOrEmpty(copyright))
				{
					copyright = copyright.Replace("*/", "");// make sure not to nest commments
					width = Math.Max(copyright.Length+6, width);
				}
				if (!String.IsNullOrEmpty(timeStamp))
				{
					timeStamp = DateTime.Now.ToString(timeStamp).Replace("*/", "");// make sure not to nest commments
					width = Math.Max(timeStamp.Length+6, width);
				}

				builder.AppendLine();
				builder.AppendLine("/*!".PadRight(width, '-')+"*\\");

				if (!String.IsNullOrEmpty(copyright))
				{
					builder.AppendLine("\t"+copyright);
				}

				if (!String.IsNullOrEmpty(timeStamp))
				{
					builder.AppendLine("\t"+timeStamp);
				}

				builder.AppendLine("\\*".PadRight(width, '-')+"*/");
			}
		}

		#endregion Private Methods
	}
}
