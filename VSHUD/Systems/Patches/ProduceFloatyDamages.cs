using Vintagestory.API.Client;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    [HarmonyPatch(typeof(Entity), "Initialize")]
    class ProduceFloatyDamages
    {
        public static bool CanCreate(ICoreClientAPI api, Entity entity)
        {
            if (api.Side.IsClient())
            {
                if (ConfigLoader.Config.FDShow)
                {
                    var playerEntity = api.World.Player.Entity;
                    var playerPos = playerEntity.Pos.XYZ.AddCopy(playerEntity.LocalEyePos);
                    var entityPos = entity.Pos.XYZ.AddCopy(entity.LocalEyePos);
                    var blockSel = new BlockSelection();
                    var entitySel = new EntitySelection();

                    if (playerPos.DistanceTo(entityPos) > ConfigLoader.Config.FDRange) return false;

                    BlockFilter bFilter = (pos, block) => block.CollisionBoxes != null;
                    
                    api.World.RayTraceForSelection(playerPos, entityPos, ref blockSel, ref entitySel, bFilter);

                    return entitySel?.Entity?.Equals(entity) ?? false;
                }
            }
            return false;
        }

        public static void Postfix(Entity __instance, EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            if (api.Side.IsClient())
            {
                var healthTree = __instance.WatchedAttributes.GetTreeAttribute("health");
                if (healthTree == null) return;
                float maxHealth = healthTree.GetFloat("maxhealth"), baseMaxHealth = healthTree.GetFloat("basemaxhealth");
                maxHealth = maxHealth == 0 ? baseMaxHealth : 20.0f;

                __instance.WatchedAttributes.RegisterModifiedListener("health", () =>
                {
                    float lastHealth = __instance.WatchedAttributes.GetFloat("fldLastHealth", maxHealth);
                    float health = healthTree.GetFloat("currenthealth");
                    float dHealth = lastHealth - health;
                    if (dHealth != 0 && CanCreate((ICoreClientAPI)api, __instance)) new HudElementFloatyDamage(api as ICoreClientAPI, dHealth, __instance.Pos.XYZ);
                    __instance.WatchedAttributes.SetFloat("fldLastHealth", health);
                });
            }
        }
    }
}
