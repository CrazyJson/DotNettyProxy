using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CDotNetty.Common
{
    /// <summary>
    /// 
    /// </summary>
    public static class ConfigHelper
    {
        private static Dictionary<string, object> configuration { get; }

        static ConfigHelper()
        {
            using (StreamReader sr = new StreamReader(Path.Combine(ProcessDirectory, "appsettings.json"), true))
            {
                string txt = sr.ReadToEnd();
                configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(txt);
            }
        }

        private static string ProcessDirectory
        {
            get
            {
#if NETSTANDARD2_0
                return AppContext.BaseDirectory;
#else
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }

        private static Type GetRealPropertyType(Type propertyType)
        {
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return propertyType.GetGenericArguments()[0];
            }
            else
            {
                return propertyType;
            }
        }

        private static object ConvertScalar(object obj, Type propertyType)
        {
            if (obj == null || DBNull.Value.Equals(obj))
            {
                return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
            }
            if (obj.GetType() == propertyType)
                return obj;

            var realType = GetRealPropertyType(propertyType);
            return Convert.ChangeType(obj, realType);
        }

        public static T GetValue<T>(string key)
        {
            if (configuration.TryGetValue(key, out object obj))
            {
                return (T)ConvertScalar(obj, typeof(T));
            }
            else
            {
                return default(T);
            }
        }
    }
}