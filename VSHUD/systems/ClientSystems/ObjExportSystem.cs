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

namespace VSHUD
{
    class QueuedObj
    {
        public MeshData Mesh;
        public int ChunkX;
        public int ChunkY;
        public int ChunkZ;
        public EnumChunkRenderPass Pass;
        public int Lod;
        public int Seed;

        public QueuedObj(MeshData mesh, int chunkX, int chunkY, int chunkZ, EnumChunkRenderPass pass, int lod, int seed)
        {
            Mesh = mesh;
            ChunkX = chunkX;
            ChunkY = chunkY;
            ChunkZ = chunkZ;
            Pass = pass;
            Lod = lod;
            Seed = seed;
        }
    }

    class ObjExportSystem : ClientSystem
    {
        public static Queue<QueuedObj> queuedObjs = new Queue<QueuedObj>();

        public ObjExportSystem(ClientMain game) : base(game) {}

        public override string Name => "ObjExport";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => ExportEnqueuedObjs();

        public void ExportEnqueuedObjs()
        {
            for (int i = 0; i < queuedObjs.Count; i++)
            {
                var obj = queuedObjs.Dequeue();
                Export(obj.Mesh, obj.ChunkX, obj.ChunkY, obj.ChunkZ, obj.Pass, obj.Lod, obj.Seed);
            }
        }

        public static void Export(MeshData mesh, int chunkX, int chunkY, int chunkZ, EnumChunkRenderPass part, int lod, int seed)
        {
            ConvertToObj(mesh, string.Format("{0} {1} {2} {3} lod{4}", chunkX, chunkY, chunkZ, part, lod), seed);
        }

        private static void ConvertToObj(MeshData mesh, string filename, int seed)
        {
            mesh = mesh.Clone();
            try
            {
                mesh.Translate(-0.5f, -0.5f, -0.5f);

                float[] uvs = mesh.Uv;
                string path = Path.Combine(GamePaths.Binaries, "worldparts");
                path = Path.Combine(path, seed.ToString());
                Directory.CreateDirectory(path);
                path = Path.Combine(path, filename + ".obj");

                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.Write("o " + filename);

                    for (int i = 0; i < mesh.Uv.Length / 4; i++)
                    {
                        if (i + 4 > mesh.UvCount) continue;
                        float[] transform = new float[] { mesh.Uv[i * 4 + 0], mesh.Uv[i * 4 + 1], mesh.Uv[i * 4 + 2], mesh.Uv[i * 4 + 3] };

                        Mat22.Scale(transform, transform, new float[] { 1.0f, -1.0f });
                        Mat22X.Translate(transform, transform, new float[] { 0.0f, 1.0f });
                        mesh.Uv[i * 4 + 0] = transform[0];
                        mesh.Uv[i * 4 + 1] = transform[1];
                        mesh.Uv[i * 4 + 2] = transform[2];
                        mesh.Uv[i * 4 + 3] = transform[3];
                    }

                    for (int i = 0; i < mesh.VerticesCount; i++)
                    {
                        tw.WriteLine();
                        tw.Write("v");
                        tw.Write(" " + mesh.xyz[i * 3 + 0].ToString("F6"));
                        tw.Write(" " + mesh.xyz[i * 3 + 1].ToString("F6"));
                        tw.Write(" " + mesh.xyz[i * 3 + 2].ToString("F6"));
                    }

                    for (int i = 0; i < uvs.Length / 2; i++)
                    {
                        tw.WriteLine();
                        tw.Write("vt " + uvs[i * 2 + 0].ToString("F6"));
                        tw.Write(" " + uvs[i * 2 + 1].ToString("F6"));
                    }

                    tw.WriteLine();
                    tw.Write("usemtl BlockTextureAtlas");

                    for (int i = 0; i < mesh.Indices.Length / 3; i++)
                    {
                        tw.WriteLine();
                        tw.Write(string.Format("f {0}/{0} {1}/{1} {2}/{2}", mesh.Indices[i * 3 + 0] + 1, mesh.Indices[i * 3 + 1] + 1, mesh.Indices[i * 3 + 2] + 1));
                    }
                    tw.Close();
                }
            }
            catch (Exception)
            {
            }

        }
    }
}
