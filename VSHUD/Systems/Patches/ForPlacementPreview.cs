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
    [HarmonyPatch(typeof(Block), "OnHeldIdle")]
    public class NewPlacementPreview
    {
        public static void Postfix(Block __instance, ItemSlot slot, EntityAgent byEntity)
        {
            var player = (byEntity as EntityPlayer).Player;
            if (byEntity.World.Side.IsClient())
            {
                if ((byEntity.World as ClientMain).ElapsedMilliseconds % 4 == 0)
                {
                    if (player?.CurrentBlockSelection != null && slot?.Itemstack != null)
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

                        bool works = __instance.TryPlaceBlock(byEntity.World, player, slot.Itemstack, blockSel, ref fail);

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

            SetBlockRedirect.blockId = setBlock ? 0 : blockId;

            xyz[0] = pos.X;
            xyz[1] = pos.Y;
            xyz[2] = pos.Z;
        }
    }

    [HarmonyPatch(typeof(BlockAngledGears))]
    public class FixAngledGears
    {
        [HarmonyPatch("TryPlaceBlock")]
        [HarmonyPrefix]
        public static bool EnableOriginal() => CheckAppSideAnywhere.Side == EnumAppSide.Server;

        [HarmonyPatch("TryPlaceBlock")]
        [HarmonyPostfix]
        public static void TryPlaceBlock(BlockAngledGears __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode, ref bool __result)
        {
            if (CheckAppSideAnywhere.Side == EnumAppSide.Server) return;

            __result = Fix(__instance, world, byPlayer, blockSel, ref failureCode);
        }

        public static bool Fix(BlockAngledGears bAg, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            Block blockExisting = world.BlockAccessor.GetBlock(blockSel.Position);
            if (!bAg.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode, blockExisting))
            {
                return false;
            }

            BlockFacing firstFace = null;
            BlockFacing secondFace = null;
            BlockMPMultiblockGear largeGearEdge = blockExisting as BlockMPMultiblockGear;
            bool validLargeGear = false;
            if (largeGearEdge != null)
            {
                BEMPMultiblock be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMPMultiblock;
                if (be != null) validLargeGear = be.Principal != null;
            }

            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                if (validLargeGear && (face == BlockFacing.UP || face == BlockFacing.DOWN)) continue;
                BlockPos pos = blockSel.Position.AddCopy(face);
                IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
                if (block != null && block.HasMechPowerConnectorAt(world, pos, face.Opposite))
                {
                    if (firstFace == null)
                    {
                        firstFace = face;
                    }
                    else
                    {
                        if (face.IsAdjacent(firstFace))
                        {
                            secondFace = face;
                            break;
                        }
                    }
                }
            }

            if (firstFace != null)
            {
                BlockPos firstPos = blockSel.Position.AddCopy(firstFace);
                BlockEntity be = world.BlockAccessor.GetBlockEntity(firstPos);
                IMechanicalPowerBlock neighbour = be?.Block as IMechanicalPowerBlock;

                BEBehaviorMPAxle bempaxle = be?.GetBehavior<BEBehaviorMPAxle>();
                if (bempaxle != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, neighbour as Block, firstPos))
                {
                    failureCode = "axlemusthavesupport";
                    return false;
                }

                BlockEntity largeGearBE = validLargeGear ? largeGearEdge.GearPlaced(world, blockSel.Position) : null;

                Block toPlaceBlock = bAg.getGearBlock(world, validLargeGear, firstFace, secondFace);
                //world.BlockAccessor.RemoveBlockEntity(blockSel.Position);  //## needed in 1.12, but not with new chunk BlockEntity Dictionary in 1.13
                world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

                if (secondFace != null)
                {
                    BlockPos secondPos = blockSel.Position.AddCopy(secondFace);
                    IMechanicalPowerBlock neighbour2 = world.BlockAccessor.GetBlock(secondPos) as IMechanicalPowerBlock;
                    neighbour2?.DidConnectAt(world, secondPos, secondFace.Opposite);
                }

                BEBehaviorMPAngledGears beAngledGear = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorMPAngledGears>();
                if (largeGearBE?.GetBehavior<BEBehaviorMPBase>() is BEBehaviorMPLargeGear3m largeGear) beAngledGear.AddToLargeGearNetwork(largeGear, firstFace);

                //do this last even for the first face so that both neighbours are correctly set
                neighbour?.DidConnectAt(world, firstPos, firstFace.Opposite);
                if (beAngledGear != null)
                {
                    beAngledGear.newlyPlaced = true;
                    if (!beAngledGear.tryConnect(firstFace) && secondFace != null) beAngledGear.tryConnect(secondFace);
                    beAngledGear.newlyPlaced = false;

                }

                return true;
            }

            failureCode = "requiresaxle";

            return false;
        }
    }
}
