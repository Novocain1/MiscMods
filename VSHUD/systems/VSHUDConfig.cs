using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    class ClockShowConfig
    {
        public bool Calendar { get; set; } = true;
        public bool Season { get; set; } = true;
        public bool Temperature { get; set; } = true;
        public bool Rainfall { get; set; } = true;
        public bool WindVelocity { get; set; } = true;
        public bool LocalTemporalStability { get; set; } = true;
        public bool PlayerTemporalStability { get; set; } = true;
        public bool TemporalStormInfo { get; set; } = true;

        public Vec2f ClockPosMod { get; set; } = new Vec2f();
    }

    class VSHUDConfig
    {
        public double DotRange { get; set; } = 2000.0;
        public double TitleRange { get; set; } = 500.0;
        public bool PerBlockWaypoints { get; set; } = false;
        public int SetColorIndex { get; set; } = 0;
        public bool WaypointPrefix { get; set; } = true;
        public bool WaypointID { get; set; } = true;
        public bool FloatyWaypoints { get; set; } = true;
        public bool DebugDeathWaypoints { get; set; } = false;

        public bool LightLevels { get; set; } = false;
        public EnumLightLevelType LightLevelType { get; set; } = EnumLightLevelType.OnlyBlockLight;
        public int LightRadius { get; set; } = 8;
        public int MinLLID { get; set; } = 128;
        public float LightLevelAlpha { get; set; } = 0.8f;
        public int LightLevelRed { get; set; } = 8;
        public bool Nutrients { get; set; } = false;
        public bool MXNutrients { get; set; } = true;

        public bool LUShowAbove { get; set; } = true;
        public bool LUSpawning { get; set; } = true;
        public bool LUOpaque { get; set; } = true;
        public bool CreateChunkObjs { get; set; } = false;

        public bool PRShow { get; set; } = true;
        public bool PRTint { get; set; } = false;
        public float[] PRTintColor { get; set; } = new float[] { 0, 0, 3 };
        public float PROpacity { get; set; } = 0.8f;

        public ClockShowConfig ClockShowConfig { get; set; } = new ClockShowConfig();

        public List<int> DisabledColors { get; set; } = new List<int>();
    }

    class ConfigLoader : ClientModSystem
    {
        ICoreClientAPI capi;
        public static VSHUDConfig Config { get; set; } = new VSHUDConfig();
        public override double ExecuteOrder() => 0.05;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            LoadConfig();
            ChunkObjCreator.Seed = api.World.Seed;
            var spawnChunk = api.World.DefaultSpawnPosition.AsBlockPos.GetChunkPos(api.World.BlockAccessor);
            ChunkObjCreator.SpawnPos = spawnChunk;
        }

        public void LoadConfig() => LoadConfig(capi);
        public void SaveConfig() => SaveConfig(capi);

        public static void LoadConfig(ICoreClientAPI capi)
        {
            if ((capi.LoadModConfig<VSHUDConfig>("vshud.json") ?? capi.LoadModConfig<VSHUDConfig>("waypointutils.json")) == null) { SaveConfig(capi); return; }

            Config = capi.LoadModConfig<VSHUDConfig>("vshud.json") ?? capi.LoadModConfig<VSHUDConfig>("waypointutils.json");
            SaveConfig(capi);
        }

        public static void SaveConfig(ICoreClientAPI capi) => capi.StoreModConfig(Config, "vshud.json");
    }
}
