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
using System.Web;

using JsonFx.Client;
using JsonFx.Json;
using JsonFx.UI.Jbst;
using JsonFx.Compilation;

namespace JsonFx.Mvc
{
	public static class Jbst
	{
		#region Properties

		private static bool IsDebug
		{
			get
			{
				// TODO: find a better way to access this
				HttpContext context = HttpContext.Current;
				return context != null && context.IsDebuggingEnabled;
			}
		}

		#endregion Properties

		#region JBST Helper Methods

		/// <summary>
		/// Bind the JBST to the provided data.
		/// </summary>
		/// <param name="jbstName"></param>
		/// <param name="dataName">named data to bind</param>
		/// <param name="dataItems">collection of data to emit</param>
		/// <returns></returns>
		public static string Bind(EcmaScriptIdentifier jbstName, EcmaScriptIdentifier dataName, IDictionary<string, object> dataItems)
		{
			StringWriter writer = new StringWriter();
			JbstBuildResult jbst = JbstBuildResult.FindJbst(jbstName);
			jbst.IsDebug = IsDebug;

			if (dataItems != null)
			{
				// render data block
				new DataBlockWriter(jbst.AutoMarkup, jbst.IsDebug).Write(writer, dataItems);
			}

			// render the JBST
			jbst.Write(writer, dataName);

			return writer.ToString();
		}

		/// <summary>
		/// Bind the JBST to the provided data.
		/// </summary>
		/// <param name="jbstName"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string Bind(EcmaScriptIdentifier jbstName, object data)
		{
			StringWriter writer = new StringWriter();

			// render the JBST
			JbstBuildResult jbst = JbstBuildResult.FindJbst(jbstName);
			jbst.IsDebug = IsDebug;
			jbst.Write(writer, data);

			return writer.ToString();
		}

		/// <summary>
		/// Bind the JBST to the provided data.
		/// </summary>
		/// <param name="jbstName"></param>
		/// <param name="data"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static string Bind(EcmaScriptIdentifier jbstName, object data, int index, int count)
		{
			StringWriter writer = new StringWriter();

			// render the JBST
			JbstBuildResult jbst = JbstBuildResult.FindJbst(jbstName);
			jbst.IsDebug = IsDebug;
			jbst.Write(writer, data, index, count);

			return writer.ToString();
		}

		#endregion JBST Helper Methods

		#region Script Data Helper Methods

		/// <summary>
		/// Emit the provided data as JavaScript variables.
		/// </summary>
		/// <param name="name">name of the data</param>
		/// <param name="data">data contents</param>
		/// <returns></returns>
		public static string ScriptData(string name, object data)
		{
			Dictionary<string, object> dataItems = new Dictionary<string, object>();

			dataItems[name] = data;

			return ScriptData(dataItems);
		}

		/// <summary>
		/// Emit the provided data as JavaScript variables.
		/// </summary>
		/// <param name="data">data contents</param>
		/// <returns></returns>
		public static string ScriptData(IDictionary<string, object> dataItems)
		{
			StringWriter writer = new StringWriter();

			// render data block
			DataBlockWriter dataBlock = new DataBlockWriter();
			dataBlock.IsDebug = IsDebug;
			dataBlock.Write(writer, dataItems);

			return writer.ToString();
		}

		#endregion Script Data Helper Methods

		#region Resource HelperMethods

		/// <summary>
		/// Include optimized resources
		/// </summary>
		/// <param name="compactUrl"></param>
		/// <param name="debugUrl"></param>
		/// <returns></returns>
		public static string ResourceInclude(string debugUrl, string compactUrl)
		{
			string url = MergeResourceCodeProvider.JoinAlternates(debugUrl, compactUrl);

			return ResourceInclude(url);
		}

		/// <summary>
		/// Include optimized resources
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static string ResourceInclude(string url)
		{
			StringWriter writer = new StringWriter();

			ResourceBuildResult result = ResourceBuildResult.FindResource(url);
			result.IsDebug = IsDebug;
			result.Write(writer);

			return writer.ToString();
		}

		#endregion Resource HelperMethods
	}
}
