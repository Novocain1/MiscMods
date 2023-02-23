﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    public enum EnumTimeType
    {
        TwelveHour, TwentyFourHour
    }

    public enum EnumBakeDataFlags
    {
        None       = 0b000,
        BlockLight = 0b001,
        SunLight   = 0b010,
        Glow       = 0b100,
        All        = 0b111,
    }

    public class ClockShowConfig
    {
        public bool Calendar { get; set; } = true;
        public bool Season { get; set; } = true;
        public bool Temperature { get; set; } = true;
        public bool Rainfall { get; set; } = true;
        public bool WindVelocity { get; set; } = true;
        public bool LocalTemporalStability { get; set; } = true;
        public bool PlayerTemporalStability { get; set; } = true;
        public bool TemporalStormInfo { get; set; } = true;
        public EnumTimeType TimeType { get; set; } = EnumTimeType.TwentyFourHour;

        public Vec2f ClockPosMod { get; set; } = new Vec2f();
        public int ClockColor { get; set; } = ColorUtil.WhiteArgb;
    }

    public class PlacementPreviewConfig
    {
        public Dictionary<string, string[]> KnownBrokenByVersion { get; set; } = new Dictionary<string, string[]>();
    }

    public class VSHUDConfig
    {
        public double DotRange { get; set; } = 2000.0;
        public double TitleRange { get; set; } = 500.0;
        public bool PerBlockWaypoints { get; set; } = false;
        public int SetColorIndex { get; set; } = 0;
        public bool WaypointPrefix { get; set; } = true;
        public bool WaypointID { get; set; } = true;
        public bool FloatyWaypoints { get; set; } = true;
        public bool DebugDeathWaypoints { get; set; } = false;
        public bool ShowPillars { get; set; } = true;
        public bool Echo { get; set; } = false;

        public bool LightLevels { get; set; } = false;
        public EnumLightLevelType LightLevelType { get; set; } = EnumLightLevelType.OnlyBlockLight;
        public int LightRadius { get; set; } = 8;

        internal int LightVolume { get => (int)Math.Round(4.0 / 3.0 * Math.PI * LightRadius * LightRadius * LightRadius); }

        public int MinLLID { get; set; } = 128;
        public float LightLevelAlpha { get; set; } = 0.8f;
        public int LightLevelRed { get; set; } = 8;
        public bool Nutrients { get; set; } = false;
        public bool MXNutrients { get; set; } = true;

        public bool LUShowAbove { get; set; } = true;
        public bool LUSpawning { get; set; } = true;
        public bool LUOpaque { get; set; } = true;
        public bool CreateChunkObjs { get; set; } = false;

        public PlacementPreviewConfig PlacementPreviewConfig { get; set; } = new PlacementPreviewConfig();
        public bool PRShow { get; set; } = true;
        public bool PRTint { get; set; } = false;
        public float[] PRTintColor { get; set; } = new float[] { 0, 0, 3 };
        public float PROpacity { get; set; } = 0.8f;
        public bool PRDrawLines { get; set; } = true;

        public bool FDShow { get; set; } = true;
        public float FDRange { get; set; } = 100.0f;

        //write custom data like vs vertex flags as undefined lines in exported objs
        public bool MEWriteCustomData { get; set; } = false;

        public EnumBakeDataFlags ExpMeshFlags { get; set; } = EnumBakeDataFlags.BlockLight | EnumBakeDataFlags.Glow;

        public ClockShowConfig ClockShowConfig { get; set; } = new ClockShowConfig();

        public List<int> DisabledColors { get; set; } = new List<int>();
    }

    class ConfigLoader : ClientModSystem
    {
        ICoreClientAPI capi;
        public static VSHUDConfig Config { get; set; } = new VSHUDConfig();
        public override double ExecuteOrder() => 0.05;

        private static bool allowSaving = true;


        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            allowSaving = LoadConfig();

            api.RegisterCommand("vshudforcesave", "Force saves vshud user settings with the current settings state.", "", (a, b) => SaveConfig(true));
        }

        public bool LoadConfig() => LoadConfig(capi);
        public void SaveConfig(bool force = false) => SaveConfig(capi, force);

        public static bool LoadConfig(ICoreClientAPI capi)
        {
            try
            {
                if ((capi.LoadModConfig<VSHUDConfig>("vshud.json") ?? capi.LoadModConfig<VSHUDConfig>("waypointutils.json")) == null) { 
                    SaveConfig(capi);
                    SetPlacementPreviewBlock.Initialize();
                    return true; 
                }

                Config = capi.LoadModConfig<VSHUDConfig>("vshud.json") ?? capi.LoadModConfig<VSHUDConfig>("waypointutils.json");
                SaveConfig(capi);
                SetPlacementPreviewBlock.Initialize();
                return true;
            }
            catch (Exception)
            {
                capi.World.Logger.Notification("Error while parsing VSHUD configuration file, will use fallback settings. All changes to configuration in game will not be saved!");
                capi.World.Logger.Notification("Use .vshudforcesave to fix.");
                return false;
            }
        }

        public static void SaveConfig(ICoreClientAPI capi, bool force = false)
        {
            if (allowSaving || force)
            {
                SetPlacementPreviewBlock.Save();

                capi.StoreModConfig(Config, "vshud.json");
                allowSaving = true;
            }
        }
    }
}
