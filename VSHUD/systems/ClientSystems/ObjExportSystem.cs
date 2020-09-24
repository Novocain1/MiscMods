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
    class QueuedObj
    {
        public MeshData Mesh;
        public string FilePath;
        public string FileName;

        public QueuedObj(MeshData mesh, string filePath, string fileName)
        {
            Mesh = mesh.Clone();
            FilePath = filePath;
            FileName = fileName;
        }

        public void Export()
        {
            try
            {
                Mesh.Translate(-0.5f, -0.5f, -0.5f);

                float[] uvs = Mesh.Uv;

                using (TextWriter tw = new StreamWriter(FilePath))
                {
                    tw.Write("o " + FileName);

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

    class ObjExportSystem : ClientSystem
    {
        public static ConcurrentStack<QueuedObj> queuedObjs = new ConcurrentStack<QueuedObj>();

        public ObjExportSystem(ClientMain game) : base(game) {}

        public override string Name => "ObjExport";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => ExportEnqueuedObjs();

        public void ExportEnqueuedObjs()
        {
            for (int i = 0; i < queuedObjs.Count; i++)
            {
                bool success = queuedObjs.TryPop(out QueuedObj obj);
                if (success) obj.Export();
            }
        }
    }
}
