#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2006-2008 Stephen M. McKamey

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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

using JsonFx.Json;

namespace JsonFx.Json.Test.UnitTests
{
	/* A set of objects used to test strongly-typed serialization */

	public class StronglyTyped
	{
		#region Constants

		public const string MyTypeHintName = "__type";

		#endregion Constants

		#region Methods

		public static void RunTest(TextWriter writer, string unitTestsFolder, string outputFolder)
		{
			JsonReaderSettings readerSettings = new JsonReaderSettings();
			readerSettings.TypeHintName = StronglyTyped.MyTypeHintName;
			readerSettings.AllowNullValueTypes = true;
			readerSettings.AllowUnquotedObjectKeys = false;

			JsonWriterSettings writerSettings = new JsonWriterSettings();
			writerSettings.TypeHintName = StronglyTyped.MyTypeHintName;
			writerSettings.PrettyPrint = true;
			writerSettings.MaxDepth = 100;

			writer.WriteLine(JsonText.Seperator);
			writer.WriteLine("JsonReaderSettings:");
			new JsonWriter(writer).Write(readerSettings);

			writer.WriteLine(JsonText.Seperator);
			writer.WriteLine("JsonWriterSettings:");
			new JsonWriter(writer).Write(writerSettings);

			#region Simple Root Types

			SerializeDeserialize(writer, unitTestsFolder, "RootEnum.json", BlahBlah.Three, readerSettings, writerSettings);

			SerializeDeserialize(writer, unitTestsFolder, "RootInt64.json", 42678L, readerSettings, writerSettings);

			SerializeDeserialize(writer, unitTestsFolder, "RootDateTime.json", DateTime.Now, readerSettings, writerSettings);

			#endregion Simple Root Types

			#region Strongly Typed Object Graph Test

			ComplexObject collectionTest = new ComplexObject();

			collectionTest.MyNested = new NestedObject();
			collectionTest.MyNested.Items = new Dictionary<string, object>();
			collectionTest.MyNested.Items["First"] = 3.14159;
			collectionTest.MyNested.Items["Second"] = "Hello World";
			collectionTest.MyNested.Items["Third"] = 42;
			collectionTest.MyNested.Items["Fourth"] = true;

			collectionTest.MyNested.Hash = new Hashtable(collectionTest.MyNested.Items);

			collectionTest.MyNested.Hybrid = new HybridDictionary();
			foreach (string key in collectionTest.MyNested.Items.Keys)
			{
				collectionTest.MyNested.Hybrid[key] = collectionTest.MyNested.Items[key];
			}

			// populate with an Array
			collectionTest.MyArray = new SimpleObject[]{
						new SimpleObject(BlahBlah.Four),
						new SimpleObject(BlahBlah.Three),
						new SimpleObject(BlahBlah.Two),
						new SimpleObject(BlahBlah.One),
						new SimpleObject()
					};

			// duplicate for ArrayList
			collectionTest.MyArrayList = new ArrayList(collectionTest.MyArray);

			// duplicate for List<T>
			collectionTest.MyList = new List<SimpleObject>(collectionTest.MyArray);

			// duplicate for LinkedList<T>
			collectionTest.MyLinkedList = new LinkedList<SimpleObject>(collectionTest.MyArray);

			// duplicate for Stack<T>
			collectionTest.MyStack = new Stack<SimpleObject>(collectionTest.MyArray);

			// duplicate for Queue<T>
			collectionTest.MyQueue = new Queue<SimpleObject>(collectionTest.MyArray);

			SerializeDeserialize(writer, unitTestsFolder, "StronglyTyped.json", collectionTest, readerSettings, writerSettings);

			#endregion Strongly Typed Object Graph Test

			#region Non-IDictionary, IDictionary<TKey, TValue> Test

			NotIDictionary notIDictionary = new NotIDictionary();
			notIDictionary["This Collection"] = "is not an IDictionary";
			notIDictionary["But It is"] = "an IDictionary<string, object>";

			SerializeDeserialize(writer, unitTestsFolder, "NotIDictionary.json", notIDictionary, readerSettings, writerSettings);

			#endregion Non-IDictionary ,IDictionary<TKey, TValue> Test
		}

