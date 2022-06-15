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
using System.Collections.Concurrent;
using System.Security.AccessControl;

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

            var Main = api.ModLoader.GetModSystem<VSHUDMain>();

            api.RegisterCommand("exportmap", "Exports the map as pngs.", "", (p, args) =>
            {
                var fmt = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
                var cMap = api.ModLoader.GetModSystem<WorldMapManager>()?.MapLayers?.OfType<ChunkMapLayer>()?.Single();
                if (cMap != null)
                {
                    var mapDB = cMap.GetField<MapDB>("mapdb");
                    var bA = api.World.BlockAccessor;

                    Bitmap img = new Bitmap(bA.ChunkSize * 3, bA.ChunkSize * 3, fmt);
                    Rectangle rect = new Rectangle(0, 0, bA.ChunkSize * 3, bA.ChunkSize * 3);

                    VSHUDTaskSystem.Actions.Enqueue(() =>
                    {
                        string path = Path.Combine(GamePaths.DataPath, "SavedMaps", api.World.Seed.ToString());

                        Directory.CreateDirectory(path);
                        VSHUDTaskSystem.MainThreadActions.Enqueue(() =>
                        {
                            var mapDats = cMap.GetField<ConcurrentDictionary<Vec2i, MultiChunkMapComponent>>("loadedMapData");
                            
                            foreach (var mapDat in mapDats)
                            {
                                var texture = mapDat.Value.Texture;
                                if (texture != null)
                                {
                                    BitmapData bmpData = img.LockBits(rect, ImageLockMode.ReadWrite, img.PixelFormat);

                                    GL.BindTexture(TextureTarget.Texture2D, texture.TextureId);
                                    
                                    GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

                                    img.Save(Path.Combine(path, string.Format("X={0} Z={1}.png", mapDat.Value.chunkCoord.X, mapDat.Value.chunkCoord.Y)), ImageFormat.Png);
                                    img.UnlockBits(bmpData);
                                }
                            }

                            bool stitch = args.PopWord()?.ToLowerInvariant() == "stitch";

                            if (stitch)
                            {
                                VSHUDTaskSystem.Actions.Enqueue(() =>
                                {
                                    Bitmap blankTile = new Bitmap(bA.ChunkSize * 3, bA.ChunkSize * 3, fmt);

                                    string stitchPath = Path.Combine(path, "stitch");
                                    Directory.CreateDirectory(stitchPath);
                                    string[] files = Directory.GetFiles(path);
                                    int minX = 0, maxX = 0, minY = 0, maxY = 0;
                                    int i = 0;
                                    foreach (var file in files)
                                    {
                                        string[] xy = Path.GetFileName(file).Replace(".png", "").Split(' ');
                                        xy[0] = xy[0].Remove(0, 2);
                                        xy[1] = xy[1].Remove(0, 2);

                                        int xParse = int.Parse(xy[0]);
                                        int yParse = int.Parse(xy[1]);
                                        if (i == 0) { minX = xParse; maxX = xParse; minY = yParse; maxY = yParse; }

                                        minX = minX > xParse ? xParse : minX;
                                        minY = minY > yParse ? yParse : minY;
                                        maxX = maxX < xParse ? xParse : maxX;
                                        maxY = maxY < yParse ? yParse : maxY;
                                        i++;
                                    }

                                    int w = maxX - minX;
                                    int h = maxY - minY;

                                    Bitmap stitched = new Bitmap(w * bA.ChunkSize, h * bA.ChunkSize, fmt);

                                    using (var canvas = Graphics.FromImage(stitched))
                                    {
                                        canvas.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                        for (int x = minX, lx = 0; x < maxX; x++, lx++)
                                        {
                                            for (int y = minX, ly = 0; y < maxY; y++, ly++)
                                            {
                                                string name = string.Format("X={0} Z={1}", x, y);
                                                string file = Path.Combine(path, string.Format("{0}.png", name));

                                                if (File.Exists(file))
                                                {
                                                    FileInfo fileInfo = new FileInfo(file);

                                                    while (IsFileLocked(fileInfo)) ; ;

                                                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                                                    {
                                                        Bitmap bmp = (Bitmap)Image.FromStream(fs);
                                                        canvas.DrawImage(bmp, lx * bA.ChunkSize, ly * bA.ChunkSize);
                                                    }
                                                }
                                            }
                                        }
                                        canvas.Save();
                                    }
                                    stitched.Save(Path.Combine(stitchPath, "0.png"), ImageFormat.Png);
                                });
                            }
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
                    Main.fileExport.Push(new ExportableMesh(mesh, Path.Combine(GamePaths.DataPath, name + ".obj"), name + ".obj"));
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
                        Main.fileExport.Clear<ExportableChunkPart>();
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
                    Main.fileExport.toExport.Push(new ExportableJsonObject(mesh, Path.Combine(GamePaths.DataPath, name)));
                }
            });
        }

        public bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
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
