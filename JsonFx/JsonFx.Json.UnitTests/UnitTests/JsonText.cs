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

using JsonFx.Json;

namespace JsonFx.Json.Test.UnitTests
{
	public class JsonText
	{
		#region Constants

		internal const string Seperator = "________________________________________\r\n";
		private const string UnitTestsUrl = "http://www.json.org/JSON_checker/test.zip";
		private const string UnitTestsFiles = "*.json";
		private const string ErrorMessage =
			Seperator+"\r\n"+
			"No unit tests were found.\r\n\r\n"+
			"Any "+UnitTestsFiles+" file in the {0} folder will be processed.\r\n"+
			"Download "+UnitTestsUrl+" and place contents into the {0} folder.";

		#endregion Constants

		#region Methods

		public static void RunTest(TextWriter writer, string unitTestsFolder, string outputFolder)
		{
			string[] unitTests = Directory.GetFiles(unitTestsFolder, UnitTestsFiles, SearchOption.AllDirectories);
			if (unitTests.Length > 0)
			{
				JsonReaderSettings readerSettings = new JsonReaderSettings();
				readerSettings.TypeHintName = StronglyTyped.MyTypeHintName;
				readerSettings.AllowNullValueTypes = true;
				readerSettings.AllowUnquotedObjectKeys = true;

				JsonWriterSettings writerSettings = new JsonWriterSettings();
				writerSettings.TypeHintName = StronglyTyped.MyTypeHintName;
				writerSettings.PrettyPrint = false;
				writerSettings.MaxDepth = 100;

				writer.WriteLine(JsonText.Seperator);
				writer.WriteLine("JsonReaderSettings:");
				new JsonWriter(writer).Write(readerSettings);

				writer.WriteLine(JsonText.Seperator);
				writer.WriteLine("JsonWriterSettings:");
				new JsonWriter(writer).Write(writerSettings);

				foreach (string unitTest in unitTests)
				{
					string source = String.Empty;

					try
					{
						writer.WriteLine(JsonText.Seperator);

						source = File.ReadAllText(unitTest);
						JsonReader jsonReader = new JsonReader(source, readerSettings);

						object obj, obj2;
						obj2 = obj = jsonReader.Deserialize();

						do
						{
							writer.WriteLine("READ: {0}", unitTest.Replace(unitTestsFolder, ""));
							writer.WriteLine("Result: {0}", (obj == null) ? "null" : obj.GetType().FullName);

							obj = jsonReader.Deserialize();
						} while (obj != null);

						string outputFile = unitTest.Replace(unitTestsFolder, outputFolder);
						string outputDir = Path.GetDirectoryName(outputFile);
						if (!Directory.Exists(outputDir))
						{
							Directory.CreateDirectory(outputDir);
						}
						using (JsonWriter jsonWriter = new JsonWriter(outputFile, writerSettings))
						{
							jsonWriter.Write(obj2);
						}
					}
					catch (JsonDeserializationException ex)
					{
						int col, line;
						ex.GetLineAndColumn(source, out line, out col);

						writer.WriteLine("ERROR: {0}", unitTest.Replace(unitTestsFolder, ""));
						writer.WriteLine("-- \"{0}\" ({1}, {2})", ex.Message, line, col);
						continue;
					}
					catch (Exception ex)
					{
						writer.WriteLine("ERROR: {0}", unitTest.Replace(unitTestsFolder, ""));
						writer.WriteLine("-- \"{0}\"", ex.Message);
						continue;
					}
				}
			}
			else
			{
				writer.WriteLine(ErrorMessage);
			}
		}

		#endregion Methods
	}
}