		private static void SerializeDeserialize(TextWriter writer, string unitTestsFolder, string unitTestFile, object obj, JsonReaderSettings readerSettings, JsonWriterSettings writerSettings)
		{
			writer.WriteLine(JsonText.Seperator);

			string source = String.Empty;
			try
			{
				using (JsonWriter jsonWriter = new JsonWriter(unitTestsFolder+unitTestFile, writerSettings))
				{
					jsonWriter.Write(obj);
				}

				source = File.ReadAllText(unitTestsFolder+unitTestFile);

				obj = new JsonReader(source, readerSettings).Deserialize((obj == null) ? null : obj.GetType());
				writer.WriteLine("READ: "+unitTestFile);
				writer.WriteLine("Result: {0}", (obj == null) ? "null" : obj.GetType().FullName);
			}
			catch (JsonDeserializationException ex)
			{
				int col, line;
				writer.WriteLine("ERROR: "+unitTestFile);
				ex.GetLineAndColumn(source, out line, out col);
				writer.WriteLine("-- \"{0}\" ({1}, {2})", ex.Message, line, col);
			}
			catch (Exception ex)
			{
				writer.WriteLine("ERROR: "+unitTestFile);
				writer.WriteLine("-- \"{0}\"", ex.Message);
			}
		}

		#endregion Methods
	}

	public class ComplexObject
	{
		#region Fields

		private Decimal myDecimal = 0.12345678901234567890123456789m;
		private Guid myGuid = Guid.NewGuid();
		private TimeSpan myTimeSpan = new TimeSpan(5, 4, 3, 2, 1);
		private Version myVersion = new Version(1, 2, 3, 4);
		private Uri myUri = new Uri("http://jsonfx.net/BuildTools");
		private DateTime myDateTime = DateTime.UtcNow;
		private Nullable<Int32> myNullableInt32 = null;
		private Nullable<Int64> myNullableInt64a = null;
		private Nullable<Int64> myNullableInt64b = 42;
		private Nullable<DateTime> myNullableDateTime = DateTime.Now;
		private SimpleObject[] myArray = null;
		private ArrayList myArrayList = null;
		private List<SimpleObject> myList = null;
		private LinkedList<SimpleObject> myLinkedList = null;
		private Stack<SimpleObject> myStack = null;
		private Queue<SimpleObject> myQueue = null;
		private NestedObject myNested = null;

		#endregion Fields

		#region Properties

		[JsonName("AnArbitraryRenameForMyNestedProperty")]
		public NestedObject MyNested
		{
			get { return this.myNested; }
			set { this.myNested = value; }
		}

		public Decimal MyDecimal
		{
			get { return this.myDecimal; }
			set { this.myDecimal = value; }
		}

		public Guid MyGuid
		{
			get { return this.myGuid; }
			set { this.myGuid = value; }
		}

		public TimeSpan MyTimeSpan
		{
			get { return this.myTimeSpan; }
			set { this.myTimeSpan = value; }
		}

		public Version MyVersion
		{
			get { return this.myVersion; }
			set { this.myVersion = value; }
		}

		public Uri MyUri
		{
			get { return this.myUri; }
			set { this.myUri = value; }
		}

		public DateTime MyDateTime
		{
			get { return this.myDateTime; }
			set { this.myDateTime = value; }
		}

		public Nullable<Int32> MyNullableInt32
		{
			get { return this.myNullableInt32; }
			set { this.myNullableInt32 = value; }
		}

		public Nullable<DateTime> MyNullableDateTime
		{
			get { return this.myNullableDateTime; }
			set { this.myNullableDateTime = value; }
		}

		[DefaultValue(null)]
		public Nullable<Int64> MyNullableInt64a
		{
			get { return this.myNullableInt64a; }
			set { this.myNullableInt64a = value; }
		}

		[DefaultValue(null)]
		public Nullable<Int64> MyNullableInt64b
		{
			get { return this.myNullableInt64b; }
			set { this.myNullableInt64b = value; }
		}

		public SimpleObject[] MyArray
		{
			get { return this.myArray; }
			set { this.myArray = value; }
		}

		public ArrayList MyArrayList
		{
			get { return this.myArrayList; }
			set { this.myArrayList = value; }
		}

		public List<SimpleObject> MyList
		{
			get { return this.myList; }
			set { this.myList = value; }
		}

		[JsonSpecifiedProperty("SerializeMyStack")]
		public Stack<SimpleObject> MyStack
		{
			get { return this.myStack; }
			set { this.myStack = value; }
		}

		[JsonIgnore]
		public bool SerializeMyStack
		{
			get { return false; }
			set { /* do nothing */ }
		}

