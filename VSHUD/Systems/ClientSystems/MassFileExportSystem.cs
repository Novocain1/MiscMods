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
    public interface IExportable : IDisposable
    {
        bool Enabled { get; set; }
        bool Disposeable { get; set; }
        string FilePath { get; set; }
        string FileName { get; set; }
        void Export();
    }

    public abstract class Exportable : IExportable
    {
        public Exportable(string filePath, string fileName = null)
        {
            FilePath = filePath;
            FileName = fileName;
        }

        public long[] ID = new long[] { 0, 0 };
        public abstract bool Enabled { get; set; }
        public abstract bool Disposeable { get; set; }
        public abstract string FilePath { get; set; }
        public abstract string FileName { get; set; }
        public abstract void Export();
        public abstract void Dispose();
    }

    public class ExportableJsonObject : Exportable
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

    public class ExportableChunkPart : ExportableMesh
    {
        public override bool Enabled { get => ConfigLoader.Config.CreateChunkObjs; set => Enabled = value; }
        public bool Is(ExportableChunkPart part)
        {
            return ID[0] == part.ID[0] && ID[1] == part.ID[1];
        }

        public ExportableChunkPart(MeshData mesh, string filePath, string fileName, long[] id) : base(mesh, filePath, fileName)
        {
            ID = id;
        }
    }

    public class ExportableMesh : Exportable
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
                var flags = new VertexFlags();

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
                    int[] rgb = new int[] { 0, 0, 0 };
                    
                    int exFlags = (int)ConfigLoader.Config.ExpMeshFlags;

                    for (int i = 0; i < Mesh.VerticesCount; i++)
                    {
                        rgb[0] = 0;
                        rgb[1] = 0;
                        rgb[2] = 0;

                        flags.All = Mesh.Flags[i];

                        if (((exFlags >> 0) & 1) != 0)
                        {
                            rgb[0] = GameMath.Max(rgb[0], Mesh.Rgba[i * 4 + 0]);
                            rgb[1] = GameMath.Max(rgb[1], Mesh.Rgba[i * 4 + 1]);
                            rgb[2] = GameMath.Max(rgb[2], Mesh.Rgba[i * 4 + 2]);
                        }
                        
                        if (((exFlags >> 1) & 1) != 0)
                        {
                            rgb[0] = GameMath.Max(rgb[0], Mesh.Rgba[i * 4 + 3]);
                            rgb[1] = GameMath.Max(rgb[1], Mesh.Rgba[i * 4 + 3]);
                            rgb[2] = GameMath.Max(rgb[2], Mesh.Rgba[i * 4 + 3]);
                        }
                        
                        if (((exFlags >> 2) & 1) != 0)
                        {
                            rgb[0] = GameMath.Max(rgb[0], flags.GlowLevel);
                            rgb[1] = GameMath.Max(rgb[1], flags.GlowLevel);
                            rgb[2] = GameMath.Max(rgb[2], flags.GlowLevel);
                        }

                        rgb[0] = GameMath.Min(rgb[0], 255);
                        rgb[1] = GameMath.Min(rgb[1], 255);
                        rgb[2] = GameMath.Min(rgb[2], 255);

                        tw.WriteLine();

                        tw.Write
                        (
                            string.Format(
                            "v {0} {1} {2} {3} {4} {5}",
                            Mesh.xyz[i * 3 + 0].ToString("F6"),
                            Mesh.xyz[i * 3 + 1].ToString("F6"),
                            Mesh.xyz[i * 3 + 2].ToString("F6"),

                            //bake into rgb glow level
                            (rgb[0] / 255f).ToString("F6"),
                            (rgb[1] / 255f).ToString("F6"),
                            (rgb[2] / 255f).ToString("F6")
                        ));
                    }

                    if (ConfigLoader.Config.MEWriteCustomData)
                    {
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
                    }

                    for (int i = 0; i < uvs.Length / 2; i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("vt {0} {1}", uvs[i * 2 + 0].ToString("F6"), uvs[i * 2 + 1].ToString("F6")));
                    }

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

    public class MassFileExportSystem : ClientSystem
    {
        public ConcurrentStack<Exportable> toExport = new ConcurrentStack<Exportable>();

        public ConcurrentStack<Exportable> toExportLast = new ConcurrentStack<Exportable>();

        public void Push(Exportable exportable)
        {
            toExportLast.Push(exportable);
        }

        public void PushFast(Exportable exportable)
        {
            toExport.Push(exportable);
        }

        public void Clear() => Clear<Exportable>();

        public void Clear<T>()
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

            Array.Reverse(items);

            foreach (var val in items)
            {
                if (val != null) toExport.Push(val);
            }
        }

        public MassFileExportSystem(ClientMain game) : base(game) {}

        public override string Name => "File Export";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public void Prepare()
        {
            Stack<int> indices = new Stack<int>();
            int i = 0;

            foreach (var exportable in toExport)
            {
                if (exportable is ExportableChunkPart)
                {
                    ExportableChunkPart a = (ExportableChunkPart)exportable;

                    foreach (var incoming in toExportLast)
                    {
                        if (incoming is ExportableChunkPart)
                        {
                            ExportableChunkPart b = (ExportableChunkPart)incoming;
                            if (a.Is(b))
                            {
                                indices.Push(i);
                                break;
                            }
                        }

                    }
                    i++;
                }
            }
            
            if (indices.Count == 0) return;

            var ind = indices.ToArray();
            var items = new Exportable[toExport.Count];
            toExport.TryPopRange(items);

            for (i = 0; i < ind.Length; i++)
            {
                items[ind[i]].Dispose();
                items[ind[i]] = null;
            }

            Array.Reverse(items);

            foreach (var val in items)
            {
                if (val != null) toExport.Push(val);
            }
        }

        public void PushToBottom()
        {
            if (toExportLast.IsEmpty) return;
            bool toExportWasEmpty = toExport.IsEmpty;

            Exportable[] toExportFallback = new Exportable[toExport.Count];
            Exportable[] toExportLastFallback = new Exportable[toExportLast.Count];

            if (!toExportWasEmpty)
            {
                toExport.CopyTo(toExportFallback, 0);
                Array.Reverse(toExportFallback);
            }

            toExportLast.CopyTo(toExportLastFallback, 0);
            Array.Reverse(toExportLastFallback);

            Exportable[] range = new Exportable[toExport.Count];


            if (toExportWasEmpty || toExport.TryPopRange(range) == range.Length)
            {
                Exportable[] newRange = new Exportable[toExportLast.Count];

                if (toExportLast.TryPopRange(newRange) == newRange.Length)
                {
                    toExport.PushRange(newRange);
                }
                else
                {
                    toExportLast.Clear();
                    toExportLast.PushRange(toExportLastFallback);
                };
                if (!toExportWasEmpty)
                {
                    Array.Reverse(range);
                    toExport.PushRange(range);
                }
            }
            else if (!toExportWasEmpty)
            {
                toExport.Clear();
                toExport.PushRange(toExportFallback);
            }
        }

        public override void OnSeperateThreadGameTick(float dt)
        {
            lock (toExport)
            {
                lock (toExportLast)
                {
                    try
                    {
                        Prepare();
                        PushToBottom();
                        ProcessStackedExportables();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public void ProcessStackedExportables()
        {
            int curCount = toExport.Count;
            
            int lastd = -1;

            if (curCount > 0) VSHUDTaskSystem.MainThreadActionsAPI.Enqueue((api) => api.ShowChatMessage("VSHUD File Export System received a batch of exportables, processing now."));

            for (int i = 0; i < curCount; i++)
            {
                int d = (int)((double)i / curCount * 10.0);

                if (d > lastd)
                {
                    string _ = string.Format("Exporting... |{0}{1}{2}{3}{4}{5}{6}{7}{8}|",
                        d > 0 ? "|" : " ",
                        d > 1 ? "|" : " ",
                        d > 2 ? "|" : " ",
                        d > 3 ? "|" : " ",
                        d > 4 ? "|" : " ",
                        d > 5 ? "|" : " ",
                        d > 6 ? "|" : " ",
                        d > 7 ? "|" : " ",
                        d > 8 ? "|" : " "
                    );

                    VSHUDTaskSystem.MainThreadActionsAPI.Enqueue((api) => api.ShowChatMessage(_));
                }
                
                lastd = d;

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

            if (curCount > 0) VSHUDTaskSystem.MainThreadActionsAPI.Enqueue((api) => api.ShowChatMessage("Batch done!"));
        }

        public override void Dispose(ClientMain game)
        {
            base.Dispose(game);
            Clear();
        }
    }
}
