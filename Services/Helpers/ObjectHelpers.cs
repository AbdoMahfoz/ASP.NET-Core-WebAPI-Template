using System;
using System.Collections.Generic;
using System.Linq;
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
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (!propertyInfo.CanRead) continue;
                if (propertyInfo.PropertyType == typeof(string))
                {
                    string val = (string) propertyInfo.GetValue(obj);
                    return string.IsNullOrWhiteSpace(val);
                }
            }

            return false;
        }

        public static void UpdateObjects<T>(T oldobj, T newobj)
        {
            if (oldobj == null || newobj == null) throw new ArgumentNullException();
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (!propertyInfo.CanRead) continue;
                object val = propertyInfo.GetValue(newobj);
                if (val != null) propertyInfo.SetValue(oldobj, val);
            }
        }

        public static T MapTo<T>(object obj) where T : new()
        {
            return (T) MapTo(typeof(T), obj);
        }

        public static object MapTo(Type T, object obj)
        {
            if (obj == null) return null;
            object res = Activator.CreateInstance(T);
            Dictionary<string, PropertyInfo> OutProps = new Dictionary<string, PropertyInfo>();
            foreach (var property in T.GetProperties())
            {
                bool Ignore = (from attrib in property.CustomAttributes
                    where attrib.AttributeType == typeof(IgnoreInHelpers)
                    select attrib).Any();
                if (!Ignore && property.CanWrite) OutProps.Add(property.Name, property);
            }

            foreach (var InProp in obj.GetType().GetProperties())
            {
                bool Ignore = (from attrib in InProp.CustomAttributes
                    where attrib.AttributeType == typeof(IgnoreInHelpers)
                    select attrib).Any();
                if (!Ignore && InProp.CanRead && OutProps.TryGetValue(InProp.Name, out PropertyInfo OutProp))
                    if (OutProp.PropertyType == InProp.PropertyType)
                        OutProp.SetValue(res, InProp.GetValue(obj));
            }

            return res;
        }

        public static IEnumerable<EnumResult> GetEnumOptions(string EnumName)
        {
            List<EnumResult> res = new List<EnumResult>();
            Type t = (from type in Assembly.GetAssembly(typeof(BaseModel)).GetTypes()
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