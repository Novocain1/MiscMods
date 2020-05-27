using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Newtonsoft.Json;
using System.IO;
using Cairo;
using Vintagestory.API.Util;
using Path = System.IO.Path;
using System.Globalization;
using ICSharpCode.SharpZipLib.Zip;
using Vintagestory.API.Server;

namespace VSMod
{
    class ForgeJarLoader : ModSystem
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        

        public override double ExecuteOrder() => 0.1;

        public static readonly Dictionary<string, string> VSEquivelentShapes = new Dictionary<string, string>()
        {
            { "minecraft:block/cross", "game:shapes/block/basic/shortcross.json" },
            { "block/cross", "game:shapes/block/basic/shortcross.json" },
            { "block/cube", "game:shapes/block/basic/cube.json" },
            { "block/cube_all", "game:shapes/block/basic/cube.json" },
            { "minecraft:block/cube", "game:shapes/block/basic/cube.json" },
            { "minecraft:block/cube_all", "game:shapes/block/basic/cube.json" },
            { "block/crop", "game:shapes/block/plant/crop/default1.json" },
            { "minecraft:block/crop", "game:shapes/block/plant/crop/default1.json" },
        };

        public Dictionary<string, bool> fresh = new Dictionary<string, bool>();

        public override void Start(ICoreAPI api)
        {
            Convert();
        }

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;
            capi = api as ICoreClientAPI;
            sapi = api as ICoreServerAPI;

