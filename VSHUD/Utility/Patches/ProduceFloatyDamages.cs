using Vintagestory.API.Client;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace VSHUD
{
    [HarmonyPatch(typeof(Entity), "Initialize")]
    class ProduceFloatyDamages
    {
        public static void Postfix(Entity __instance, EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            if (api.Side.IsClient())
            {
                __instance.WatchedAttributes.SetFloat("lastHealth", 0.0f);
                __instance.WatchedAttributes.RegisterModifiedListener("health", () =>
                {
                    float lastHealth = __instance.WatchedAttributes.GetFloat("lastHealth");
                    float health = __instance.WatchedAttributes.GetTreeAttribute("health").GetFloat("currenthealth");
                    float dHealth = lastHealth - health;
                    new HudElementFloatyDamage(api as ICoreClientAPI, dHealth, __instance.Pos.XYZ);
                    __instance.WatchedAttributes.SetFloat("lastHealth", health);
                });
            }
        }
    }
}
