using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using HarmonyLib;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using System.Collections.Concurrent;

namespace VSHUD
{
    interface IExportable
    {
        bool Enabled { get; set; }
        string FilePath { get; set; }
        string FileName { get; set; }
        void Export();
    }

    abstract class Exportable : IExportable
    {
        public abstract bool Enabled { get; set; }
        public abstract string FilePath { get; set; }
        public abstract string FileName { get; set; }
        public abstract void Export();
    }

    class PreparedMesh : Exportable
    {
        public MeshData Mesh;
        public override bool Enabled { get => ConfigLoader.Config.CreateChunkObjs; set => Enabled = value; }
        public override string FilePath { get; set; }
        public override string FileName { get; set; }

        public PreparedMesh(MeshData mesh, string filePath, string fileName)
        {
            Mesh = mesh.Clone();
            FilePath = filePath;
            FileName = fileName;
        }

        public override void Export() => ExportAsObj();

        public void ExportAsObj()
        {
            try
            {
                Mesh.Translate(-0.5f, -0.5f, -0.5f);

                float[] uvs = Mesh.Uv;

                using (TextWriter tw = new StreamWriter(FilePath))
                {
                    tw.Write("o " + FileName);

                    for (int i = 0; i < Mesh.Uv.Length / 4; i++)
                    {
                        if (i + 4 > Mesh.UvCount) continue;
                        float[] transform = new float[] { Mesh.Uv[i * 4 + 0], Mesh.Uv[i * 4 + 1], Mesh.Uv[i * 4 + 2], Mesh.Uv[i * 4 + 3] };

                        Mat22.Scale(transform, transform, new float[] { 1.0f, -1.0f });
                        Mat22X.Translate(transform, transform, new float[] { 0.0f, 1.0f });
                        Mesh.Uv[i * 4 + 0] = transform[0];
                        Mesh.Uv[i * 4 + 1] = transform[1];
                        Mesh.Uv[i * 4 + 2] = transform[2];
                        Mesh.Uv[i * 4 + 3] = transform[3];
                    }

                    for (int i = 0; i < Mesh.VerticesCount; i++)
                    {
                        tw.WriteLine();
                        tw.Write("v");
                        tw.Write(" " + Mesh.xyz[i * 3 + 0].ToString("F6"));
                        tw.Write(" " + Mesh.xyz[i * 3 + 1].ToString("F6"));
                        tw.Write(" " + Mesh.xyz[i * 3 + 2].ToString("F6"));
                    }

                    for (int i = 0; i < uvs.Length / 2; i++)
                    {
                        tw.WriteLine();
                        tw.Write("vt " + uvs[i * 2 + 0].ToString("F6"));
                        tw.Write(" " + uvs[i * 2 + 1].ToString("F6"));
                    }

                    tw.WriteLine();
                    tw.Write("usemtl TextureAtlas");

                    for (int i = 0; i < Mesh.Indices.Length / 3; i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("f {0}/{0} {1}/{1} {2}/{2}", Mesh.Indices[i * 3 + 0] + 1, Mesh.Indices[i * 3 + 1] + 1, Mesh.Indices[i * 3 + 2] + 1));
                    }
                    tw.Close();
                }
            }
            catch (Exception)
            {
            }

        }
    }

    class MassFileExportSystem : ClientSystem
    {
        public static ConcurrentStack<Exportable> toExport = new ConcurrentStack<Exportable>();

        public MassFileExportSystem(ClientMain game) : base(game) {}

        public override string Name => "ObjExport";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => ProcessStackedExportables();

        public void ProcessStackedExportables()
        {
            for (int i = 0; i < toExport.Count; i++)
            {
                bool success = toExport.TryPop(out Exportable exportable) && exportable.Enabled;
                if (success) exportable.Export();
            }
        }
    }
}
