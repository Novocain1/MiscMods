using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    class MeshTools : ModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            api.RegisterCommand("obj", "", "", (p, a) =>
            {
                var bs = api.World.Player.CurrentBlockSelection;
                var es = api.World.Player.CurrentEntitySelection;
                string word = a.PopWord("object");

                if (bs != null)
                {
                    var asset = api.World.BlockAccessor.GetBlock(bs.Position).Shape.Base;
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), (api.TesselatorManager as ShapeTesselatorManager).shapes[asset], out MeshData mesh);
                    ConvertToObj(mesh, word);
                }
                else if (es != null)
                {
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), es.Entity.Properties.Client.LoadedShape, out MeshData mesh);
                    ConvertToObj(mesh, word);
                }
            });
            api.RegisterCommand("objworld", "", "", (p, a) =>
            {
                MeshData mesh = new MeshData(1, 1);
                BlockPos playerPos = api.World.Player.Entity.Pos.AsBlockPos;
                int rad = (int)a.PopInt(16);
                int yrad = (int)a.PopInt(16);
                bool fixuv = (bool)a.PopBool(false);

                api.World.BlockAccessor.WalkBlocks(playerPos.AddCopy(rad, yrad, rad), playerPos.AddCopy(-rad, -yrad, -rad), (block, bpos) =>
                {
                    if (block.Id != 0 && api.World.BlockAccessor.GetLightLevel(bpos, EnumLightLevelType.MaxLight) > 0)
                    {
                        api.Tesselator.TesselateBlock(block, out MeshData thismesh);
                        thismesh = thismesh.Clone();
                        Vec3f translation = new Vec3f(playerPos.X - bpos.X, playerPos.Y - bpos.Y, playerPos.Z - bpos.Z);
                        /*
                        if (block.Lod0Shape?.Base != null)
                        {
                            api.Tesselator.TesselateShape(block, api.TesselatorManager.GetCachedShape(block.Lod0Shape.Base), out MeshData lod);
                            thismesh.AddMeshData(lod);
                        }
                        */
                        thismesh.Translate(translation);
                        mesh.AddMeshData(thismesh);
                    }
                });
                mesh.Rotate(new Vec3f(0, 0, 0), GameMath.DEG2RAD * 180, 0, 0);
                ConvertToObj(mesh, "world", fixuv);
            });

        }

        private void ConvertToObj(MeshData mesh, string filename = "object", bool fixuv = true)
        {
            mesh = mesh.Clone();
            try
            {
                Queue<float> uvsq = new Queue<float>();
                if (fixuv)
                {
                    for (int i = 0; i < mesh.Uv.Length; i++)
                    {
                        if (i + 4 > mesh.UvCount) continue;
                        float[] transform = new float[] { mesh.Uv[i], mesh.Uv[++i], mesh.Uv[++i], mesh.Uv[++i] };
                        Mat22.Scale(transform, transform, new float[] { 128.0f, -64.0f });
                        Mat22X.Translate(transform, transform, new float[] { 0.0f, 1.0f });
                        for (int j = 0; j < transform.Length; j++)
                        {
                            uvsq.Enqueue(transform[j]);
                        }
                    }
                    mesh.Translate(-0.5f, -0.5f, -0.5f);
                }
                else
                {
                    uvsq = new Queue<float>(mesh.Uv);
                }

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
