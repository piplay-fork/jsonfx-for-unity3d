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
using System.Reflection;
using System.Web.Compilation;

namespace JsonFx.Compilation
{
	[AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
	public class BuildPathAttribute : Attribute
	{
		#region Fields

		private readonly string VirtualPath;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public BuildPathAttribute(string virtualPath)
		{
			this.VirtualPath = virtualPath;
		}

		#endregion Init

		#region Methods

		/// <summary>
		/// Gets the corresponding virtual path for the source of the given generated type.
		/// </summary>
		/// <param name="type">the Type marked with VirtualPathAttribute</param>
		/// <returns>virtual path of the source</returns>
		public static string GetVirtualPath(Type type)
		{
			if (type == null)
			{
				return null;
			}

			BuildPathAttribute attrib = Attribute.GetCustomAttribute(type, typeof(BuildPathAttribute), false) as BuildPathAttribute;
			if (attrib == null)
			{
				return null;
			}

			return attrib.VirtualPath;
		}

		#endregion Methods
	}

	public sealed class BuildCache
	{
		#region Fields

		public static readonly BuildCache Instance = new BuildCache();
		private readonly IDictionary<string, string> Cache = new Dictionary<string, string>(StringComparer.Ordinal);

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		private BuildCache()
		{
		}

		#endregion Init

		#region Methods

		/// <summary>
		/// Creates an instance from the simple type name
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public T Create<T>(string typeName)
			where T : class
		{
			string virtualPath = this.GetVirtualPath(typeName);
			if (String.IsNullOrEmpty(virtualPath))
			{
				return default(T);
			}

			// instantiate using cached path
			return BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(object)) as T;
		}

		/// <summary>
		/// Gets the type from the simple type name
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public Type GetType(string typeName)
		{
			string virtualPath = this.GetVirtualPath(typeName);
			if (String.IsNullOrEmpty(virtualPath))
			{
				return null;
			}

			// return using cached path
			return BuildManager.GetCompiledType(virtualPath);
		}

		/// <summary>
		/// Gets the virtual path of the source of the generated type
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public string GetVirtualPath(string typeName)
		{
			string virtualPath;

			lock (this.Cache)
			{
				// check the cache to prevent extra lookups
				if (this.Cache.TryGetValue(typeName, out virtualPath))
				{
					return virtualPath;
				}
			}

			// precompiled sites can bring back directly since already loaded in AppDomain
			Type type = BuildManager.GetType(typeName, false, false);
			if (type == null)
			{
				// dynamic apps compile to a temp directory
				foreach (string asm in Directory.GetFiles(AppDomain.CurrentDomain.DynamicDirectory, "App_Web_*.dll", SearchOption.TopDirectoryOnly))
				{
					type = Assembly.LoadFrom(asm).GetType(typeName, false, false);
					if (type != null)
					{
						break;
					}
				}
			}

			// save the virtual path in cache for future lookup
			virtualPath = BuildPathAttribute.GetVirtualPath(type);
			if (String.IsNullOrEmpty(virtualPath))
			{
				lock (this.Cache)
				{
					// mark as not found for future lookups
					this.Cache[typeName] = virtualPath = String.Empty;
				}
			}
			else
			{
				lock (this.Cache)
				{
					// apply to both just in case discrepancy
					this.Cache[type.FullName] = this.Cache[typeName] = virtualPath;
				}
			}

			return virtualPath;
		}

		#endregion Methods
	}
}
