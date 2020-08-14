using Vintagestory.API.Client;
using Vintagestory.API.Common;

[assembly: ModInfo("VSHUD",
    Description = "Automatically creates waypoints on player death, floaty waypoints, and other misc client side things",
    Side = "Client",
    Authors = new[] { "Novocain" },
    Version = "1.5.8")]

namespace VSHUD
{
    class VSHUDMain : VSHUDClientSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () => api.Shader.ReloadShaders();
        }
    }
}
