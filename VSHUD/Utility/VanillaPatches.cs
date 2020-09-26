using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSHUD
{
    class VanillaPatches : ClientModSystem
    {
        public override void StartClientSide(ICoreClientAPI api) => Patch();

        public void Patch()
        {
            var Harmony = new Harmony("ModSystem.VanillaPatches");
            Harmony.PatchAll();
        }
    }
}
