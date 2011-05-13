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
using System.IO;

namespace JsonFx.BuildTools.IO
{
	public class FileUtility
	{
		#region Constants

		private static readonly char[] IllegalChars;

		#endregion Constants

		#region Init

		static FileUtility()
		{
			List<char> chars = new List<char>(Path.GetInvalidPathChars());
			foreach (char ch in Path.GetInvalidFileNameChars())
			{
				if (!chars.Contains(ch) && ch != Path.DirectorySeparatorChar)
				{
					chars.Add(ch);
				}
			}
			FileUtility.IllegalChars = chars.ToArray();
		}

		#endregion Init

		#region Methods

		/// <summary>
		/// Makes sure directory exists and if file exists is not readonly.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns>if valid path</returns>
		public static bool PrepSavePath(string filename)
		{
			if (File.Exists(filename))
			{
				// make sure not readonly
				FileAttributes attributes = File.GetAttributes(filename);
				attributes &= ~FileAttributes.ReadOnly;
				File.SetAttributes(filename, attributes);
			}
			else
			{
				string dir = Path.GetDirectoryName(filename);
				if (!String.IsNullOrEmpty(dir) && dir.IndexOfAny(FileUtility.IllegalChars) >= 0)
				{
					return false;
				}
				if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					// make sure path exists
					Directory.CreateDirectory(dir);
				}
				string file = Path.GetFileName(filename);
				if (!String.IsNullOrEmpty(file) && file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				{
					return false;
				}
			}
			return true;
		}

		#endregion Methods
	}
}
