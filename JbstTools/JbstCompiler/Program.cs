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
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using JsonFx.BuildTools;
using JsonFx.BuildTools.HtmlDistiller;
using JsonFx.BuildTools.HtmlDistiller.Filters;
using JsonFx.BuildTools.IO;
using JsonFx.UI.Jbst;

class Program
{
	#region Constants

	private const string Help =
		"JsonML+BST Template Compiler (version {0})\r\n\r\n"+
		"JbstCompiler.exe /IN:file [ /OUT:file ] [ /INFO:copyright ] [ /TIME:timeFormat ] [ /WARNING ]\r\n\r\n"+
		"\t/IN:\t\tInput File Path (not used with /DIR:...)\r\n" +
		"\t/OUT:\t\tOutput File Path (not used with /DIR:...)\r\n" +
		"\t/DIR:\t\tInput Directory Root (for bulk compiling an entire app *.jbst -> *.jbst.js)\r\n" +
		"\t/INFO:\t\tCopyright label\r\n" +
		"\t/TIME:\t\tTimeStamp Format\r\n"+
		"\t/WARNING\tSyntax issues reported as warnings\r\n"+
		"\t/PRETTY\t\tPretty-Print the output (default is compact)\r\n\r\n"+
		"Examples:"+
		"\tJbstCompiler.exe /IN:myTemplate.jbst /OUT:compiled/myTemplate.js /INFO:\"(c)2009 My Template, Inc.\" /TIME:\"'Compiled 'yyyy-MM-dd @ HH:mm\""+
		"\tJbstCompiler.exe /DIR:myTemplates/ /INFO:\"(c)2009 My Template, Inc.\" /TIME:\"'Compiled 'yyyy-MM-dd @ HH:mm\"";

	private enum ArgType
	{
		Empty,// need a default value
		InputFile,
		OutputFile,
		Directory,
		Copyright,
		TimeStamp,
		PrettyPrint,
		Warning
	}

	private static readonly ArgsTrie<ArgType> Mapping = new ArgsTrie<ArgType>(
		new ArgsMap<ArgType>[] {
			new ArgsMap<ArgType>("/IN:", ArgType.InputFile),
			new ArgsMap<ArgType>("/OUT:", ArgType.OutputFile),
			new ArgsMap<ArgType>("/DIR:", ArgType.Directory),
			new ArgsMap<ArgType>("/INFO:", ArgType.Copyright),
			new ArgsMap<ArgType>("/TIME:", ArgType.TimeStamp),
			new ArgsMap<ArgType>("/PRETTY", ArgType.PrettyPrint),
			new ArgsMap<ArgType>("/WARNING", ArgType.Warning)
		});

	#endregion Constants

	#region Program Entry

