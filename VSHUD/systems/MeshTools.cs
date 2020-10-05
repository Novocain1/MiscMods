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
    class MeshTools : ClientModSystem
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
                    string name = asset.ToShortString().Replace("/", "-");
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), (api.TesselatorManager as ShapeTesselatorManager).shapes[asset], out MeshData mesh);
                    MassFileExportSystem.toExport.Push(new PreparedMesh(mesh, Path.Combine(GamePaths.Binaries, name + ".obj"), name + ".obj"));
                }
                else if (es != null)
                {
                    Shape loadedShape = es.Entity.Properties.Client.LoadedShape;
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), loadedShape, out MeshData mesh);
                    MassFileExportSystem.toExport.Push(new PreparedMesh(mesh, Path.Combine(GamePaths.Binaries, es.Entity.Code.ToShortString() + ".obj"), es.Entity.Code.ToShortString() + ".obj"));
                }
            });

            api.RegisterCommand("objworld", "", "", (p, a) =>
            {
                ConfigLoader.Config.CreateChunkObjs = a.PopBool() ?? !ConfigLoader.Config.CreateChunkObjs;
                capi.ShowChatMessage(string.Format("Chunk Tesselator .obj Caching {0}.", ConfigLoader.Config.CreateChunkObjs ? "Enabled" : "Disabled"));
                ConfigLoader.SaveConfig(capi);
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

                capi.InjectClientThread("ObjExport", 1000, new MassFileExportSystem(api.World as ClientMain));
            };
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
