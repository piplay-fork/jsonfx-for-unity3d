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
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;

using JsonFx.Json;

namespace JsonFx.Mvc
{
	/// <summary>
	/// Deserializes data according to a specified format
	/// </summary>
	public class DataModelBinder : IModelBinder
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="provider"></param>
		public DataModelBinder(IDataReaderProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}

			this.Provider = provider;
			this.DefaultBinder = new DefaultModelBinder();
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the binder used if the provider cannot find a matching IDataReaders 
		/// </summary>
		public IModelBinder DefaultBinder
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the provider which finds the matching IDataReader
		/// </summary>
		public IDataReaderProvider Provider
		{
			get;
			private set;
		}

		#endregion Properties

		#region IModelBinder Members

		/// <summary>
		/// Reads the request body using the supplied IDataReader
		/// </summary>
		/// <param name="controllerContext"></param>
		/// <param name="bindingContext"></param>
		/// <returns></returns>
		public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			HttpRequestBase request = controllerContext.HttpContext.Request;

			IDataReader reader = this.Provider.Find(request.ContentType);
			if (reader == null)
			{
				return this.DefaultBinder.BindModel(controllerContext, bindingContext);
			}

			return reader.Deserialize(
				new StreamReader(request.InputStream, request.ContentEncoding??Encoding.UTF8),
				bindingContext.ModelType);
		}

		#endregion IModelBinder Members
	}
}
