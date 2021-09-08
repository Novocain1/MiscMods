using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace GravelSandFix
{
    public class GravelSandFixSystem : ModSystem
    {
        public Harmony harmony;
        public const string id = "Vintagestory.ModSystem.Novocain.GravelSandFixSystem";

        public override double ExecuteOrder() => 0.0;

        public override void StartClientSide(ICoreClientAPI api)
        {
            harmony = new Harmony(id);
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(id);
        }
    }

    [HarmonyPatch(typeof(ClientMain), "OnServerBlocksItemsReceived")]
    public class PatchTextures
    {
        public static Dictionary<AssetLocation, AssetLocation> PatchingBlocks = new Dictionary<AssetLocation, AssetLocation>()
        {
            {  new AssetLocation("game:muddygravel"), new AssetLocation("game:block/soil/tileless/muddygravel*") },
            {  new AssetLocation("game:peat"), new AssetLocation("game:block/soil/tileless/peat*") },
            {  new AssetLocation("game:gravel"), new AssetLocation("game:block/stone/tileless/gravel/{rock}*") },
            {  new AssetLocation("game:sand"), new AssetLocation("game:block/stone/tileless/sand/{rock}*") }
        };

        [HarmonyPrefix]
        public static void PreFix(ref IList<Block> blocks)
        {
            foreach (Block block in blocks)
            {
                AssetLocation val = null;

                if (PatchingBlocks.Any(a => {
                    if (a.Key.FirstPathPart() == block.FirstCodePart())
                    {
                        val = a.Value;
                        return true;
                    }
                    return false;
                }))
                {
                    if (val != null && block.Textures.ContainsKey("all"))
                    {
                        if (val.Path.Contains('{'))
                        {
                            var x = val.Path.Split('{', '}');

                            var r = val.Clone();
                            r.Path = r.Path.Replace('{' + x[1] + '}', block.Variant[x[1]]);
                            
                            block.Textures["all"].Base = r;
                        }
                        else
                        {
                            block.Textures["all"].Base = val;
                        }
                    }
                }
            }
        }
    }
}
