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
        static readonly Type[] knownBroken = new Type[]
        {
            typeof(BlockAngledGears)
        };

        public static void Postfix(Block __instance, ItemSlot slot, EntityAgent byEntity)
        {
            var player = (byEntity as EntityPlayer).Player;
            if (byEntity.World.Side.IsClient())
            {
                if (player?.CurrentBlockSelection != null && slot?.Itemstack != null && !knownBroken.Contains(__instance.GetType()))
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

    [HarmonyPatch(typeof(BlockAccessorRelaxed), "SetBlock")]
    public class SetBlockRedirect
    {
        public static bool setBlock = true;
        public static int blockId;
        public static int[] xyz = new int[3];

        public static bool Prefix() => setBlock;

        public static void Postfix(ref int blockId, BlockPos pos)
        {
            SetBlockRedirect.blockId = blockId;

            xyz[0] = pos.X;
            xyz[1] = pos.Y;
            xyz[2] = pos.Z;
        }
    }
}
