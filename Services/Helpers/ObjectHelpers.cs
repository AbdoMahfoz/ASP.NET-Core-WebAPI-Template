using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Models.DataModels;
using Services.DTOs;

namespace Services.Helpers
{
    public static class ObjectHelpers
    {
        public static bool HasNullOrEmptyStrings<T>(T obj)
        {
            if (obj == null) return true;
            return (from propertyInfo in typeof(T).GetProperties()
                where propertyInfo.CanRead
                where propertyInfo.PropertyType == typeof(string)
                select (string)propertyInfo.GetValue(obj)
                into val
                select string.IsNullOrWhiteSpace(val)).FirstOrDefault();
        }

        public static T MapTo<T>(object obj) where T : new()
        {
            return (T)MapTo(typeof(T), obj);
        }

        public static object MapTo(Type T, object obj)
        {
            if (obj == null) return null;
            var res = Activator.CreateInstance(T);
            var outProps = new Dictionary<string, PropertyInfo>();
            foreach (var property in T.GetProperties())
            {
                var ignore = (from attrib in property.CustomAttributes
                    where attrib.AttributeType == typeof(IgnoreInHelpers)
                    select attrib).Any();
                if (!ignore && property.CanWrite) outProps.Add(property.Name, property);
            }

            foreach (var inProp in obj.GetType().GetProperties())
            {
                var ignore = (from attrib in inProp.CustomAttributes
                    where attrib.AttributeType == typeof(IgnoreInHelpers)
                    select attrib).Any();
                if (ignore || !inProp.CanRead || !outProps.TryGetValue(inProp.Name, out var outProp)) continue;
                if (outProp.PropertyType == inProp.PropertyType)
                {
                    outProp.SetValue(res, inProp.GetValue(obj));
                    if (outProp.PropertyType != typeof(DateTime)) continue;
                    var dateTime = (DateTime)outProp.GetValue(res)!;
                    outProp.SetValue(res, dateTime.ToUniversalTime());
                }
                else if (outProp.PropertyType == typeof(byte[]) && inProp.PropertyType == typeof(string))
                {
                    try
                    {
                        if (inProp.GetValue(obj) is string val)
                        {
                            if (val.Contains(','))
                            {
                                val = val[(val.IndexOf(',') + 1)..];
                            }

                            outProp.SetValue(res, Convert.FromBase64String(val));
                        }
                    }
                    catch (FormatException)
                    {
                    }
                }
                else if (outProp.PropertyType == typeof(string) && inProp.PropertyType == typeof(byte[]))
                {
                    try
                    {
                        if (inProp.GetValue(obj) is byte[] val)
                        {
                            outProp.SetValue(res, Convert.ToBase64String(val));
                        }
                    }
                    catch (FormatException)
                    {
                    }
                }
                else if (outProp.PropertyType.IsAssignableTo(typeof(IEnumerable)) &&
                         inProp.PropertyType.IsAssignableTo(typeof(IEnumerable)))
                {
                    var values = (IEnumerable<object>)inProp.GetValue(obj);
                    if (values == null) continue;
                    var innerType = outProp.PropertyType.GetGenericArguments()[0];
                    var parameterExpression = Expression.Parameter(inProp.PropertyType.GetGenericArguments()[0]);
                    var mapToMethod = typeof(ObjectHelpers).GetMethods()
                        .Single(u => u.Name == "MapTo" && u.IsGenericMethod).MakeGenericMethod(innerType);
                    var callExpression = Expression.Call(null, mapToMethod, parameterExpression);
                    var lambda = Expression.Lambda(callExpression, parameterExpression);
                    var selectMethod = typeof(Enumerable).GetMethods().First(u => u.Name == "Select")
                        .MakeGenericMethod(inProp.PropertyType.GetGenericArguments()[0],
                            outProp.PropertyType.GetGenericArguments()[0]);
                    var toListMethod = typeof(Enumerable).GetMethods().First(u => u.Name == "ToList")
                        .MakeGenericMethod(innerType);
                    var selectExpression = Expression.Call(null, selectMethod, Expression.Constant(values), lambda);
                    selectExpression = Expression.Call(null, toListMethod, selectExpression);
                    var setValueMethod = typeof(PropertyInfo).GetMethods()
                        .Single(u => u.Name == "SetValue" && u.GetParameters().Length == 2);
                    var outPropParameter = Expression.Parameter(typeof(PropertyInfo));
                    var finalExpression = Expression.Call(outPropParameter, setValueMethod,
                        Expression.Constant(res), selectExpression);
                    Expression.Lambda<Action<PropertyInfo>>(finalExpression, outPropParameter).Compile()(outProp);
                }
                else if (outProp.PropertyType.IsClass && inProp.PropertyType.IsClass)
                {
                    try
                    {
                        outProp.SetValue(res, MapTo(outProp.PropertyType, inProp.GetValue(obj)));
                    }
                    catch (Exception)
                    {
                        // It is possible that some exception may occur while attempting to recursively set properties
                        // In that case, we can just ignore the property without halting the application
                    }
                }
            }

            return res;
        }

        public static IEnumerable<EnumResult> GetEnumOptions(string EnumName)
        {
            var res = new List<EnumResult>();
            var t = (from type in Assembly.GetAssembly(typeof(BaseModel))?.GetTypes()
                where type.IsEnum && type.Name == EnumName
                select type).SingleOrDefault();
            if (t == null) return null;
            foreach (var val in t.GetEnumNames())
                res.Add(new EnumResult
                {
                    Id = res.Count,
                    Name = val
                });
            return res;
        }
    }
}