	static void Main(string[] args)
	{
		try
		{
			Dictionary<ArgType, string> argList = Mapping.MapAndTrimPrefixes(args);

			string inputFile = argList.ContainsKey(ArgType.InputFile) ? argList[ArgType.InputFile] : null;
			string outputFile = argList.ContainsKey(ArgType.OutputFile) ? argList[ArgType.OutputFile] : null;
			string inputDir = argList.ContainsKey(ArgType.Directory) ? argList[ArgType.Directory] : null;
			string copyright = argList.ContainsKey(ArgType.Copyright) ? argList[ArgType.Copyright] : null;
			string timeStamp = argList.ContainsKey(ArgType.TimeStamp) ? argList[ArgType.TimeStamp] : null;
			bool warning = argList.ContainsKey(ArgType.Warning);
			bool prettyPrint = argList.ContainsKey(ArgType.PrettyPrint);

			if (!String.IsNullOrEmpty(inputDir))
			{
				if (!Directory.Exists(inputDir))
				{
					Console.Error.WriteLine("Input directory does not exist:\r\n"+inputDir);
					Console.Error.WriteLine(Program.Help, Assembly.GetExecutingAssembly().GetName().Version);
					return;
				}

				String[] inputFiles = Directory.GetFiles(inputDir, "*.jbst", SearchOption.AllDirectories);
				foreach (string file in inputFiles)
				{
					CompileOne(file, file+".js", copyright, timeStamp, warning, prettyPrint);
				}
				return;
			}

			if (String.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
			{
				Console.Error.WriteLine("Input file does not exist:\r\n" + inputFile);
				Console.Error.WriteLine(Program.Help, Assembly.GetExecutingAssembly().GetName().Version);
				return;
			}

			CompileOne(inputFile, outputFile, copyright, timeStamp, warning, prettyPrint);
		}
		catch (ParseException ex)
		{
			Console.Error.WriteLine(ex.GetCompilerMessage());
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
		}
	}

	private static void CompileOne(string inputFile, string outputFile, string copyright, string timeStamp, bool warning, bool prettyPrint)
	{
		List<ParseException> errors;
		if (String.IsNullOrEmpty(outputFile))
		{
			errors = Compile(inputFile, null, Console.Out, copyright, timeStamp, prettyPrint);
		}
		else
		{
			errors = Compile(inputFile, outputFile, copyright, timeStamp, prettyPrint);
		}

		if (errors.Count > 0)
		{
			foreach (ParseException ex in errors)
			{
				if (warning)
				{
					Console.Error.WriteLine(ex.GetCompilerMessage(warning));
				}
				else
				{
					Console.Error.WriteLine(ex.GetCompilerMessage());
				}
			}
		}
	}

	#endregion Program Entry

	#region Public Methods

	public static List<ParseException> Compile(
		string inputFile,
		string outputFile,
		string copyright,
		string timeStamp,
		bool prettyPrint)
	{
		if (!File.Exists(inputFile))
		{
			throw new FileNotFoundException(String.Format("File (\"{0}\") not found.", inputFile), inputFile);
		}

		if (inputFile.Equals(outputFile, StringComparison.InvariantCultureIgnoreCase))
		{
			throw new ApplicationException("Input and output file are set to the same path.");
		}

		FileUtility.PrepSavePath(outputFile);
		using (TextWriter output = File.CreateText(outputFile))
		{
			return Compile(inputFile, null, output, copyright, timeStamp, prettyPrint);
		}
	}

	public static List<ParseException> Compile(
		string inputFile,
		string inputSource,
		TextWriter output,
		string copyright,
		string timeStamp,
		bool prettyPrint)
	{
		if (output == null)
		{
			throw new NullReferenceException("Output TextWriter was null.");
		}

		if (String.IsNullOrEmpty(inputSource))
		{
			if (String.IsNullOrEmpty(inputFile))
			{
				throw new NullReferenceException("Input file path was empty.");
			}

			inputSource = File.ReadAllText(inputFile);
		}

		// write out header with copyright and timestamp
		WriteHeader(output, copyright, timeStamp);

		// verify, compact and write out results
		// parse JBST markup
		JbstWriter writer = new JbstWriter(inputFile);

		List<ParseException> errors = new List<ParseException>();
		try
		{
			HtmlDistiller parser = new HtmlDistiller();
			parser.EncodeNonAscii = false;
			parser.BalanceTags = false;
			parser.NormalizeWhitespace = false;
			parser.HtmlWriter = writer;
			parser.HtmlFilter = new NullHtmlFilter();
			parser.Parse(inputSource);
		}
		catch (ParseException ex)
		{
			errors.Add(ex);
		}
		catch (Exception ex)
		{
			errors.Add(new ParseError(ex.Message, inputFile, 0, 0, ex));
		}

		// render the pretty-printed version
		writer.Render(output);

		// return any errors
		return errors;
	}

	#endregion Public Methods

	#region Private Methods

	private static void WriteHeader(TextWriter writer, string copyright, string timeStamp)
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

			writer.WriteLine("/*".PadRight(width, '-')+"*\\");

			if (!String.IsNullOrEmpty(copyright))
			{
				writer.WriteLine("\t"+copyright);
			}

			if (!String.IsNullOrEmpty(timeStamp))
			{
				writer.WriteLine("\t"+timeStamp);
			}

			writer.WriteLine("\\*".PadRight(width, '-')+"*/");
		}
	}

	#endregion Private Methods
}
