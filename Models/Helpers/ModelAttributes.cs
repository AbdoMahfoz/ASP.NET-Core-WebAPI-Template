using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.Helpers;

/// <summary>
///     This Attribute is used to identify which entities to expose in the API
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExposeToApi : Attribute
{
    public Type DtoIn { get; }
    public Type DtoOut { get; }

    public ExposeToApi(Type dtoIn, Type dtoOut)
    {
        DtoIn = dtoIn;
        DtoOut = dtoOut;
    }
}

public static class IncludedEntities
{
    //Types are loaded with Lazy<T> to improve performance and resource utilization
    private static readonly Lazy<IReadOnlyList<(Type entityType, Type dtoIn, Type dtoOut)>> _types = new Lazy<IReadOnlyList<(Type, Type, Type)>>(LoadTypes);

    public static IReadOnlyList<(Type, Type, Type)> Types => _types.Value;

    private static IReadOnlyList<(Type, Type, Type)> LoadTypes()
    {
        var assembly = typeof(IncludedEntities).GetTypeInfo().Assembly;
        var typeList = new List<(Type, Type, Type)>();
        //Simplified the attribute fetching using GetCustomAttribute<>() instead of GetCustomAttributes() and checking the length
        foreach (var type in assembly.GetLoadableTypes())
        {
            var attrib = type.GetCustomAttribute<ExposeToApi>();
            if (attrib != null)
            {
                typeList.Add((type, attrib.DtoIn, attrib.DtoOut));
            }
        }

        return typeList;
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
