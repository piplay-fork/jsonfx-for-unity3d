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

using JsonFx.Client;
using JsonFx.Handlers;
using JsonFx.Json;

namespace JsonFx.UI.Jbst
{
	internal class SimpleJbstBuildResult :
		JbstBuildResult,
		IOptimizedResult
	{
		#region Fields

		private string source;
		private string prettyPrinted;
		private string compacted;
		private string fileExtension;
		private string hash;
		private string contentType;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="writer"></param>
		public SimpleJbstBuildResult(EcmaScriptIdentifier jbstName, AutoMarkupType autoMarkup)
			: base(jbstName, autoMarkup)
		{
		}

		#endregion Init

		#region IBuildResult Members

		public string ContentType
		{
			get { return this.contentType; }
			internal set { this.contentType = value; }
		}

		public string FileExtension
		{
			get { return this.fileExtension; }
			internal set { this.fileExtension = value; }
		}

		public string Hash
		{
			get { return this.hash; }
			internal set { this.hash = value; }
		}

		#endregion IBuildResult Members

		#region IOptimizedResult Members

		public string Source
		{
			get { return this.source; }
			internal set { this.source = value; }
		}

		public string PrettyPrinted
		{
			get { return this.prettyPrinted; }
			internal set { this.prettyPrinted = value; }
		}

		public string Compacted
		{
			get { return this.compacted; }
			internal set { this.compacted = value; }
		}

		public byte[] Gzipped
		{
			get { throw new NotImplementedException(); }
		}

		public byte[] Deflated
		{
			get { throw new NotImplementedException(); }
		}

		#endregion IOptimizedResult Members
	}
}
