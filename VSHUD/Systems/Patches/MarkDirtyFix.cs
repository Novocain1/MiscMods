using HarmonyLib;
using Vintagestory.GameContent;

namespace VSHUD
{
    [HarmonyPatch(typeof(GuiDialogEditWayPoint), "onSave")]
    class MarkDirtyFix
    {
        public static void Postfix(GuiDialogEditWayPoint __instance)
        {
            int wp = __instance.GetField<int>("wpIndex");
            lock (FloatyWaypointManagement.WaypointElements)
            {
                foreach(var val in FloatyWaypointManagement.WaypointElements)
                {
                    if (wp == val.waypointID)
                    {
                        val.MarkDirty();
                    }
                }
            }
        }
    }
}
