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
                var healthTree = __instance.WatchedAttributes.GetTreeAttribute("health");
                float maxHealth = healthTree.GetFloat("maxhealth"), baseMaxHealth = healthTree.GetFloat("basemaxhealth");
                maxHealth = maxHealth == 0 ? baseMaxHealth : 20.0f;

                __instance.WatchedAttributes.RegisterModifiedListener("health", () =>
                {
                    float lastHealth = __instance.WatchedAttributes.GetFloat("fldLastHealth", maxHealth);
                    float health = healthTree.GetFloat("currenthealth");
                    float dHealth = lastHealth - health;
                    new HudElementFloatyDamage(api as ICoreClientAPI, dHealth, __instance.Pos.XYZ);
                    __instance.WatchedAttributes.SetFloat("fldLastHealth", health);
                });
            }
        }
    }
}
