﻿using System;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

[assembly: ModInfo("VSHUD",
    Description = "Automatically creates waypoints on player death, floaty waypoints, and other misc client side things",
    Side = "Client",
    Authors = new[] { "Novocain" },
    IconPath = "creative/textures/block/command01-inside.png",
    Version = "2.2.0")]

namespace VSHUD
{
    public class VSHUDMain : ClientModSystem
    {
        ICoreClientAPI capi;

        public MassFileExportSystem fileExport;
        public VSHUDTaskSystem taskSystem;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            api.Event.LevelFinalize += () =>
            {
                capi.Shader.ReloadShaders();
                capi.InjectClientThread("File Export", 1000, fileExport = new MassFileExportSystem(capi.World as ClientMain));
                capi.InjectClientThread("VSHUD Tasks", 30, taskSystem = new VSHUDTaskSystem(capi.World as ClientMain));
            };
        }

        public override void Dispose()
        {
            CheckAppSideAnywhere.Dispose();
        }
    }
}
