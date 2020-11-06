using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

[assembly: ModInfo("VSHUD",
    Description = "Automatically creates waypoints on player death, floaty waypoints, and other misc client side things",
    Side = "Client",
    Authors = new[] { "Novocain" },
    Version = "2.0.0-pre.8")]

namespace VSHUD
{
    class VSHUDMain : ClientModSystem
    {
        ICoreClientAPI capi;
        public ClientSystem[] systems = new ClientSystem[64];

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
                api.InjectClientThread("File Export", 30, systems[0] = new MassFileExportSystem(api.World as ClientMain));
                api.InjectClientThread("VSHUD Tasks", 30, systems[1] = new VSHUDTaskSystem(api.World as ClientMain));
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var val in systems.Where((a) => a != null))
            {
                val.Dispose(capi.World as ClientMain);
            }
        }
    }
}