		public LinkedList<SimpleObject> MyLinkedList
		{
			get { return this.myLinkedList; }
			set { this.myLinkedList = value; }
		}

		public Queue<SimpleObject> MyQueue
		{
			get { return this.myQueue; }
			set { this.myQueue = value; }
		}

		#endregion Properties
	}

	public class NotIDictionary : IDictionary<string, object>
	{
		#region Fields

		private IDictionary<string, object> dictionary = new Dictionary<string, object>();

		#endregion Fields

		#region IDictionary<string,object> Members

		public object this[string key]
		{
			get { return this.dictionary[key]; }
			set { this.dictionary[key] = value; }
		}

		void IDictionary<string, object>.Add(string key, object value)
		{
			this.dictionary.Add(key, value);
		}

		bool IDictionary<string, object>.ContainsKey(string key)
		{
			return this.dictionary.ContainsKey(key);
		}

		ICollection<string> IDictionary<string, object>.Keys
		{
			get { return this.dictionary.Keys; }
		}

		bool IDictionary<string, object>.Remove(string key)
		{
			return this.dictionary.Remove(key);
		}

		bool IDictionary<string, object>.TryGetValue(string key, out object value)
		{
			return this.dictionary.TryGetValue(key, out value);
		}

		ICollection<object> IDictionary<string, object>.Values
		{
			get { return this.dictionary.Values; }
		}

		#endregion IDictionary<string,object> Members

		#region ICollection<KeyValuePair<string,object>> Members

		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
		{
			((ICollection<KeyValuePair<string, object>>)this.dictionary).Add(item);
		}

		void ICollection<KeyValuePair<string, object>>.Clear()
		{
			((ICollection<KeyValuePair<string, object>>)this.dictionary).Clear();
		}

		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
		{
			return ((ICollection<KeyValuePair<string, object>>)this.dictionary).Contains(item);
		}

		void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, object>>)this.dictionary).CopyTo(array, arrayIndex);
		}

		int ICollection<KeyValuePair<string, object>>.Count
		{
			get { return ((ICollection<KeyValuePair<string, object>>)this.dictionary).Count; }
		}

		bool ICollection<KeyValuePair<string, object>>.IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<string, object>>)this.dictionary).IsReadOnly; }
		}

		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
		{
			return ((ICollection<KeyValuePair<string, object>>)this.dictionary).Remove(item);
		}

		#endregion ICollection<KeyValuePair<string,object>> Members

		#region IEnumerable<KeyValuePair<string,object>> Members

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, object>>)this.dictionary).GetEnumerator();
		}

		#endregion IEnumerable<KeyValuePair<string,object>> Members

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.dictionary).GetEnumerator();
		}

		#endregion IEnumerable Members
	}

	public class NestedObject
	{
		#region Fields

		private Dictionary<string, object> items = null;
		private Hashtable hash = null;
		private HybridDictionary hybrid = null;

		#endregion Fields

		#region Properties

		public Dictionary<string, object> Items
		{
			get { return this.items; }
			set { this.items = value; }
		}

		public Hashtable Hash
		{
			get { return this.hash; }
			set { this.hash = value; }
		}

		public HybridDictionary Hybrid
		{
			get { return this.hybrid; }
			set { this.hybrid = value; }
		}

		#endregion Properties
	}

	public class SimpleObject
	{
		#region Constants

		private static readonly Random Rand = new Random();

		#endregion Constants

		#region Fields

		double random;
		BlahBlah blah = BlahBlah.None;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public SimpleObject()
		{
			this.random = Rand.NextDouble();
		}

		/// <summary>
		/// Ctor
		/// </summary>
		public SimpleObject(BlahBlah blah) : this()
		{
			this.Blah = blah;
		}

		#endregion Init

		#region Properties

		public BlahBlah Blah
		{
			get { return this.blah; }
			set { this.blah = value; }
		}

		public double Random
		{
			get { return this.random; }
			set { this.random = value; }
		}

		#endregion Properties

		#region Object Overrides

		public override string ToString()
		{
			return String.Format(
				"SimpleObject: {0}, {1}",
				this.Blah,
				this.Random);
		}

		#endregion Object Overrides
	}

	public enum BlahBlah
	{
		None,
		One,
		Two,
		Three,
		Four
	}

	public class ErrorProneSimpleObject : SimpleObject
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ErrorProneSimpleObject()
		{
			throw new NotImplementedException("ErrorProneSimpleObject always throws an error in the constructor.");
		}

		#endregion Init
	}
}
