using System;
using System.Reflection;

namespace HarvestCraftLoader
{
    public static class HackerMan
    {
        public static BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static K GetField<T, K>(this T instance, string fieldName)
        {
            return (K)instance.GetField(fieldName);
        }

        public static object GetField<T>(this T instance, string fieldName)
        {
            return GetInstanceField(instance.GetType(), instance, fieldName);
        }

        public static object CallMethod<T>(this T instance, string methodName) => instance?.CallMethod(methodName, null);

        public static object CallMethod<T>(this T instance, string methodName, params object[] parameters)
        {
            MethodInfo info = instance?.GetType()?.GetMethod(methodName, bindFlags);
            return info?.Invoke(instance, parameters);
        }

        public static object GetInstanceField(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
    }
}
