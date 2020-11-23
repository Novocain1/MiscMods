using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Vintagestory.API.Config;
using System.IO;
using System.Diagnostics;

namespace CustomGLSetter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CustomGL enabler = new CustomGL();
            enabler.Patch();
            enabler.StartVS(args);
        }
    }

    public class CustomGL
    {
        public AssemblyDefinition OpenTKRef { get; set; }
        string LibFolder { get; set; }
        string BackupFolder { get; set; }
        string OpenTKPath { get; set; }
        string OpenTKBackupPath { get; set; }
        string GLPath { get; set; }

        public CustomGL()
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
}
