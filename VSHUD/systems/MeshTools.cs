using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using OpenTK.Graphics.OpenGL;
using HarmonyLib;

namespace VSHUD
{
    [HarmonyPatch(typeof(ChunkTesselator), "NowProcessChunks")]
    class ChunkObjCreator
    {
        public static MeshData Combined = new MeshData(1, 1);
        public static bool Process = false;
        public static int Seed = 0;
        public static Vec3i SpawnPos;

        public static void Export(MeshData mesh, int chunkX, int chunkY, int chunkZ, EnumChunkRenderPass part, int lod)
        {
            ConvertToObj(mesh, string.Format("{0} {1} {2} {3} lod{4}", chunkX, chunkY, chunkZ, part, lod));
        }
        
        public static void Postfix(int chunkX, int chunkY, int chunkZ, ref TesselatedChunkPart[] __result)
        {
            if (!Process) return;

            int i = 0;
            foreach (var val in __result)
            {
                if (val == null) continue;
                
                var mesh0 = val.GetField<MeshData>("modelDataLod0").Clone();
                var mesh1 = val.GetField<MeshData>("modelDataLod1").Clone();
                var cPass = val.GetField<EnumChunkRenderPass>("pass");

                mesh0.Translate(new Vec3f(chunkX - SpawnPos.X, chunkY - SpawnPos.Y, chunkZ - SpawnPos.Z).Mul(32));
                mesh1.Translate(new Vec3f(chunkX - SpawnPos.X, chunkY - SpawnPos.Y, chunkZ - SpawnPos.Z).Mul(32));
                mesh0.CompactBuffers();
                mesh1.CompactBuffers();

                if (mesh0.VerticesCount > 0) Export(mesh0, chunkX, chunkY, chunkZ, cPass, 0);
                if (mesh1.VerticesCount > 0) Export(mesh1, chunkX, chunkY, chunkZ, cPass, 1);

                i++;
            }
        }

