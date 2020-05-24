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
    class VSModSystem : ModSystem
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            capi = api as ICoreClientAPI;
            sapi = api as ICoreServerAPI;
            
            if (capi != null) capi.Event.BlockTexturesLoaded += () => LoadForgeJars();
            if (sapi != null) sapi.Event.SaveGameLoaded += () => LoadForgeJars();
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
                    if (!Directory.Exists(folderPath))
                    {
                        api.World.Logger.StoryEvent("Caching jar: \'", jar.Name + "\'...");
                        fastZip.ExtractZip(jar.FullName, folderPath, "");
                    }

                    if (api.Side.IsClient())
                    {
                        string assetsPath = Path.Combine(folderPath, "assets");
                        DirectoryInfo domainInfo = new DirectoryInfo(assetsPath);
                        foreach (var domain in domainInfo.EnumerateDirectories())
                        {
                            string texturesPath = Path.Combine(domain.FullName, "textures");
                            
                            string blockTexPath = Path.Combine(texturesPath, "blocks");
                            string itemTexPath = Path.Combine(texturesPath, "items");

                            IEnumerable<string> blockTextures = Directory.EnumerateFiles(blockTexPath, "*.png", SearchOption.AllDirectories);
                            IEnumerable<string> itemTextures = Directory.EnumerateFiles(itemTexPath, "*.png", SearchOption.AllDirectories);

                            LoadTextures(blockTextures, capi.BlockTextureAtlas, domain.Name, "block");
                            LoadTextures(itemTextures, capi.ItemTextureAtlas, domain.Name, "item");
                        }
                    }
                }
                catch (Exception ex)
                {
                    api.World.Logger.Notification("Failed loading jar with the name \'", jar.Name, "\' Exception thrown: ", ex);
                }
            }
        }

        public void LoadTextures(IEnumerable<string> paths, ITextureAtlasAPI textureAtlas, string domain, string type)
        {
            foreach (var texPath in paths)
            {
                FileInfo fileInfo = new FileInfo(texPath);

                using (StreamReader reader = new StreamReader(texPath))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        reader.BaseStream.Position = 0;
                        reader.BaseStream.CopyTo(ms);
                        byte[] bytes = ms.ToArray();
                        BitmapExternal bitmapExternal = capi.Render.BitmapCreateFromPng(bytes);
                        AssetLocation loc = new AssetLocation(domain, type + "/" + fileInfo.Name);

                        textureAtlas.InsertTextureCached(loc, bitmapExternal, out int texId, out TextureAtlasPosition texPos);

                        ms.Close();
                    }
                    reader.Close();
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
}
