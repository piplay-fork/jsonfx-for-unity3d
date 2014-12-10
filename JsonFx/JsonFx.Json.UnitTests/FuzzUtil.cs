using System;
using System.Collections;

public static class FuzzUtil
{
	static Random random;
	
	static FuzzUtil() {
		random = new Random((int)DateTime.Now.ToFileTime());
	}
	
	public static T FuzzGen<T>(int depthRemaining = 4) {
		return (T)FuzzGen(typeof(T), depthRemaining);
	}
	
	public static object FuzzGen(Type type, int depthRemaining = 4) {
		object generated;
		if (type.IsEnum) {
			var values = Enum.GetValues(type);
			generated = values.GetValue(random.Next(values.Length));
		}
		else {
			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean:	generated = random.Next(2) == 0; break;
			case TypeCode.Byte:		generated = (byte)random.Next(256); break;
			case TypeCode.SByte:	generated = (sbyte)random.Next(-128, 128); break;
			case TypeCode.Int16:	generated = (short)SRandMag(16); break;
			case TypeCode.UInt16:	generated = (ushort)RandMag(16); break;
			case TypeCode.Int32:	generated = (int)SRandMag(32); break;
			case TypeCode.UInt32:	generated = (uint)RandMag(32); break;
			case TypeCode.Int64:	generated = (long)SRandMag(64); break;
			case TypeCode.UInt64:	generated = (ulong)RandMag(64); break;
			case TypeCode.Double:	generated = random.NextDouble(); break;
			case TypeCode.Single:	generated = (float)random.NextDouble(); break;
			case TypeCode.Char:		generated = RandChar(); break;
			case TypeCode.String:
				var chs = new char[(int)RandMag(8)];
				for (var i = 0; i< chs.Length; i++) chs[i]=RandChar();
				generated = new string(chs, 0, chs.Length).Trim();
				break;
			case TypeCode.Object:
				if (depthRemaining == 0) generated = null;
				else if (type.IsArray) {
					int length = (int) RandMag(8);
					var elementType = type.GetElementType();
					var array = Array.CreateInstance(elementType, (int)length);
					for (var i = 0; i < array.Length; i++) {
						array.SetValue(FuzzGen(elementType, depthRemaining - 1), i);
					}
					generated = array;
				}
				else if (typeof(IList).IsAssignableFrom(type)) {
					var list = Activator.CreateInstance(type, (int)RandMag(8)) as IList;
					var elementType = list.GetType().GetGenericArguments()[0];
					for (var i = 0; i < list.Count; i++) {
						list[i] = FuzzGen(elementType, depthRemaining - 1);
					}
					generated = list;
				}
				else {
					var obj = Activator.CreateInstance(type);
					foreach (var prop in type.GetProperties()) {
						if (prop.GetSetMethod() != null) {
							if (prop.GetSetMethod().IsPublic) {
								prop.SetValue(obj, FuzzGen(prop.GetGetMethod().ReturnType, depthRemaining - 1), null);
							}
						}
						else if (typeof(IList).IsAssignableFrom(prop.GetGetMethod().ReturnType)) {
							var list = prop.GetValue(obj, null) as IList;
							var elementType = list.GetType().GetGenericArguments()[0];
							for (int i = 0, l = (int)RandMag(8); i < l; i++) {
								list.Add(FuzzGen(elementType, depthRemaining - 1));
							}
						}
					}
					foreach (var field in type.GetFields()) {
						if (field.IsPublic && !field.IsStatic && !field.IsInitOnly) {
							field.SetValue(obj, FuzzGen(field.FieldType, depthRemaining - 1));
						}
					}
					generated = obj;
				}
				break;
			default:
				throw new NotImplementedException("Fuzz gen not supported for " + type);
			}
		}
//		Console.WriteLine(new String(' ',5 - depthRemaining) + "Generated " + generated);
		return generated;
	}

	static long RandMag(int maxPot) {
		return (long)Math.Pow(2, random.NextDouble() * maxPot) - 1;
	}
	
	static long SRandMag(int maxPot) {
		var val = RandMag(maxPot);
		if (random.Next(2) == 0) val = -val;
		return val;
	}
	
	static char RandChar() {
		return (char)random.Next(32, 128);
	}
	
}

