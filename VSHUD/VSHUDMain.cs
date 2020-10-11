using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

[assembly: ModInfo("VSHUD",
    Description = "Automatically creates waypoints on player death, floaty waypoints, and other misc client side things",
    Side = "Client",
    Authors = new[] { "Novocain" },
    Version = "2.0.0")]

namespace VSHUD
{
    class VSHUDMain : ClientModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
                api.InjectClientThread("File Export", 30, new MassFileExportSystem(api.World as ClientMain));
                api.InjectClientThread("VSHUD Tasks", 30, new VSHUDTaskSystem(api.World as ClientMain));
            };
        }
    }
}
