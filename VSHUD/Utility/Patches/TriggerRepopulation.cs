using HarmonyLib;
using Vintagestory.GameContent;

namespace VSHUD
{
    [HarmonyPatch(typeof(WaypointMapLayer), "OnDataFromServer")]
    class TriggerRepopulation
    {
        public static void Postfix()
        {
            FloatyWaypointManagement.TriggerRepopulation();
        }
    }
}
