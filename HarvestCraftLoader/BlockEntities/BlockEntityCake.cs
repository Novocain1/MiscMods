using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace HarvestCraftLoader
{
    class BlockEntityCake : BlockEntity
    {
        EnumCakeEatenState eatenState = EnumCakeEatenState.uneaten;
        MultiPropFood multiPropFood { get => Block.Attributes?["MultiPropFood"]?.AsObject<MultiPropFood>(); }

        public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side.IsServer())
            {
                Block eatento = eatenState < EnumCakeEatenState.slice6 ? world.BlockAccessor.GetBlock(Block.CodeWithVariant("eaten", Enum.GetName(typeof(EnumCakeEatenState), ++eatenState))) : world.GetBlock(0);
                world.SpawnCubeParticles(this.Pos, this.Pos.ToVec3d().Add(0.5, 0, 0.5), 1.0f, 32);
                multiPropFood?.AddNutrientsToPlayer(byPlayer as IServerPlayer);

                if (eatento.Id == 0) world.BlockAccessor.SetBlock(eatento.Id, this.Pos);
                else world.BlockAccessor.ExchangeBlock(eatento.Id, this.Pos);
            }
            else
            {
                byPlayer.Entity.PlayEntitySound("eat", byPlayer);
            }
            return true;
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            eatenState = (EnumCakeEatenState)Enum.Parse(typeof(EnumCakeEatenState), Block.Variant["eaten"]);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            eatenState = (EnumCakeEatenState)tree.GetInt("eatenstate");
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("eatenstate", (int)eatenState);
            base.ToTreeAttributes(tree);
        }
    }
}
