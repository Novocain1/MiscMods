using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System;
using Vintagestory.API.Util;
using System.Collections.Generic;

namespace VSHUD
{
    [HarmonyPatch(typeof(WaypointMapLayer), "OnDataFromServer")]
    class TriggerRepopulation
    {
        public static void Postfix(WaypointMapLayer __instance, byte[] data)
        {
            string currentHash = FloatyWaypointManagement.GetWaypointsHash();

            if (currentHash != null)
            {
                var incomingWaypoints = SerializerUtil.Deserialize<List<Waypoint>>(data);
                string str = "";

                for (int i = 0; i < incomingWaypoints.Count; i++)
                {
                    str += incomingWaypoints[i].Title;
                    str += i;
                }

                string incomingHash = GameMath.Md5Hash(str);

                if (currentHash == incomingHash)
                {
                    return;
                }
            }

            FloatyWaypointManagement.TriggerRepopulation();
        }
    }
}
