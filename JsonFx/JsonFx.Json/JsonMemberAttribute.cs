using System;
namespace JsonFx.Json
{
	/** Explicitly declare this member to be serialized.
	 * \see JsonOptInAttribute
	 */
	public class JsonMemberAttribute : Attribute
	{
		public JsonMemberAttribute ()
		{
		}
	}
}

