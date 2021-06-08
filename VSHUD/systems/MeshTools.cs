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
    static class AssetExtensions
    {
        public static string GetSafeName(this AssetLocation asset)
        {
            return asset.ToShortString().Replace("/", "-");
        }
    }

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
                
                MeshData mesh = null;
                string name = a.PopWord();

                if (bs != null)
                {
                    var asset = api.World.BlockAccessor.GetBlock(bs.Position).Shape.Base;
                    name = name ?? asset.GetSafeName();
                    
                    api.Tesselator.TesselateShape(api.World.GetBlock(0), (api.TesselatorManager as ShapeTesselatorManager).shapes[asset], out mesh);
                }
                else if (es != null)
                {
                    Shape loadedShape = es.Entity.Properties.Client.LoadedShape;
                    var texPos = es.Entity.Properties.Client.Renderer as ITexPositionSource;
                    if (texPos == null) return;
                    
                    name = name ?? es.Entity.Code.GetSafeName();
                    api.Tesselator.TesselateShape("", loadedShape, out mesh, texPos);
                }

                if (mesh != null)
                {
                    lock (MassFileExportSystem.toExport)
                    {
                        MassFileExportSystem.toExport.Push(new ExportableMesh(mesh, Path.Combine(GamePaths.DataPath, name + ".obj"), name + ".obj"));
                    }
                }
            });

            api.RegisterCommand("objworld", "", "", (p, a) =>
            {
                string arg = a.PopWord("cache");
                switch (arg)
                {
                    case "cache":
                        ConfigLoader.Config.CreateChunkObjs = a.PopBool() ?? !ConfigLoader.Config.CreateChunkObjs;
                        capi.ShowChatMessage(string.Format("Chunk Tesselator .obj Caching {0}.", ConfigLoader.Config.CreateChunkObjs ? "Enabled" : "Disabled"));
                        break;
                    case "clear":
                        MassFileExportSystem.Clear<ExportableChunkPart>();
                        break;
                    default:
                        break;
                }

                ConfigLoader.SaveConfig(capi);
            });

            api.RegisterCommand("meshdata", "", "", (p, a) =>
            {
                var bs = api.World.Player.CurrentBlockSelection;
                var es = api.World.Player.CurrentEntitySelection;
                
                string name = a.PopWord();

                MeshData mesh = null;

                if (bs != null)
                {
                    var asset = api.World.BlockAccessor.GetBlock(bs.Position).Shape.Base;
                    name = name ?? asset.GetSafeName();

                    api.Tesselator.TesselateShape(api.World.GetBlock(0), (api.TesselatorManager as ShapeTesselatorManager).shapes[asset], out mesh);
                }
                else if (es != null)
                {
                    name = name ?? es.Entity.Code.GetSafeName();

                    api.Tesselator.TesselateShape(api.World.GetBlock(0), es.Entity.Properties.Client.LoadedShape, out mesh);
                }

                if (mesh != null)
                {
                    lock (MassFileExportSystem.toExport)
                    {
                        MassFileExportSystem.toExport.Push(new ExportableJsonObject(mesh, Path.Combine(GamePaths.DataPath, name)));
                    }
                }
            });
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
