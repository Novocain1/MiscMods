using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Vintagestory.API.Config;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory;
using System.Diagnostics;

namespace ReshadeEnablerMod
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ReshadeEnabler enabler = new ReshadeEnabler();
            enabler.Patch();
            enabler.StartVS(args);
        }
    }

    public class ReshadeEnabler
    {
        public AssemblyDefinition OpenTKRef { get; set; }
        string LibFolder { get; set; }
        string BackupFolder { get; set; }
        string OpenTKPath { get; set; }
        string OpenTKBackupPath { get; set; }
        string GLPath { get; set; }

        public ReshadeEnabler()
        {
            LibFolder = Path.Combine(GamePaths.Binaries, "lib");
            OpenTKPath = Path.Combine(LibFolder, "OpenTK.dll");
            BackupFolder = Path.Combine(LibFolder, "backup");
            OpenTKBackupPath = Path.Combine(BackupFolder, "OpenTK.dll");
            GLPath = Path.Combine(GamePaths.Binaries, "opengl32.dll");
            Directory.CreateDirectory(BackupFolder);

            OpenTKRef = AssemblyDefinition.ReadAssembly(OpenTKPath, new ReaderParameters() { ReadWrite = true });
        }

        public void Patch()
        {
            if (!File.Exists(GLPath)) throw new FileNotFoundException();

            var mod = OpenTKRef.MainModule;
            var glRefs = mod.Types.Where(t => t.IsClass).SelectMany(t => t.Fields.Where(a => a.HasConstant)).Where(t => t.Constant.Equals((object)"OPENGL32.DLL"));
            if (glRefs.Count() > 0)
            {
                OpenTKRef.Write(OpenTKBackupPath);
            }
            foreach (var val in glRefs)
            {
                val.Constant = GLPath;
            }
            OpenTKRef.Write();
        }

        public void StartVS(string[] args)
        {
            StringBuilder bdr = new StringBuilder();
            foreach (var val in args)
            {
                bdr.Append(val);
                bdr.Append(' ');
            }
            Process.Start("VintageStory.exe", bdr.ToString());
            Environment.Exit(0);
        }
    }

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
