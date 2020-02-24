using System;
using System.Collections.Generic;
using System.Reflection;

namespace Models.Helpers
{
    /// <summary>
    ///     This Attribute is used to identify which entities to expose in the API
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExposeToApi : Attribute
    {
        public Type DTOIn, DTOOut;

        public ExposeToApi(Type DTOIn, Type DTOOut)
        {
            this.DTOIn = DTOIn;
            this.DTOOut = DTOOut;
        }
    }

    public static class IncludedEntities
    {
        public static IReadOnlyList<(Type, Type, Type)> Types;

        static IncludedEntities()
        {
            var assembly = typeof(IncludedEntities).GetTypeInfo().Assembly;
            var typeList = new List<(Type, Type, Type)>();

            foreach (Type type in assembly.GetTypes())
            {
                var attribs = type.GetCustomAttributes(typeof(ExposeToApi), true);
                if (attribs.Length > 0)
                {
                    ExposeToApi o = (ExposeToApi) attribs[0];
                    typeList.Add((type, o.DTOIn, o.DTOOut));
                }
            }

            Types = typeList;
        }
    }
}