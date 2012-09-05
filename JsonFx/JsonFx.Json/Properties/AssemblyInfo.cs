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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("JsonFx for A* Pathfinding Project")]
[assembly: AssemblyDescription("JsonFx.NET JSON Serialization")]
[assembly: AssemblyProduct("JsonFx.NET.Unity")]
[assembly: AssemblyCopyright("Copyright © 2006-2009 Stephen M. McKamey. All rights reserved.")]
[assembly: AssemblyCompany("http://jsonfx.net")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#elif STRONG
[assembly: AssemblyConfiguration("Signed")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: ComVisible(false)]
[assembly: Guid("a3a74921-2901-456c-84f2-b0d83994d33a")]
[assembly: ObfuscateAssembly(false, StripAfterObfuscation=false)]
