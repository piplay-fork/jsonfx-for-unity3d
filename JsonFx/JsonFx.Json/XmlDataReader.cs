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

#if !UNITY3D
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using JsonFx.Json;

namespace JsonFx.Xml
{
	/// <summary>
	/// An <see cref="IDataReader"/> adapter for <see cref="XmlReader"/>
	/// </summary>
	public class XmlDataReader : IDataReader
	{
		#region Constants

		public const string XmlMimeType = XmlDataWriter.XmlMimeType;

		#endregion Constants

		#region Fields

		private readonly XmlReaderSettings Settings;
		private readonly XmlSerializerNamespaces Namespaces;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="namespaces"></param>
		public XmlDataReader(XmlReaderSettings settings, XmlSerializerNamespaces namespaces)
		{
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}
			this.Settings = settings;

			if (namespaces == null)
			{
				namespaces = new XmlSerializerNamespaces();
				namespaces.Add(String.Empty, String.Empty);// tricks the serializer into not emitting default xmlns attributes
			}
			this.Namespaces = namespaces;
		}

		#endregion Init

		#region IDataSerializer Members

		/// <summary>
		/// Gets the content type
		/// </summary>
		public string ContentType
		{
			get { return XmlDataWriter.XmlMimeType; }
		}

		/// <summary>
		/// Deserializes a data object from the given input
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public object Deserialize(TextReader input, Type type)
		{
			XmlReader reader = XmlReader.Create(input, this.Settings);

			// skip DocType / processing instructions
			reader.MoveToContent();

			// serialize feed
			XmlSerializer serializer = new XmlSerializer(type);
			return serializer.Deserialize(reader);
		}

		#endregion IDataSerializer Members

		#region Methods

		/// <summary>
		/// Builds a common settings objects
		/// </summary>
		/// <returns></returns>
		public static XmlReaderSettings CreateSettings()
		{
			// setup document formatting
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.CloseInput = false;
			settings.ConformanceLevel = ConformanceLevel.Auto;
			settings.IgnoreComments = true;
			settings.IgnoreWhitespace = true;
			settings.IgnoreProcessingInstructions = true;
			return settings;
		}

		#endregion Methods
	}
}
#endif