using System;

namespace JsonFx.Json
{
    /// <summary>
    /// Attribute to specify that compiler generated fields should be serialized 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SerializeCompilerGenerated : Attribute
    {
        
    }
}