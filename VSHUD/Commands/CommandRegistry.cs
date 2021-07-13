using Vintagestory.API.Client;

namespace VSHUD
{
    public class CommandRegistry : ClientModSystem
    {
        public override double ExecuteOrder() => 1.0;

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.RegisterCommand(new CommandFloatyWaypoints(api, api.ModLoader.GetModSystem<WaypointUtils>()));
        }
    }
}
