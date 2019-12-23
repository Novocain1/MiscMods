using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSHUD
{
    class WaypointUtilConfig
    {
        public double DotRange { get; set; } = 2000.0;
        public double TitleRange { get; set; } = 500.0;
        public bool PerBlockWaypoints { get; set; } = false;
        public int SetColorIndex { get; set; } = 0;
        public bool WaypointPrefix { get; set; } = true;
        public bool WaypointID { get; set; } = true;

        public bool LightLevels { get; set; } = false;
        public EnumLightLevelType LightLevelType { get; set; } = EnumLightLevelType.OnlyBlockLight;
        public int LightRadius { get; set; } = 8;
        public int MinLLID { get; set; } = 128;
        public float LightLevelAlpha { get; set; } = 0.8f;
        public int LightLevelRed { get; set; } = 8;

        public bool LUShowAbove { get; set; } = true;
        public bool LUSpawning { get; set; } = true;
        public bool LUOpaque { get; set; } = true;

        public bool PRShow { get; set; } = true;
        public bool PRTint { get; set; } = false;

        public List<int> DisabledColors { get; set; } = new List<int>();
    }

    class ConfigLoader : ModSystem
    {
        ICoreClientAPI capi;
        public WaypointUtilConfig Config { get; set; } = new WaypointUtilConfig();
        public override double ExecuteOrder() => 0.05;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            LoadConfig();
        }

        public void LoadConfig()
        {
            if (capi.LoadModConfig<WaypointUtilConfig>("waypointutils.json") == null) { SaveConfig(); return; }

            Config = capi.LoadModConfig<WaypointUtilConfig>("waypointutils.json");
            SaveConfig();
        }

        public void SaveConfig() => capi.StoreModConfig(Config, "waypointutils.json");
    }
}
