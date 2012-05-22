using System;
using UnityEngine;
using JsonFx.Json;
using System.Collections.Generic;

/** \file Some converters you can use with basic Unity built-in types. Supports Bounds, LayerMask and Vector2,3,4. */

	public class BoundsConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return type == typeof(Bounds);
		}
		
		public override object ReadJson ( Type objectType, Dictionary<string,object> values) {
			Bounds b = new Bounds();
			b.center = new Vector3(	CastFloat(values["cx"]),CastFloat(values["cy"]),CastFloat(values["cz"]));
			b.extents = new Vector3(CastFloat(values["ex"]),CastFloat(values["ey"]),CastFloat(values["ez"]));
			return b;
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			Bounds b = (Bounds)value;
			return new Dictionary<string, object>() {
				{"cx",b.center.x},
				{"cy",b.center.y},
				{"cz",b.center.z},
				{"ex",b.extents.x},
				{"ey",b.extents.y},
				{"ez",b.extents.z}
			};
		}
	}
	
	public class LayerMaskConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return type == typeof(LayerMask);
		}
		
		public override object ReadJson (Type type, Dictionary<string,object> values) {
			return (LayerMask)(int)values["value"];
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			return new Dictionary<string, object>() {{"value",((LayerMask)value).value}};
		}
	}
	
	public class VectorConverter : JsonConverter
	{
		public override bool CanConvert (Type type) {
			return type == typeof(Vector2) || type==typeof(Vector3)||type==typeof(Vector4);
		}
		
		public override object ReadJson (Type type, Dictionary<string,object> values) {
			if (type == typeof(Vector2)) {
				return new Vector2(CastFloat(values["x"]),CastFloat(values["y"]));
			} else if (type == typeof(Vector3)) {
				return new Vector3(CastFloat(values["x"]),CastFloat(values["y"]),CastFloat(values["z"]));
			} else if (type == typeof(Vector4)) {
				return new Vector4(CastFloat(values["x"]),CastFloat(values["y"]),CastFloat(values["z"]),CastFloat(values["w"]));
			} else {
				throw new System.NotImplementedException ("Can only read Vector2,3,4. Not objects of type "+type);
			}
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			if (type == typeof(Vector2)) {
				Vector2 v = (Vector2)value;
				return new Dictionary<string, object>() {
					{"x",v.x},
					{"y",v.y}
				};
			} else if (type == typeof(Vector3)) {
				Vector3 v = (Vector3)value;
				return new Dictionary<string, object>() {
					{"x",v.x},
					{"y",v.y},
					{"z",v.z}
				};
			} else if (type
			           == typeof(Vector4)) {
				Vector4 v = (Vector4)value;
				return new Dictionary<string, object>() {
					{"x",v.x},
					{"y",v.y},
					{"z",v.z},
					{"w",v.w}
				};
			}
			throw new System.NotImplementedException ("Can only write Vector2,3,4. Not objects of type "+type);
		}
	}
