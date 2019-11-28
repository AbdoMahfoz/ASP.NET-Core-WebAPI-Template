using System;
using System.Collections.Generic;
using System.Reflection;

namespace Models.Helpers
{
    /// <summary>
    /// This Attribute is used to identify which entities to expose in the API
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExposeToApi : Attribute
    {

    }
    public static class IncludedEntities
    {
        public static IReadOnlyList<TypeInfo> Types;

        static IncludedEntities()
        {
            var assembly = typeof(IncludedEntities).GetTypeInfo().Assembly;
            var typeList = new List<TypeInfo>();

            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(ExposeToApi), true).Length > 0)
                {
                    typeList.Add(type.GetTypeInfo());
                }
            }
            Types = typeList;
        }
    }
}
