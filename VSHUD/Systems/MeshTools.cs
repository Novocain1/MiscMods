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
using Vintagestory.GameContent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

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

            api.RegisterCommand("exportmap", "Exports the map as pngs.", "", (p, a) =>
            {
                var cMap = api.ModLoader.GetModSystem<WorldMapManager>()?.MapLayers?.OfType<ChunkMapLayer>()?.Single();
                if (cMap != null)
                {
                    var mapDB = cMap.GetField<MapDB>("mapdb");
                    var bA = api.World.BlockAccessor;
                    
                    Bitmap img = new Bitmap(bA.ChunkSize, bA.ChunkSize, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    Rectangle rect = new Rectangle(0, 0, bA.ChunkSize, bA.ChunkSize);

                    api.ShowChatMessage(string.Format("Searching {0} chunks for map tiles...", (bA.MapSizeX / bA.ChunkSize) * (bA.MapSizeZ / bA.ChunkSize)));

                    VSHUDTaskSystem.Actions.Enqueue(() =>
                    {
                        ulong searchedCount = 0;
                        ulong foundCount = 0;

                        for (int x = 0; x < bA.MapSizeX / bA.ChunkSize; x++)
                        {
                            for (int z = 0; z < bA.MapSizeZ / bA.ChunkSize; z++)
                            {
                                var mChunk = new Vec2i(x, z);
                                searchedCount++;

                                if (bA.GetMapChunk(mChunk) != null)
                                {
                                    foundCount++;

                                    VSHUDTaskSystem.Actions.Enqueue(() =>
                                    {
                                        var piece = mapDB.GetMapPiece(mChunk);
                                        if (piece != null)
                                        {
                                            BitmapData bmpData = img.LockBits(rect, ImageLockMode.ReadWrite, img.PixelFormat);

                                            for (int i = 0; i < piece.Pixels.Length; i++)
                                            {
                                                int b = ColorUtil.ColorR(piece.Pixels[i]);
                                                int g = ColorUtil.ColorG(piece.Pixels[i]);
                                                int r = ColorUtil.ColorB(piece.Pixels[i]);
                                                int newpix = r << 16 | g << 08 | b << 00;

                                                Marshal.WriteInt32(bmpData.Scan0, i * sizeof(int), newpix);
                                            }

                                            string path = Path.Combine(GamePaths.DataPath, "SavedMaps", api.World.Seed.ToString());
                                            
                                            Directory.CreateDirectory(path);
                                            var dft = api.World.DefaultSpawnPosition.AsBlockPos;
                                            var pos = new Vec2i(dft.X / bA.ChunkSize, dft.Z / bA.ChunkSize);
                                            pos.X -= mChunk.X;
                                            pos.Y -= mChunk.Y;

                                            img.Save(Path.Combine(path, string.Format("X={0} Z={1}.png", pos.X, pos.Y)), ImageFormat.Png);
                                            img.UnlockBits(bmpData);
                                        }
                                    });
                                }
                            }
                        }

                        VSHUDTaskSystem.MainThreadActions.Enqueue(() =>
                        {
                            api.ShowChatMessage(string.Format("Found {0} tiles, enqueued export tasks.", foundCount));
                        });
                    });
                }
            });

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
