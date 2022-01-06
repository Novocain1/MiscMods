using System;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

[assembly: ModInfo("VSHUD",
    Description = "Automatically creates waypoints on player death, floaty waypoints, and other misc client side things",
    Side = "Client",
    Authors = new[] { "Novocain" },
    Version = "2.0.27")]

namespace VSHUD
{
    class VSHUDMain : ClientModSystem
    {
        ICoreClientAPI capi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            api.Event.LevelFinalize += () =>
            {
                capi.Shader.ReloadShaders();
                capi.InjectClientThread("File Export", 1000, new MassFileExportSystem(capi.World as ClientMain));
                capi.InjectClientThread("VSHUD Tasks", 30, new VSHUDTaskSystem(capi.World as ClientMain));
            };
        }

        public override void Dispose()
        {
            CheckAppSideAnywhere.Dispose();
        }
    }
}
