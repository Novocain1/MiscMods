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
        }

        private void ConvertToObj(MeshData mesh, string filename = "object", bool fixuv = true)
        {
            mesh = mesh.Clone();
            try
            {
                Queue<float> uvsq = new Queue<float>();
                Queue<float> vecq = new Queue<float>();
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
