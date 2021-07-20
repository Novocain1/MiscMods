using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSHUD
{
    public class VSHUDCommand : ClientChatCommandExt
    {
        public static VSHUDConfig Config { get => ConfigLoader.Config; }
        public override void CallHandler(IPlayer player, int groupId, Vintagestory.API.Common.CmdArgs args)
        {
            WaypointUtils.doingConfigAction = true;

            base.CallHandler(player, groupId, args);

            WaypointUtils.doingConfigAction = false;
            capi.SendMyWaypoints();
            ConfigLoader.SaveConfig(capi);
        }

        public VSHUDCommand(ICoreClientAPI capi) : base(capi)
        {
        }
    }
}
