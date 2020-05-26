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
using Vintagestory.API.Server;

namespace HarvestCraftLoader
{
    class HarvestCraftLoaderMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockCake", typeof(BlockCake));
            api.RegisterBlockClass("BlockCropHC", typeof(BlockCropHC));
            api.RegisterBlockEntityClass("Cake", typeof(BlockEntityCake));
            api.RegisterItemClass("ItemPlantableSeedHC", typeof(ItemPlantableSeedHC));
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
        MultiPropFood multiPropFood { get => Block.Attributes?["MultiPropFood"]?.AsObject<MultiPropFood>(); }

        public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side.IsServer())
            {
                Block eatento = eatenState < EnumEatenState.slice6 ? world.BlockAccessor.GetBlock(Block.CodeWithVariant("eaten", Enum.GetName(typeof(EnumEatenState), ++eatenState))) : world.GetBlock(0);
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
            eatenState = (EnumEatenState)Enum.Parse(typeof(EnumEatenState), Block.Variant["eaten"]);
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

    class MultiPropFood
    {
        [JsonProperty]
        public FoodNutritionProperties[] NutritionProperties { get; set; }

        [JsonProperty]
        public int Division { get; set; }

        public void AddNutrientsToPlayer(IServerPlayer player)
        {
            for (int i = 0; i < Division; i++)
            {
                for (int j = 0; j < NutritionProperties.Length; j++)
                {
                    player?.Entity.ReceiveSaturation(NutritionProperties[j].Satiety / Division, NutritionProperties[j].FoodCategory);
                }
            }
        }
    }

    class ItemPlantableSeedHC : ItemPlantableSeed
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null) return;

            BlockPos pos = blockSel.Position;

            string lastCodePart = itemslot.Itemstack.Collectible.LastCodePart();

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
            if (be is BlockEntityFarmland)
            {
                Block cropBlock = byEntity.World.GetBlock(CodeWithPath("crops-" + lastCodePart + "-0"));
                if (cropBlock == null) return;

                IPlayer byPlayer = null;
                if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                bool planted = (bool)((BlockEntityFarmland)be).CallMethod("TryPlant", cropBlock);
                if (planted)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), pos.X, pos.Y, pos.Z, byPlayer);

                    ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                    if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                    {
                        itemslot.TakeOut(1);
                        itemslot.MarkDirty();
                    }
                }

                if (planted) handHandling = EnumHandHandling.PreventDefault;
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            Block cropBlock = world.GetBlock(CodeWithPath("crops-" + inSlot.Itemstack.Collectible.LastCodePart() + "-1"));
            if (cropBlock == null || cropBlock.CropProps == null) return;

            dsc.AppendLine(Lang.Get("soil-nutrition-requirement") + cropBlock.CropProps.RequiredNutrient);
            dsc.AppendLine(Lang.Get("soil-nutrition-consumption") + cropBlock.CropProps.NutrientConsumption);
            dsc.AppendLine(Lang.Get("soil-growth-time") + Math.Round(cropBlock.CropProps.TotalGrowthDays, 1) + " days");
        }
    }
    
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
