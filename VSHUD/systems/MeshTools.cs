using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace VSHUD
{
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

            api.Event.LevelFinalize += () =>
            {
                string path = Path.Combine(GamePaths.Binaries, "worldparts");
                path = Path.Combine(path, api.World.Seed.ToString(), "textures");
                Directory.CreateDirectory(path);

                ClientMain game = (api.World as ClientMain);
                game.SetField("guiShaderProg", ShaderPrograms.Gui);
                BlockTextureAtlasManager mgr = game.GetField<BlockTextureAtlasManager>("BlockAtlasManager");

                for (int i = 0; i < mgr.Atlasses.Count; i++)
                {
                    mgr.Atlasses[i].Export(Path.Combine(path, "blockAtlas-" + i), game, mgr.AtlasTextureIds[i]);
                }

                ObjExportThread objExportThread = new ObjExportThread(api);
            };
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
