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
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using JsonFx.Json;

namespace JsonFx.Xml
{
	/// <summary>
	/// An <see cref="IDataWriter"/> adapter for <see cref="XmlWriter"/>
	/// </summary>
	public class XmlDataWriter : IDataWriter
	{
		#region Constants

		public const string XmlMimeType = "application/xml";
		public const string XmlFileExtension = ".xml";

		#endregion Constants

		#region Fields

		private readonly XmlWriterSettings Settings;
		private readonly XmlSerializerNamespaces Namespaces;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="namespaces"></param>
		public XmlDataWriter(XmlWriterSettings settings, XmlSerializerNamespaces namespaces)
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
		/// Gets the content encoding
		/// </summary>
		public Encoding ContentEncoding
		{
			get { return this.Settings.Encoding ?? Encoding.UTF8; }
		}

		/// <summary>
		/// Gets the content type
		/// </summary>
		public string ContentType
		{
			get { return XmlDataWriter.XmlMimeType; }
		}

		/// <summary>
		/// Gets the file extension
		/// </summary>
		public string FileExtension
		{
			get { return XmlDataWriter.XmlFileExtension; }
		}

		/// <summary>
		/// Serializes the data object to the given output
		/// </summary>
		/// <param name="output"></param>
		/// <param name="data"></param>
		public void Serialize(TextWriter output, object data)
		{
			if (data == null)
			{
				return;
			}

			if (this.Settings.Encoding == null)
			{
				this.Settings.Encoding = this.ContentEncoding;
			}
			XmlWriter writer = XmlWriter.Create(output, this.Settings);

			// serialize feed
			XmlSerializer serializer = new XmlSerializer(data.GetType());
			serializer.Serialize(writer, data, this.Namespaces);
		}

		#endregion IDataSerializer Members

		#region Methods

		/// <summary>
		/// Builds a common settings objects
		/// </summary>
		/// <param name="encoding"></param>
		/// <param name="prettyPrint"></param>
		/// <returns></returns>
		public static XmlWriterSettings CreateSettings(Encoding encoding, bool prettyPrint)
		{
			// setup document formatting
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.CheckCharacters = true;
			settings.CloseOutput = false;
			settings.ConformanceLevel = ConformanceLevel.Auto;
			settings.Encoding = encoding;
			settings.OmitXmlDeclaration = true;

			if (prettyPrint)
			{
				// make human readable
				settings.Indent = true;
				settings.IndentChars = "\t";
			}
			else
			{
				// compact
				settings.Indent = false;
				settings.NewLineChars = String.Empty;
			}
			settings.NewLineHandling = NewLineHandling.Replace;

			return settings;
		}

		#endregion Methods
	}
}
#endif