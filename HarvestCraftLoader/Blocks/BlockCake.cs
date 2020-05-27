using Vintagestory.API.Common;

namespace HarvestCraftLoader
{
    class BlockCake : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCake cake = blockSel.Position != null ? world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCake : null;
            return cake?.OnBlockInteractStart(world, byPlayer, blockSel) ?? false;
        }
    }
}
