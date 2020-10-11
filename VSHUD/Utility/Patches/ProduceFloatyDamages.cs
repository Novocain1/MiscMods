using Vintagestory.API.Client;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VSHUD
{
    [HarmonyPatch(typeof(Entity), "ReceiveDamage")]
    class ProduceFloatyDamages
    {
        public static void Postfix(Entity __instance, float damage)
        {
            if (__instance.World.Side.IsClient()) new HudElementFloatyDamage(__instance.World.Api as ICoreClientAPI, damage, __instance.Pos.XYZ);
        }
    }
}