            LoadForgeJars();
        }

        public void LoadForgeJars()
        {
            DirectoryInfo info = new DirectoryInfo(Path.Combine(GamePaths.Binaries, "ForgeMods"));
            FastZip fastZip = new FastZip();

            foreach (var jar in info.EnumerateFiles())
            {
                if (jar.Extension != ".jar") continue;
                string folderPath = Path.Combine(GamePaths.Cache, "forgemods", jar.Name);

                try
                {
                    string assetsPath = Path.Combine(folderPath, "assets");
                    DirectoryInfo domainInfo = new DirectoryInfo(assetsPath);

                    if (!Directory.Exists(folderPath))
                    {
                        api.World.Logger.StoryEvent("Caching jar: '{0}'...", jar.Name);
                        fastZip.ExtractZip(jar.FullName, folderPath, "");
                        fresh[folderPath] = true;
                    }
                    else fresh[folderPath] = false;

                    foreach (var domain in domainInfo.EnumerateDirectories())
                    {
                        api.World.Logger.StoryEvent("Adding path origin '{0}'...", domain.Name);
                        api.Assets.AddPathOrigin(domain.Name, domain.FullName);
                    }
                }
                catch (Exception ex)
                {
                    api.World.Logger.Notification("Failed loading jar with the name '{0}', Execption thrown: {1}", jar.Name, ex);
                }
            }
        }

        public void Convert()
        {
            api.World.Logger.StoryEvent("Converting jar asset paths...");
            foreach (var jarmod in Directory.GetDirectories(Path.Combine(GamePaths.Cache, "forgemods")))
            {
                if (!fresh[jarmod]) continue;

                string assetsPath = Path.Combine(jarmod, "assets");
                DirectoryInfo domainInfo = new DirectoryInfo(assetsPath);

                foreach (var domain in domainInfo.EnumerateDirectories())
                {
                    string texturesPath = Path.Combine(domain.FullName, "textures");
                    string origBlocksPath = Path.Combine(texturesPath, "blocks");
                    string origItemsPath = Path.Combine(texturesPath, "items");

                    string origModelsPath = Path.Combine(domain.FullName, "models");
                    string origRecipesPath = Path.Combine(domain.FullName, "recipes");
                    string origLangPath = Path.Combine(domain.FullName, "lang");
                    string newShapesPath = Path.Combine(domain.FullName, "shapes");


                    if (Directory.Exists(origBlocksPath)) Directory.Move(origBlocksPath, Path.Combine(texturesPath, "block"));
                    if (Directory.Exists(origItemsPath)) Directory.Move(origItemsPath, Path.Combine(texturesPath, "item"));
                    if (Directory.Exists(origModelsPath)) Directory.Move(origModelsPath, newShapesPath);

                    if (Directory.Exists(origRecipesPath)) Directory.Move(origRecipesPath, Path.Combine(domain.FullName, "forgerecipes"));
                    if (Directory.Exists(origLangPath)) Directory.Move(origLangPath, Path.Combine(domain.FullName, "forgelang"));

                    api.World.Logger.StoryEvent("Converting shapes in '{0}'...", domain.Name);

                    foreach (var shapePath in Directory.EnumerateFiles(newShapesPath, "*.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            DummyForgeModel shape;
                            using (TextReader tr = new StreamReader(shapePath))
                            {
                                string data = tr.ReadToEnd();
                                shape = JsonConvert.DeserializeObject<DummyForgeModel>(data);
                                if (shape != null)
                                {
                                    if (shape.Textures != null)
                                    {
                                        Dictionary<string, AssetLocation> newTextures = new Dictionary<string, AssetLocation>();
                                        foreach (var tex in shape.Textures)
                                        {
                                            newTextures[tex.Key] = new AssetLocation(tex.Value.ToString().Replace(":blocks", ":block"));
                                            newTextures[tex.Key] = new AssetLocation(newTextures[tex.Key].ToString().Replace(":items", ":item"));
                                        }
                                        shape.Textures = newTextures;
                                    }
                                    if (shape.Elements == null && shape.Parent != null)
                                    {
                                        if (!shape.Parent.StartsWith(domain.Name + ":"))
                                        {
                                            if (VSEquivelentShapes.ContainsKey(shape.Parent))
                                            {
                                                Shape parentShape = api.Assets.TryGet(VSEquivelentShapes[shape.Parent])?.ToObject<Shape>();
                                                if (parentShape != null) {
                                                    List<ShapeElement> psElements = new List<ShapeElement>(parentShape?.Elements ?? new ShapeElement[] { });
                                                    shape.Elements = psElements.ToArray();
                                                }
                                            }
                                            else shape.Elements = api.Assets.TryGet(VSEquivelentShapes["block/cube"])?.ToObject<Shape>().Elements;
                                        }
                                        else
                                        {
                                            string odShape = Directory.EnumerateFiles(newShapesPath, shape.Parent.Split('/').Last() + ".json", SearchOption.AllDirectories)?.First();
                                            if (odShape != null)
                                            {
                                                using (TextReader odShapeReader = new StreamReader(odShape))
                                                {
                                                    string oData = odShapeReader.ReadToEnd();
                                                    Shape parentShape = JsonConvert.DeserializeObject<DummyForgeModel>(oData);
                                                    List<ShapeElement> psElements = new List<ShapeElement>(parentShape?.Elements ?? new ShapeElement[] { });
                                                    shape.Elements = psElements.ToArray();
                                                    odShapeReader.Close();
                                                }
                                            }

                                        }
                                    }
                                }

                                tr.Close();
                            }
                            using (TextWriter tw = new StreamWriter(shapePath))
                            {
                                tw.Write(JsonConvert.SerializeObject(shape as Shape, Formatting.Indented));
                                tw.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            api.World.Logger.Notification("Failed converting shape with the path '{0}', Execption thrown: {1}", shapePath, ex);
                        }
                    }
                    api.World.Logger.StoryEvent("Done converting domain {0}.", domain.Name);
                }
            }

        }

        public void ClearCachedJars()
        {
            api.World.Logger.StoryEvent("Cleaning Jar Cache...");
            DirectoryInfo jarModCache = new DirectoryInfo(Path.Combine(GamePaths.Cache, "forgemods"));
            jarModCache.Delete(true);
        }
    }

    class DummyForgeModel : Shape
    {
        [JsonProperty("parent")]
        public string Parent { get; set; }
    }
}