        private static void ConvertToObj(MeshData mesh, string filename)
        {
            mesh = mesh.Clone();
            try
            {
                mesh.Translate(-0.5f, -0.5f, -0.5f);

                float[] uvs = mesh.Uv;
                string path = Path.Combine(GamePaths.Binaries, "worldparts");
                path = Path.Combine(path, Seed.ToString());
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

    class MeshTools : VSHUDClientSystem
    {
        ICoreClientAPI capi;
        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.RegisterCommand("obj", "", "", (p, a) =>
            {
                var bs = api.World.Player.CurrentBlockSelection;
                var es = api.World.Player.CurrentEntitySelection;
                string word = a.PopWord("object");

                if (bs != null)
                {
                    var asset = api.World.BlockAccessor.GetBlock(bs.Position).Shape.Base;
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), (api.TesselatorManager as ShapeTesselatorManager).shapes[asset], out MeshData mesh);
                    ConvertToObj(mesh, word, true, false);
                }
                else if (es != null)
                {
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), es.Entity.Properties.Client.LoadedShape, out MeshData mesh);
                    ConvertToObj(mesh, word, true, false);
                }
            });
            api.RegisterCommand("meshdata", "", "", (p, a) =>
            {
                var bs = api.World.Player.CurrentBlockSelection;
                var es = api.World.Player.CurrentEntitySelection;
                string word = a.PopWord("object");

                if (bs != null)
                {
                    var asset = api.World.BlockAccessor.GetBlock(bs.Position).Shape.Base;
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), (api.TesselatorManager as ShapeTesselatorManager).shapes[asset], out MeshData mesh);
                    using (TextWriter tw = new StreamWriter(Path.Combine(GamePaths.Binaries, word + ".json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(mesh, Formatting.Indented));
                    }
                }
                else if (es != null)
                {
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), es.Entity.Properties.Client.LoadedShape, out MeshData mesh);
                    using (TextWriter tw = new StreamWriter(Path.Combine(GamePaths.Binaries, word + ".json")))
                    {
                        tw.Write(JsonConvert.SerializeObject(mesh, Formatting.Indented));
                    }
                }
            });

            api.RegisterCommand("objworld", "", "", (p, a) =>
            {
                ConvertToObj(ChunkObjCreator.Combined, "world", false, true);
                {
                    /*
                    MeshData mesh = new MeshData(1, 1);
                    int rad = a.PopInt() ?? 1;

                    var game = api.World as ClientMain;
                    var tess = new ChunkTesselator();
                    tess.SetField("game", game);
                    tess.Start();

                    var worldmap = game.GetField<ClientWorldMap>("WorldMap");
                    IBlockAccessor ba = api.World.BlockAccessor;

                    BlockPos playerPos = api.World.Player.Entity.Pos.AsBlockPos;
                    Vec3i startChunkPos = playerPos.GetChunkPos(ba);

                    for (int x = -rad; x < rad; x++)
                    {
                        for (int y = 0; y < (ba.MapSizeY / ba.ChunkSize); y++)
                        {
                            for (int z = -rad; z < rad; z++)
                            {
                                MeshData meshPart = new MeshData(1, 1);
                                IntRef intRef = new IntRef();

                                int extChunkSize = ba.ChunkSize + 2;
                                int[] blocks = new int[extChunkSize * extChunkSize * extChunkSize];
                                ushort[] light = new ushort[extChunkSize * extChunkSize * extChunkSize];
                                byte[] lightSat = new byte[extChunkSize * extChunkSize * extChunkSize];

                                Vec3i chunkPos = startChunkPos.AddCopy(x, 0, z);
                                chunkPos.Y = y;

                                IWorldChunk chunk = ba.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
                                if (chunk == null) continue;
                                chunk.Unpack();

                                worldmap.GetExtendedChunk(blocks, light, lightSat, chunkPos.X, chunkPos.Y, chunkPos.Z);

                                tess.BeginProcessChunk(chunkPos.X, chunkPos.Y, chunkPos.Z, chunk.Blocks, blocks, light, lightSat);
                                TesselatedChunkPart[] tessParts = tess.NowProcessChunks(chunkPos.X, chunkPos.Y, chunkPos.Z, intRef);

                                for (int i = 0; i < tessParts.Length; i++)
                                {
                                    if (tessParts[i] == null) continue;
                                    meshPart.AddMeshData(tessParts[i].GetField<MeshData>("modelDataLod0").Clone());
                                    meshPart.AddMeshData(tessParts[i].GetField<MeshData>("modelDataLod1").Clone());
                                }
                                meshPart.Translate(x * ba.ChunkSize, y * ba.ChunkSize, z * ba.ChunkSize);
                                mesh.AddMeshData(meshPart);
                            }
                        }
                    }



                    ConvertToObj(mesh, "world", false, true);
                    */
                }
                /*
                MeshData mesh = new MeshData(1, 1);
                BlockPos playerPos = api.World.Player.Entity.Pos.AsBlockPos;
                int rad = (int)a.PopInt(16);
                int yrad = (int)a.PopInt(16);
                ShapeTesselatorManager tesselatormanager = api.TesselatorManager as ShapeTesselatorManager;

                api.World.BlockAccessor.WalkBlocks(playerPos.AddCopy(rad, yrad, rad), playerPos.AddCopy(-rad, -yrad, -rad), (block, bpos) =>
                {
                    if (block.Id != 0 && api.World.BlockAccessor.GetLightLevel(bpos, EnumLightLevelType.MaxLight) > 0)
                    {
                        MeshData thismesh = tesselatormanager.blockModelDatasLod0.ContainsKey(block.Id) ?
                        tesselatormanager.blockModelDatasLod0[block.Id].Clone() : block.MeshInPos(bpos, api);

                        Vec3f translation = new Vec3f(-(playerPos.X - bpos.X), -(playerPos.Y - bpos.Y), -(playerPos.Z - bpos.Z));

                        thismesh.Translate(translation);
                        mesh.AddMeshData(thismesh);
                    }
                });
                ConvertToObj(mesh, "world", false, true);
                
            */
            });
        }

        private void ConvertToObj(MeshData mesh, string filename = "object", params bool[] flags)
        {
            mesh = mesh.Clone();
            try
            {
                Queue<float> uvsq = new Queue<float>();
                for (int i = 0; i < mesh.Uv.Length; i++)
                {
                    if (i + 4 > mesh.UvCount) continue;
                    float[] transform = new float[] { mesh.Uv[i], mesh.Uv[++i], mesh.Uv[++i], mesh.Uv[++i] };
                    if (flags[0])
                    {
                        Mat22.Scale(transform, transform, new float[] { capi.BlockTextureAtlas.Size.Width / 32, -(capi.BlockTextureAtlas.Size.Height / 32) });
                        Mat22X.Translate(transform, transform, new float[] { 0.0f, 1.0f });
                    }
                    if (flags[1])
                    {
                        Mat22.Scale(transform, transform, new float[] { 1.0f, -1.0f });
                        Mat22X.Translate(transform, transform, new float[] { 0.0f, 1.0f });
                    }

                    for (int j = 0; j < transform.Length; j++)
                    {
                        uvsq.Enqueue(transform[j]);
                    }
                }

                mesh.Translate(-0.5f, -0.5f, -0.5f);

                float[] uvs = uvsq.ToArray();

                using (TextWriter tw = new StreamWriter(Path.Combine(GamePaths.Binaries, filename + ".obj")))
                {
                    tw.WriteLine("o " + filename);
                    for (int i = 0; i < mesh.xyz.Length; i++)
                    {
                        if (i % 3 == 0)
                        {
                            if (i != 0) tw.WriteLine();
                            tw.Write("v " + mesh.xyz[i].ToString("F6"));
                        }
                        else
                        {
                            tw.Write(" " + mesh.xyz[i].ToString("F6"));
                        }

                    }
                    tw.WriteLine();
                    for (int i = 0; i < uvs.Length; i++)
                    {
                        if (i % 2 == 0)
                        {
                            if (i != 0) tw.WriteLine();
                            tw.Write("vt " + uvs[i].ToString("F6"));
                        }
                        else
                        {
                            tw.Write(" " + uvs[i].ToString("F6"));
                        }
                    }

                    tw.WriteLine();
                    for (int i = 0; i < mesh.Indices.Length; i++)
                    {
                        tw.WriteLine(
                            "f " + (mesh.Indices[i] + 1) + "/" + (mesh.Indices[i] + 1) + " "
                            + (mesh.Indices[++i] + 1) + "/" + (mesh.Indices[i] + 1) + " "
                            + (mesh.Indices[++i] + 1) + "/" + (mesh.Indices[i] + 1));
                    }
                    tw.Close();
                }
            }
            catch (Exception)
            {
            }

        }
    }

    public class Mat22X : Mat22
    {
        public static float[] Translate(float[] output, float[] a, float[] v)
        {
            output[0] = a[0] + v[0];
            output[1] = a[1] + v[1];
            output[2] = a[2] + v[0];
            output[3] = a[3] + v[1];
            return output;
        }
    }
}
