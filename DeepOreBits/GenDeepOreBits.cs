using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace DeepOreBits
{
    class DeepOreBitsModSystem : ModSystem
    {
        public Harmony sidedHarmony;
        public string harmonyID = "Novocain.ModSystem.DeepOreBitsModSystem";

        public override void Start(ICoreAPI api)
        {
            harmonyID += '.' + Enum.GetName(typeof(EnumAppSide), api.Side);
            sidedHarmony = new Harmony(harmonyID);
            sidedHarmony.PatchAll();
        }

        public override void Dispose()
        {
            sidedHarmony.UnpatchAll(harmonyID);
        }
    }

    [HarmonyPatch(typeof(DepositGeneratorRegistry), "CreateGenerator")]
    public class UpChances
    {
        public static void Postfix(ref DepositGeneratorBase __result)
        {
            if (__result is DiscDepositGenerator)
            {
                var gen = __result as DiscDepositGenerator;

                gen.SurfaceBlockChance = 1.0f;
                gen.GenSurfaceBlockChance = 0.1f;
            }
        }
    }
}
