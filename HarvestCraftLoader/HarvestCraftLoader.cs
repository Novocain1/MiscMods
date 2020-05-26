using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Newtonsoft.Json;
using System.IO;
using Cairo;
using Vintagestory.API.Util;
using Path = System.IO.Path;
using System.Globalization;

namespace VSMod
{
    class HarvestCraftLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockCake", typeof(BlockCake));
            api.RegisterBlockEntityClass("Cake", typeof(BlockEntityCake));
        }
    }

    class BlockCake : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCake cake = blockSel.Position != null ? world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCake : null;
            return cake?.OnBlockInteractStart(world, byPlayer, blockSel) ?? false;
        }
    }

    class BlockEntityCake : BlockEntity
    {
        EnumEatenState eatenState = EnumEatenState.uneaten;
        
        public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side.IsServer())
            {
                Block eatento = eatenState < EnumEatenState.slice6 ? world.BlockAccessor.GetBlock(Block.CodeWithVariant("eaten", Enum.GetName(typeof(EnumEatenState), ++eatenState))) : world.GetBlock(0);
                world.SpawnCubeParticles(this.Pos, this.Pos.ToVec3d().Add(0.5, 0, 0.5), 1.0f, 32);
                if (eatento.Id == 0) world.BlockAccessor.SetBlock(eatento.Id, this.Pos);
                else world.BlockAccessor.ExchangeBlock(eatento.Id, this.Pos);
            }
            return true;
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            eatenState = (EnumEatenState)tree.GetInt("eatenstate");
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("eatenstate", (int)eatenState);
            base.ToTreeAttributes(tree);
        }
    }

    enum EnumEatenState
    {
        uneaten, slice1, slice2, slice3, slice4, slice5, slice6
    }
}
