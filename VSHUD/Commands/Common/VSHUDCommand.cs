using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSHUD
{
    public class VSHUDCommand : ClientChatCommandExt
    {
        public override void CallHandler(IPlayer player, int groupId, Vintagestory.API.Common.CmdArgs args)
        {
            WaypointUtils.doingConfigAction = true;

            base.CallHandler(player, groupId, args);

            WaypointUtils.doingConfigAction = false;
            capi.SendMyWaypoints();
        }

        public VSHUDCommand(ICoreClientAPI capi) : base(capi)
        {
        }
    }
}
