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

using JsonFx.BuildTools.Collections;

namespace JsonFx.BuildTools.IO
{
	/// <summary>
	/// Defines a character sequence to filter out when reading.
	/// </summary>
	/// <remarks>
	/// If the sequence exists in the read source, it will be read out as if it was never there.
	/// </remarks>
	public struct ReadFilter
	{
		#region Fields

		public readonly string StartToken;
		public readonly string EndToken;

		#endregion Fields

		#region Init

		public ReadFilter(string start, string end)
		{
			if (String.IsNullOrEmpty(start))
			{
				throw new ArgumentNullException("start");
			}
			if (String.IsNullOrEmpty(end))
			{
				throw new ArgumentNullException("end");
			}

			this.StartToken = start;
			this.EndToken = end;
		}

		#endregion Init
	}

	/// <summary>
	/// Creates a Trie out of ReadFilters
	/// </summary>
	public class FilterTrie : TrieNode<char, string>
	{
		#region Constants

		private const int DefaultTrieWidth = 1;

		#endregion Constants

		#region Init

		public FilterTrie(IEnumerable<ReadFilter> filters) : base(DefaultTrieWidth)
		{
			// load trie
			foreach (ReadFilter filter in filters)
			{
				TrieNode<char, string> node = this;

				// build out the path for StartToken
				foreach (char ch in filter.StartToken)
				{
					if (!node.Contains(ch))
					{
						node[ch] = new TrieNode<char, string>(DefaultTrieWidth);
					}

					node = (TrieNode<char, string>)node[ch];
				}

				// at the end of StartToken path is the EndToken
				node.Value = filter.EndToken;
			}
		}

		#endregion Init
	}
}
