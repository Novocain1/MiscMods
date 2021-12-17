using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.ServerMods;

namespace VSHUD
{
    [HarmonyPatch(typeof(CollectibleObject), "OnHeldIdle")]
    public class SetPlacementPreviewItem
    {
        public static void Postfix(CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity)
        {
            if (__instance.ItemClass == EnumItemClass.Item)
            {
                if (__instance is ItemStone)
                {
                    var loc0 = __instance.CodeWithPath("loosestones-" + __instance.LastCodePart() + "-free");
                    var loc1 = __instance.CodeWithPath("loosestones-" + __instance.LastCodePart(1) + "-" + __instance.LastCodePart(0) + "-free");

                    var block = byEntity.World.GetBlock(loc0);
                    block = block ?? byEntity.World.GetBlock(loc1);

                    if (block != null) block.OnHeldIdle(new DummySlot(new ItemStack(block)), byEntity);
                }
                else SetBlockRedirect.blockId = 0;
            }
        }
    }


    [HarmonyPatch(typeof(Block), "OnHeldIdle")]
    public class SetPlacementPreviewBlock
    {
        static HashSet<Type> KnownBroken = new HashSet<Type>();

        public static void Postfix(Block __instance, ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.World.Side.IsClient())
            {
                if (!(slot is DummySlot) && slot is ItemSlotOffhand) return;

                if ((byEntity.World as ClientMain).ElapsedMilliseconds % 4 == 0)
                {   
                    var player = (byEntity as EntityPlayer).Player;

                    if (!KnownBroken.Contains(__instance.GetType()) && player?.CurrentBlockSelection != null && slot?.Itemstack != null)
                    {
                        SetBlockRedirect.setBlock = false;

                        var blockSel = player.CurrentBlockSelection;
                        Block onBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                        BlockPos buildPos = blockSel.Position;

                        if (onBlock == null || !onBlock.IsReplacableBy(__instance))
                        {
                            buildPos = buildPos.Offset(blockSel.Face);
                            blockSel.DidOffset = true;
                        }

                        string fail = "";

                        bool works = false;

                        try
                        {
                            works = __instance.TryPlaceBlock(byEntity.World, player, slot.Itemstack, blockSel, ref fail);
                        }
                        catch (Exception)
                        {
                            KnownBroken.Add(__instance.GetType());
                        }

                        if (blockSel.DidOffset)
                        {
                            buildPos.Offset(blockSel.Face.Opposite);
                            blockSel.DidOffset = false;
                        }

                        if (!works) SetBlockRedirect.blockId = 0;
                        SetBlockRedirect.setBlock = true;
                    }
                    else SetBlockRedirect.blockId = 0;
                }
            }
        }
    }

    [HarmonyPatch(typeof(BlockAccessorBase), "SpawnBlockEntity")]
    public class SpawnBlockEntityHalt
    {
        public static bool Prefix() => SetBlockRedirect.ShouldNotSkipOriginal;
    }

    [HarmonyPatch(typeof(BlockAccessorRelaxed), "ExchangeBlock")]
    public class ExchangeBlockHalt
    {
        public static bool Prefix() => SetBlockRedirect.ShouldNotSkipOriginal;
    }

    [HarmonyPatch(typeof(WorldChunk), "SetDecor", new Type[] { typeof(IBlockAccessor), typeof(Block), typeof(BlockPos), typeof(BlockFacing) })]
    public class SetDecorHalt0
    {
        public static bool Prefix() => 
            SetBlockRedirect.ShouldNotSkipOriginal;

        public static void Postfix(ref bool __result)
        {
            if (SetBlockRedirect.ShouldNotSkipOriginal) return;

            __result = true;
        }
    }

    [HarmonyPatch(typeof(WorldChunk), "SetDecor", new Type[] { typeof(IBlockAccessor), typeof(Block), typeof(BlockPos), typeof(int) })]
    public class SetDecorHalt1
    {
        public static bool Prefix() => SetBlockRedirect.ShouldNotSkipOriginal;

        public static void Postfix(ref bool __result)
        {
            if (SetBlockRedirect.ShouldNotSkipOriginal) return;

            __result = true;
        }
    }

    [HarmonyPatch(typeof(BlockAccessorBase), "SetDecor", new Type[] { typeof(Block), typeof(BlockPos), typeof(BlockFacing) })]
    public class SetDecorRedirect0
    {
        public static bool Prefix() => SetBlockRedirect.ShouldNotSkipOriginal;

        public static void Postfix(ref bool __result, Block block, BlockPos position)
        {
            if (SetBlockRedirect.ShouldNotSkipOriginal) return;

            SetBlockRedirect.blockId = block.Id;

            SetBlockRedirect.xyz[0] = position.X;
            SetBlockRedirect.xyz[1] = position.Y;
            SetBlockRedirect.xyz[2] = position.Z;
            __result = true;
        }
    }

    [HarmonyPatch(typeof(BlockAccessorBase), "SetDecor", new Type[] { typeof(Block), typeof(BlockPos), typeof(int) })]
    public class SetDecorRedirect1
    {
        public static bool Prefix() => SetBlockRedirect.ShouldNotSkipOriginal;

        public static void Postfix(ref bool __result, Block block, BlockPos position)
        {
            SetDecorRedirect0.Postfix(ref __result, block, position);
        }
    }

    [HarmonyPatch(typeof(BlockAccessorRelaxed), "SetBlock")]
    public class SetBlockRedirect
    {   
        public static bool ShouldNotSkipOriginal { get => setBlock || CheckAppSideAnywhere.Side == EnumAppSide.Server; }

        public static bool setBlock = true;

        public static int blockId;
        public static int[] xyz = new int[3];

        public static bool Prefix() => ShouldNotSkipOriginal;

        public static void Postfix(ref int blockId, BlockPos pos)
        {
            if (ShouldNotSkipOriginal) return;

            SetBlockRedirect.blockId = blockId;

            xyz[0] = pos.X;
            xyz[1] = pos.Y;
            xyz[2] = pos.Z;
        }
    }
}
