using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JsonFx.Json.UnitTests;
using NUnit.Framework;

namespace JsonFx.Json
{
    [TestFixture()]
    class SurrogateTest
    {
        	
		[Test()]
		public void TestSurrogate()
		{
		    var input = File.ReadAllText("UnitTests/surrogate-test.json");
		    var deserialized =JsonReader.Deserialize<Dictionary<string,string>>(input);
            Assert.AreEqual("🔥5             🚒2",deserialized["title"]);
		}
    }
}
