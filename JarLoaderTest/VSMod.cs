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

                        api.World.Logger.StoryEvent("Converting jar asset paths...");
                        foreach (var domain in domainInfo.EnumerateDirectories())
                        {
                            string texturesPath = Path.Combine(domain.FullName, "textures");
                            string origBlocksPath = Path.Combine(texturesPath, "blocks");
                            string origItemsPath = Path.Combine(texturesPath, "items");

                            string origModelsPath = Path.Combine(domain.FullName, "models");
                            string origRecipesPath = Path.Combine(domain.FullName, "recipes");
                            string origLangPath = Path.Combine(domain.FullName, "lang");

                            if (Directory.Exists(origBlocksPath)) Directory.Move(origBlocksPath, Path.Combine(texturesPath, "block"));
                            if (Directory.Exists(origItemsPath)) Directory.Move(origItemsPath, Path.Combine(texturesPath, "item"));
                            if (Directory.Exists(origModelsPath)) Directory.Move(origModelsPath, Path.Combine(domain.FullName, "shapes"));

                            if (Directory.Exists(origRecipesPath)) Directory.Move(origRecipesPath, Path.Combine(domain.FullName, "forgerecipes"));
                            if (Directory.Exists(origLangPath)) Directory.Move(origLangPath, Path.Combine(domain.FullName, "forgelang"));
                        }
                    }

                    foreach (var domain in domainInfo.EnumerateDirectories())
                    {
                        api.World.Logger.StoryEvent("Adding path origin '{0}'...", domain.Name);
                        api.Assets.AddPathOrigin(domain.Name, domain.FullName);
                    }
                }
                catch (Exception ex)
                {
                    api.World.Logger.Notification("Failed loading jar with the name \'", jar.Name, "\' Exception thrown: ", ex);
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
