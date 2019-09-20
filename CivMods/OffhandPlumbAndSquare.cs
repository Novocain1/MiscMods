using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CivMods
{
    class OffhandPlumbAndSquare : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.DidPlaceBlock += TriggerOffhand;
            api.Event.DidBreakBlock += TriggerRemoveReinforcement;
        }

        private void TriggerRemoveReinforcement(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
        {
            ItemSlot offhand = byPlayer?.InventoryManager?.GetHotbarInventory()?[10];
            if (offhand?.Itemstack?.Item is ItemPlumbAndSquare)
            {
                var handling = EnumHandHandling.Handled;
                (offhand.Itemstack.Item as ItemPlumbAndSquare).OnHeldAttackStart(offhand, byPlayer.Entity, blockSel, null, ref handling);
                if (handling != EnumHandHandling.Handled)
                {
                    byPlayer.Entity.World.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
                }
            }
        }

        private void TriggerOffhand(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            ItemSlot offhand = byPlayer?.InventoryManager?.GetHotbarInventory()?[10];
            if (offhand?.Itemstack?.Item is ItemPlumbAndSquare)
            {
                var handling = EnumHandHandling.Handled;
                (offhand.Itemstack.Item as ItemPlumbAndSquare).OnHeldInteractStart(offhand, byPlayer.Entity, blockSel, null, true, ref handling);
            }
        }
    }

    class BedSpawn : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockBed", typeof(SpawnSettingBed));
        }
    }

    class SpawnSettingBed : BlockBed
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel?.Position;
            if (world.Side.IsServer() && world.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.BuildOrBreak))
            {
                (byPlayer as IServerPlayer).SetSpawnPosition(new PlayerSpawnPos(pos.X, pos.Y, pos.Z));
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
