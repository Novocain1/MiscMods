using HarmonyLib;
using System.Linq;
using Vintagestory.GameContent;

namespace VSHUD
{
    [HarmonyPatch(typeof(GuiDialogEditWayPoint), "onSave")]
    class MarkDirtyFix
    {
        public static void Postfix(GuiDialogEditWayPoint __instance)
        {
            int wp = __instance.GetField<int>("wpIndex");
            FloatyWaypointManagement.WaypointElements.Where(a => a.waypointID == wp).Single().MarkDirty();
        }
    }
}
