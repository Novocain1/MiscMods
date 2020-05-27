using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HarvestCraftLoader
{
    class BlockCropHC : BlockCrop
    {
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string info = world.BlockAccessor.GetBlock(pos.DownCopy()).GetPlacedBlockInfo(world, pos.DownCopy(), forPlayer);

            return
                Lang.Get("Required Nutrient: {0}", CropProps.RequiredNutrient) + "\n" +
                Lang.Get("Growth Stage: {0} / {1}", CurrentStage(), CropProps.GrowthStages - 1) +
                (info != null && info.Length > 0 ? "\n\n" + Lang.Get("soil-tooltip") + "\n" + info : "")
            ;
        }
    }
}
