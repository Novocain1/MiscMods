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
using Newtonsoft.Json;

namespace VSHUD
{
    interface IExportable : IDisposable
    {
        bool Enabled { get; set; }
        bool Disposeable { get; set; }
        string FilePath { get; set; }
        string FileName { get; set; }
        void Export();
    }

    abstract class Exportable : IExportable
    {
        public Exportable(string filePath, string fileName = null)
        {
            FilePath = filePath;
            FileName = fileName;
        }

        public abstract bool Enabled { get; set; }
        public abstract bool Disposeable { get; set; }
        public abstract string FilePath { get; set; }
        public abstract string FileName { get; set; }
        public abstract void Export();
        public abstract void Dispose();
    }

    class ExportableJsonObject : Exportable
    {
        public object thing;

        public ExportableJsonObject(object thing, string filePath) : base(filePath)
        {
            this.thing = thing;
        }

        public override bool Enabled { get; set; } = true;
        public override bool Disposeable { get; set; } = true;
        public override string FilePath { get; set; }
        public override string FileName { get; set; }

        public override void Dispose()
        {
        }

        public override void Export()
        {
            using (TextWriter tw = new StreamWriter(FilePath + ".json"))
            {
                tw.Write(JsonConvert.SerializeObject(thing, Formatting.Indented));
                tw.Close();
            }
        }
    }

    class ExportableChunkPart : ExportableMesh
    {
        public override bool Enabled { get => ConfigLoader.Config.CreateChunkObjs; set => Enabled = value; }

        public ExportableChunkPart(MeshData mesh, string filePath, string fileName) : base(mesh, filePath, fileName)
        {
        }
    }

    class ExportableMesh : Exportable
    {
        public MeshData Mesh;
        public override bool Enabled { get; set; } = true;
        public override bool Disposeable { get; set; } = true;
        public override string FilePath { get; set; }
        public override string FileName { get; set; }

        public ExportableMesh(MeshData mesh, string filePath, string fileName) : base(filePath, fileName)
        {
            Mesh = mesh.Clone();
        }

        public override void Export() => ExportAsObj();

        public virtual void ExportAsGltf()
        {
            throw new NotImplementedException();
        }

        public virtual void ExportAsObj()
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
                        tw.Write(string.Format("v {0} {1} {2}", Mesh.xyz[i * 3 + 0].ToString("F6"), Mesh.xyz[i * 3 + 1].ToString("F6"), Mesh.xyz[i * 3 + 2].ToString("F6")));
                    }

                    for (int i = 0; i < Mesh.FlagsCount; i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("vf {0}", Mesh.Flags[i].ToString()));
                    }

                    for (int i = 0; i < (Mesh.CustomBytes?.Count ?? 0); i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("cb {0}", Mesh.CustomBytes.Values[i].ToString()));
                    }

                    for (int i = 0; i < (Mesh.CustomFloats?.Count ?? 0); i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("cf {0}", Mesh.CustomFloats.Values[i].ToString("F6")));
                    }

                    for (int i = 0; i < (Mesh.CustomInts?.Count ?? 0); i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("ci {0}", Mesh.CustomInts.Values[i].ToString()));
                    }

                    for (int i = 0; i < (Mesh.CustomShorts?.Count ?? 0); i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("cs {0}", Mesh.CustomShorts.Values[i].ToString()));
                    }

                    for (int i = 0; i < uvs.Length / 2; i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("vt {0} {1}", uvs[i * 2 + 0].ToString("F6"), uvs[i * 2 + 1].ToString("F6")));
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

        public override void Dispose()
        {
            Mesh.Clear();
        }
    }

    class MassFileExportSystem : ClientSystem
    {
        public static ConcurrentStack<Exportable> toExport = new ConcurrentStack<Exportable>();

        public static void Clear() => Clear<Exportable>();

        public static void Clear<T>()
        {
            if (toExport.IsEmpty) return;

            Stack<int> indices = new Stack<int>();

            int i = 0;

            foreach (var val in toExport)
            {
                if (val is T) indices.Push(i);
                i++;
            }

            var ind = indices.ToArray();

            var items = new Exportable[toExport.Count];
            toExport.TryPopRange(items);

            for (i = 0; i < ind.Length; i++)
            {
                items[ind[i]].Dispose();
                items[ind[i]] = null;
            }

            foreach (var val in items)
            {
                if (val != null) toExport.Push(val);
            }
        }

        public MassFileExportSystem(ClientMain game) : base(game) {}

        public override string Name => "File Export";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => ProcessStackedExportables();

        public void ProcessStackedExportables()
        {
            for (int i = 0; i < toExport.Count; i++)
            {
                if (toExport.TryPop(out Exportable exportable))
                {
                    if (exportable.Enabled)
                    {
                        exportable.Export();
                        if (exportable.Disposeable) exportable.Dispose();
                    }
                    else
                    {
                        Exportable[] exportables = new Exportable[toExport.Count];
                        toExport.TryPopRange(exportables);
                        toExport.Push(exportable);
                        Array.Reverse(exportables);
                        toExport.PushRange(exportables);
                    }
                }
            }
        }

        public override void Dispose(ClientMain game)
        {
            base.Dispose(game);
            Clear();
        }
    }
}
