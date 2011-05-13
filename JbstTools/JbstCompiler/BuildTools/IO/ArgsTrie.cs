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
	/// Defines the prefix for mapping args to an indexed array
	/// </summary>
	public struct ArgsMap<TValue>
	{
		#region Fields

		public readonly string Prefix;
		public readonly TValue Index;

		#endregion Fields

		#region Init

		public ArgsMap(string prefix, TValue index)
		{
			if (String.IsNullOrEmpty(prefix))
			{
				throw new ArgumentNullException("prefix");
			}

			this.Prefix = prefix;
			this.Index = index;
		}

		#endregion Init
	}

	/// <summary>
	/// Creates a Trie out of ReadFilters
	/// </summary>
	public class ArgsTrie<TValue> : TrieNode<char, TValue>
	{
		#region Constants

		private const int DefaultTrieWidth = 3;
		private readonly bool CaseSensitive;

		#endregion Constants

		#region Init

		public ArgsTrie(IEnumerable<ArgsMap<TValue>> mappings) : this(mappings, false) { }

		public ArgsTrie(IEnumerable<ArgsMap<TValue>> mappings, bool caseSensitive)
			: base(DefaultTrieWidth)
		{
			this.CaseSensitive = caseSensitive;

			// load trie
			foreach (ArgsMap<TValue> map in mappings)
			{
				TrieNode<char, TValue> node = this;

				string prefix = caseSensitive ? map.Prefix : map.Prefix.ToLowerInvariant();

				// build out the path for StartToken
				foreach (char ch in prefix)
				{
					if (!node.Contains(ch))
					{
						node[ch] = new TrieNode<char, TValue>(DefaultTrieWidth);
					}

					node = (TrieNode<char, TValue>)node[ch];
				}

				// at the end of the Prefix is the Index
				node.Value = map.Index;
			}
		}

		#endregion Init

		#region Methods

		public Dictionary<TValue, string> MapAndTrimPrefixes(string[] args)
		{
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}

			Dictionary<TValue, string> map = new Dictionary<TValue, string>(args.Length);
			foreach (string arg in args)
			{
				ITrieNode<char, TValue> node = this;

				// walk each char of arg until match prefix
				for (int i=0; i<arg.Length; i++)
				{
					if (this.CaseSensitive)
					{
						node = node[arg[i]];
					}
					else
					{
						node = node[Char.ToLowerInvariant(arg[i])];
					}

					if (node == null)
					{
						// no prefix found
						throw new ArgumentException("Unrecognized argument", arg);
					}

					if (node.HasValue)
					{
						// prefix found
						string value = arg.Substring(i+1);
						if (String.IsNullOrEmpty(value))
						{
							value = String.Empty;
						}
						map[node.Value] = value;
						break;
					}
				}
			}
			return map;
		}

		#endregion Methods
	}
}
