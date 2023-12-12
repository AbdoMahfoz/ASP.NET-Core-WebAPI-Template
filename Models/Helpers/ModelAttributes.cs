using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.Helpers;

/// <summary>
///     This Attribute is used to identify which entities to expose in the API
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExposeToApi(Type dtoIn, Type dtoOut) : Attribute
{
    public Type DtoIn = dtoIn, DtoOut = dtoOut;
}

public static class IncludedEntities
{
    public static IReadOnlyList<(Type, Type, Type)> Types;

    static IncludedEntities()
    {
        var assembly = typeof(IncludedEntities).GetTypeInfo().Assembly;
        var typeList = new List<(Type, Type, Type)>();

        foreach (var type in assembly.GetLoadableTypes())
        {
            var attribs = type.GetCustomAttributes(typeof(ExposeToApi), true);
            if (attribs.Length > 0)
            {
                var o = (ExposeToApi)attribs[0];
                typeList.Add((type, o.DtoIn, o.DtoOut));
            }
        }

        Types = typeList;
    }

    public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }
}