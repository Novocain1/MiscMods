using Vintagestory.Client.NoObf;
using HarmonyLib;
using System.Linq;
using Vintagestory.API.Common;

namespace VSHUD
{
    [HarmonyPatch(typeof(ClientEventManager), "TriggerNewServerChatLine")]
    class PreventWaypointChatLogging
    {
        public static readonly string[] WaypointRelated = new string[]
        {
            "Ok, waypoint nr.",
            "Ok, deleted waypoint."
        };

        public static bool Prefix(string message, EnumChatType chattype)
        {
            bool contains = false;
            if (!ConfigLoader.Config.Echo && chattype == EnumChatType.CommandSuccess)
            {
                foreach (var text in WaypointRelated)
                {
                    contains |= message.Contains(text);
                    if (contains)
                    {
                        break;
                    }
                }
            }
            return !contains;
        }
    }
}
