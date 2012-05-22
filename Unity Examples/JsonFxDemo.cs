using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DB = UnityEngine.Debug;
using JsonFx.Json;
using System;

public class Tweet {
	public System.DateTime created_at;
	public string from_user;
	public long from_user_id;
	public string from_user_id_str;
	public string from_user_name;
	public string geo;
	public long id;
	public string id_str;
	public string iso_language_code;
	public Dictionary<string, string> metadata;
	public string profile_image_url;
	public string source;
	public string text;
	public long to_user_id;
	public string to_user_id_str;
}

public class TwitterSearchResults {
	public float completed_in;
	public long max_id;
	public string max_id_str;
	public string next_page;
	public int page;
	public string query;
	public string refresh_url;
	public Tweet[] results;
	public int results_per_page;
	public long since_id;
	public string since_id_str;
}

[JsonOptIn]
public class TestClass {
	[JsonMember]
	public float x = 5;
	public int y = 3;
	[JsonMember]
	public Vector3 vec = new Vector3(1,2,3);
	public string s = "03525asfg##";
	public double areallylongnamethisis = 3.14159296;
	public Vector3[] vecs = new Vector3[10];
	public byte[] bytes = new byte[10];
}


public class JsonFxDemo : MonoBehaviour {

	public string query = "#Unity3d";

	// Use this for initialization
	void Start () {
		StartCoroutine(PerformSearch(query));
	}

	void PrintResults(string rawJson) {
		// Raw output:
		/*DB.Log(DC.Log("******** raw string from Twitter ********"));
		DB.Log(DC.Log(rawJson));
		
		
		// Turn the JSON into C# objects
		var search = JsonReader.Deserialize<TwitterSearchResults>(rawJson);
		
		
		// iterate through the array of results;
		DB.Log(DC.Log("******** search results ********"));

	
		foreach (var tweet in search.results) {
			DB.Log(DC.Log(tweet.from_user_name + " : " + tweet.text));
		}

		DB.Log(DC.Log("******** serialize an entity ********"));

		JsonWriterSettings settings = new JsonWriterSettings();
		settings.PrettyPrint = true;
		
		System.Text.StringBuilder output = new System.Text.StringBuilder();
		
		JsonWriter writer = new JsonWriter (output,settings);
		writer.Write (search.results[0]);
		
		// this turns a C# object into a JSON string.
		string json = output.ToString();//JsonWriter.Serialize();

		DB.Log(DC.Log(json));*/
		
		for (int i=0;i<10;i++) {
		System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
		watch.Start();
		System.Text.StringBuilder output = new System.Text.StringBuilder();
		
		Debug.Log ("+++ Serializing +++");
		JsonWriterSettings settings = new JsonWriterSettings();
		settings.PrettyPrint = false;
		settings.AddTypeConverter (new VectorConverter());
		
		TestClass test = new TestClass();
		test.vec.y = 128.513589999F;
		JsonWriter writer = new JsonWriter (output,settings);
			
		Debug.Log ("+++ Writing +++");
		writer.Write (test);
		
		if (i==0)
			Debug.Log (output.ToString());
		
		Debug.Log ("+++ Deserializing - Init +++");
		JsonReaderSettings settings2 = new JsonReaderSettings();
		settings2.AddTypeConverter (new VectorConverter());
		JsonReader reader = new JsonReader(output.ToString(),settings2);
			
		Debug.Log ("+++ Deserializing +++");
		TestClass deserialized = reader.Deserialize<TestClass>();
		
		watch.Stop();
		Debug.Log ((watch.ElapsedTicks*0.0001).ToString("0.00"));
		Debug.Log (deserialized.vec.y.ToString("r"));
		}
	}
	
	IEnumerator PerformSearch(string query) {
		/*query = WWW.EscapeURL(query);

		using (var www = new WWW(string.Format("http://search.twitter.com/search.json?q={0}", query))) {
			while (!www.isDone) {
				yield return null;
			}

			PrintResults(www.text);
		}*/
		
		PrintResults (null);
		yield return 0;
	}
}

public class VectorConverter : JsonConverter {
	public override bool CanConvert (Type t) {
		return t == typeof(Vector3);
	}
	
	public override Dictionary<string,object> WriteJson (Type type, object value) {
		Vector3 v = (Vector3)value;
		Dictionary<string,object> dict = new Dictionary<string, object>();
		dict.Add ("x",v.x);
		dict.Add ("y",v.y);
		dict.Add ("z",v.z);
		return dict;
	}
	
	public override object ReadJson (Type type, Dictionary<string,object> value) {
		//Debug.Log ("First key type "+value["x"].GetType());
		Vector3 v = new Vector3(CastFloat(value["x"]),CastFloat(value["y"]),CastFloat(value["z"]));
		return v;
	}
}