using System.Reflection;
using System.Collections.Generic;
using System;

namespace Services
{
    public static class Helpers
    {
        public static bool HasNullOrEmptyStrings<T>(T obj)
        {
            if (obj == null) return true;
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (!propertyInfo.CanRead) continue;
                if (propertyInfo.PropertyType == typeof(string))
                {
                    string val = (string)propertyInfo.GetValue(obj);
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
                if (val != null)
                {
                    propertyInfo.SetValue(oldobj, val);
                }
            }
        }
        public static T MapTo<T>(object obj) where T : new()
        {
            T res = new T();
            Dictionary<string, PropertyInfo> OutProps = new Dictionary<string, PropertyInfo>();
            foreach(var property in typeof(T).GetProperties())
            {
                if(property.CanRead)
                {
                    OutProps.Add(property.Name, property);
                }
            }
            foreach(var InProp in obj.GetType().GetProperties())
            {
                PropertyInfo OutProp;
                if(InProp.CanRead && OutProps.TryGetValue(InProp.Name, out OutProp))
                {
                    OutProp.SetValue(res, InProp.GetValue(obj));
                }
            }
            return res;
        }
    }
}