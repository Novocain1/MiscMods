﻿using HarmonyLib;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSHUD
{
    [HarmonyPatch(typeof(Block), "GetPlacedBlockInfo")]
    class ATLCharcoalPit
    {
        public static void Postfix(IWorldAccessor world, BlockPos pos, ref string __result)
        {
            if (world.Side == EnumAppSide.Client)
            {
                var be = world.BlockAccessor.GetBlockEntity(pos.DownCopy());
                if (be is BlockEntityCharcoalPit)
                {
                    StringBuilder sb = new StringBuilder(__result);
                    if (be.GetField<int>("state") > 0)
                    {
                        double timeLeft = be.GetField<double>("finishedAfterTotalHours") - world.Calendar.TotalHours;
                        sb.AppendLine(string.Format("Process Completed In {0} Hours", Math.Round(timeLeft, 2)));
                        __result = sb.ToString().TrimEnd();
                    }
                    else
                    {
                        double timeLeft = be.GetField<double>("startingAfterTotalHours") - world.Calendar.TotalHours;
                        sb.AppendLine(string.Format("Warming Up Finished In {0} Hours", Math.Round(timeLeft, 2)));
                        __result = sb.ToString().TrimEnd();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityPitKiln), "GetBlockInfo")]
    class ATLBEPitkiln
    {
        public static void Postfix(BlockEntityPitKiln __instance, StringBuilder dsc)
        {
            var api = __instance.Api;

            if (__instance.Lit)
            {
                double timeLeft = __instance.BurningUntilTotalHours - api.World.Calendar.TotalHours;
                dsc.AppendLine(string.Format("Process Completed In {0} Hours", Math.Round(timeLeft, 2)));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityBloomery), "GetBlockInfo")]
    class ATLBEBloomery
    {
        public static void Postfix(BlockEntityBloomery __instance, StringBuilder dsc)
        {
            var api = __instance.Api;

            if (__instance.GetField<bool>("burning"))
            {
                double timeLeft = (__instance.GetField<double>("burningUntilTotalDays") - api.World.Calendar.TotalDays) * 24.0;
                dsc.AppendLine(string.Format("Process Completed In {0} Hours", Math.Round(timeLeft, 2)));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityOpenableContainer), "GetBlockInfo")]
    class ATLBEQuern
    {
        public static void Postfix(BlockEntityOpenableContainer __instance, StringBuilder dsc)
        {
            if (__instance is BlockEntityQuern quern)
            {
                if (quern.CanGrind() && quern.GrindSpeed > 0)
                {
                    double percent = quern.inputGrindTime / quern.maxGrindingTime();
                    int mss = quern.InputSlot?.Itemstack?.Item?.MaxStackSize ?? 1;
                    double stackSize = (double)(quern.InputSlot?.Itemstack?.StackSize ?? 0) / mss;
                    stackSize = 1.0 - stackSize;
                    stackSize += percent / mss;
                    stackSize *= 100;
                    percent *= 100;

                    dsc.AppendLine(string.Format("Stack {0}%", Math.Round(stackSize, 2)));
                    dsc.AppendLine(string.Format("Item {0}%", Math.Round(percent, 2)));
                }
            }

        }
    }
}
