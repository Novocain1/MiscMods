using HarmonyLib;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace VSHUD
{
    [HarmonyPatch(typeof(BlockEntityPitKiln), "GetBlockInfo")]
    class ATLBEPitkiln
    {
        public static void Postfix(BlockEntityPitKiln __instance, StringBuilder dsc)
        {
            var api = __instance.Api;

            if (__instance.Lit)
            {
                double timeLeft = __instance.BurningUntilTotalHours - api.World.Calendar.TotalHours;
                dsc.AppendLine(string.Format("Process Completed In: {0} Hours", Math.Round(timeLeft, 2)));
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
                dsc.AppendLine(string.Format("Process Completed In: {0} Hours", Math.Round(timeLeft, 2)));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityOpenableContainer), "GetBlockInfo")]
    class ATLBEQuern
    {
        public static void Postfix(BlockEntityOpenableContainer __instance, StringBuilder dsc)
        {
            if (__instance is BlockEntityQuern)
            {
                var quern = (BlockEntityQuern)__instance;

                if (quern.CanGrind() && quern.GrindSpeed > 0)
                {
                    double timeLeft = (quern.maxGrindingTime() - quern.inputGrindTime) * 2.0;
                    dsc.AppendLine(string.Format("Process Completed In: {0} Game Minutes", Math.Round(timeLeft, 2)));
                }
            }

        }
    }
}
