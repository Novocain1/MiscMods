using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HarvestCraftLoader
{
    class BlockFruit : BlockPlantFix
    {
        public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block cinnamon = blockAccessor.GetBlock(new AssetLocation("harvestcraftloader:fruits-cinnamon-ripe"));
            Block block = blockAccessor.GetBlock(pos.X, pos.Y + 1, pos.Z);
            return cinnamon.Id == Id ? true : block.BlockMaterial == EnumBlockMaterial.Leaves;
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool test = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            failureCode = test ? "__ignore__" : "requiresleavesabove";
            return test;
        }

        //mostly copied from BlockBeehive
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRand)
        {
            Block cinnamon = blockAccessor.GetBlock(new AssetLocation("harvestcraftloader:fruits-cinnamon-ripe"));

            for (int i = 1; i < 4; i++)
            {
                Block aboveBlock = blockAccessor.GetBlock(pos.X, pos.Y - i, pos.Z);

                if ((aboveBlock.BlockMaterial == EnumBlockMaterial.Wood || aboveBlock.BlockMaterial == EnumBlockMaterial.Leaves) && aboveBlock.SideSolid[BlockFacing.DOWN.Index])
                {
                    BlockPos atpos = new BlockPos(pos.X, pos.Y - i - 1, pos.Z);

                    Block block = blockAccessor.GetBlock(atpos);

                    if (block.BlockMaterial == EnumBlockMaterial.Wood && aboveBlock.BlockMaterial == EnumBlockMaterial.Wood && blockAccessor.GetBlock(pos.X, pos.Y - i - 2, pos.Z).BlockMaterial == EnumBlockMaterial.Wood && aboveBlock.LastCodePart() == "ud")
                    {
                        blockAccessor.SetBlock(cinnamon.BlockId, atpos);
                        if (EntityClass != null) blockAccessor.SpawnBlockEntity(EntityClass, atpos);

                        return true;
                    }

                    if (aboveBlock.BlockMaterial == EnumBlockMaterial.Wood || Id == cinnamon.Id || (block.BlockMaterial != EnumBlockMaterial.Leaves && block.BlockMaterial != EnumBlockMaterial.Air)) continue;

                    int dx = pos.X % blockAccessor.ChunkSize;
                    int dz = pos.Z % blockAccessor.ChunkSize;
                    int surfacey = blockAccessor.GetMapChunkAtBlockPos(atpos).WorldGenTerrainHeightMap[dz * blockAccessor.ChunkSize + dx];

                    if (pos.Y - surfacey < 4) return false;

                    blockAccessor.SetBlock(BlockId, atpos);
                    if (EntityClass != null) blockAccessor.SpawnBlockEntity(EntityClass, atpos);

                    return true;
                }
            }

            return false;
        }
    }
}