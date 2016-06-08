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
		public void TestSurrogates()
		{
			var tests = new Dictionary<string,string>(){
				
				{"Test UTF-8 heart eyes 1 😍 ",           @"{""inner"":""Test UTF-8 heart eyes 1 \uD83D\uDE0D ""}"},
				{"Test UTF-8 heart eyes 2 \xd83d\xde0d ", @"{""inner"":""Test UTF-8 heart eyes 2 \uD83D\uDE0D ""}"},
				{"Test UTF-8 heart eyes 3 \xd83d\xde0d", @"{""inner"":""Test UTF-8 heart eyes 3 \uD83D\uDE0D""}"},
				{"\xd83d\xde0d Test UTF-8 heart eyes 4", @"{""inner"":""\uD83D\uDE0D Test UTF-8 heart eyes 4""}"},
				{"\xd83d\xde0d Test UTF-8 heart eyes 5\xd83d\xde0d", @"{""inner"":""\uD83D\uDE0D Test UTF-8 heart eyes 5\uD83D\uDE0D""}"},
				{"Test UTF-8 g with caron Ǧ ", @"{""inner"":""Test UTF-8 g with caron \u01E6 ""}"},
				{"Test UTF-8 345f 㑟 ", @"{""inner"":""Test UTF-8 345f \u345F ""}"},
				{"Test UTF-8 2a7ed � ", @"{""inner"":""Test UTF-8 2a7ed \uFFFD ""}"},
				{"\xd83d\xdd25             \xd83d\xde92",@"{""inner"":""\uD83D\uDD25             \uD83D\uDE92""}"}
            };
            foreach (var inner in tests)
            {
            	var dict = new Dictionary<string,string>(){{"inner",inner.Key}};
				var json = JsonWriter.Serialize(dict);
				Assert.AreEqual(inner.Value, json);
				Console.WriteLine("Parsing back json: " + json);
				Console.WriteLine("                  |0    .    1    .    2    .    3    .    4    .    5    .    6    .    7    .    8    .    9    .    0" );
				dict = JsonReader.Deserialize<Dictionary<string,string>>(json);
				Assert.AreEqual(inner.Key, dict["inner"]);
            }
		}
    }
}
