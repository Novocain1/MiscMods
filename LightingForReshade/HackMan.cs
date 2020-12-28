using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LightingForReshade
{
    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname) => (T)AccessTools.Field(instance.GetType(), fieldname).GetValue(instance);
        public static T GetProperty<T>(this object instance, string fieldname) => (T)AccessTools.Property(instance.GetType(), fieldname).GetValue(instance);
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

        public static void CallMethod(this object instance, string method) => instance?.CallMethod(method, null);
        public static void CallMethod(this object instance, string method, params object[] args) => instance?.CallMethod<object>(method, args);
        public static T CallMethod<T>(this object instance, string method) => (T)instance.CallMethod<object>(method, null);

        public static T CallMethod<T>(this object instance, string method, params object[] args)
        {
            Type[] parameters = null;
            if (args != null)
            {
                parameters = args.Length > 0 ? new Type[args.Length] : null;
                for (int i = 0; i < args.Length; i++)
                {
                    parameters[i] = args[i].GetType();
                }
            }
            return (T)instance?.GetMethod(method, parameters).Invoke(instance, args);
        }

        public static MethodInfo GetMethod(this object instance, string method, params Type[] parameters) => instance.GetMethod(method, parameters, null);
        public static MethodInfo GetMethod(this object instance, string method, Type[] parameters = null, Type[] generics = null) => AccessTools.Method(instance.GetType(), method, parameters, generics);
    }

}
