using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RandomTests
{
    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname) => (T)AccessTools.Field(instance.GetType(), fieldname).GetValue(instance);
        public static T GetProperty<T>(this object instance, string fieldname) => (T)AccessTools.Property(instance.GetType(), fieldname).GetValue(instance);
        public static T CallMethod<T>(this object instance, string method, params object[] args) => (T)AccessTools.Method(instance.GetType(), method).Invoke(instance, args);
        public static void CallMethod(this object instance, string method, params object[] args) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, args);
        public static void CallMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, null);
        public static object CreateInstance(this Type type) => AccessTools.CreateInstance(type);
        public static T[] GetFields<T>(this object instance)
        {
            List<T> fields = new List<T>();
            var declaredFields = AccessTools.GetDeclaredFields(instance.GetType())?.Where((t) => t.FieldType == typeof(T));
            foreach (var val in declaredFields)
            {
                fields.Add(instance.GetField<T>(val.Name));
            }
            return fields.ToArray();
        }

        public static void SetField(this object instance, string fieldname, object setVal) => AccessTools.Field(instance.GetType(), fieldname).SetValue(instance, setVal);
        public static MethodInfo GetMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method);
    }
}
