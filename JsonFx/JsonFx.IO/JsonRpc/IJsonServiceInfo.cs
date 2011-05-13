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
using System.Web.Compilation;
using System.Reflection;

using JsonFx.Handlers;

namespace JsonFx.JsonRpc
{
	/// <summary>
	/// Runtime adapter between service infrastructure and service implementation
	/// </summary>
	public interface IJsonServiceInfo
	{
		#region Properties

		/// <summary>
		/// Gets the Type used to implement the service
		/// </summary>
		Type ServiceType { get; }

		#endregion Properties

		#region Methods

		/// <summary>
		/// Factory Method for the Service class
		/// </summary>
		/// <returns></returns>
		Object CreateService();

		/// <summary>
		/// Method map for the Service class
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		MethodInfo ResolveMethodName(string name);

		/// <summary>
		/// Param map for the Service class
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		string[] GetMethodParams(string name);

		#endregion Methods
	}
}
