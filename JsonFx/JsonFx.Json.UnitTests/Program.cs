#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2008 Stephen M. McKamey

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
using System.Text;
using System.Diagnostics;

using JsonFx.Json;

namespace JsonFx.Json.Test
{
	class Program
	{
		#region Constants

		private const string ReportPath = "{0:yyyy-MM-dd-HHmmss}_Report.txt";
		private const string UnitTestsFolder = @".\UnitTests\";
		private const string OutputFolder = @".\Output\";
		private const string HeaderMessage =
			"NOTE: JsonFx.Json accepts all valid JSON and can recover from many minor errors.\r\n\r\n"+
			"Unit Test Report ({0:yyyy-MM-dd @ HH:mm:ss})";

		#endregion Constants

		#region Program Entry

		static void Main(string[] args)
		{
			DateTime now = DateTime.Now;
			string reportPath = String.Format(ReportPath, now);
			const int Count = 100;
			double total = 0;

			for (int i=0; i<Count; i++)
			{
				using (StreamWriter writer = new StreamWriter(reportPath, false, Encoding.UTF8))
				{
					writer.WriteLine(HeaderMessage, now);

					Stopwatch watch = Stopwatch.StartNew();

					UnitTests.StronglyTyped.RunTest(writer, UnitTestsFolder, OutputFolder);

					UnitTests.JsonText.RunTest(writer, UnitTestsFolder, OutputFolder);

					watch.Stop();

					writer.WriteLine(UnitTests.JsonText.Seperator);
					writer.WriteLine("Elapsed: {0} ms", watch.Elapsed.TotalMilliseconds);

					total += watch.Elapsed.TotalMilliseconds;
				}
			}

			Console.WriteLine("Average after {1} tries: {0} ms", total/Count, Count);

			Process process = new Process();
			process.StartInfo.FileName = "notepad.exe";
			process.StartInfo.Arguments = reportPath;
			process.Start();
		}

		#endregion Program Entry
	}
}
