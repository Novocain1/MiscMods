using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    class ClockDialogSystem : ClientModSystem
    {
        private long id;
        ICoreClientAPI capi;

        ConfigLoader ConfigLoader { get => capi.ModLoader.GetModSystem<ConfigLoader>(); }
        ClockShowConfig Config { get => ConfigLoader.Config.ClockShowConfig; }
        const string syntax = "[Calendar|Season|Temperature|Rainfall|WindVelocity|LocalTemporalStability|PlayerTemporalStability|TemporalStormInfo|TimeType|Clock] true/false [Offset] x/y hex color";

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            GuiDialogClock clock = new GuiDialogClock(api);

            api.Input.RegisterHotKey("clock", "Open Clock GUI", GlKeys.ControlRight, HotkeyType.GUIOrOtherControls);

            id = api.Event.RegisterGameTickListener(dt => 
            {
                if (api.World.Player?.Entity != null)
                {
                    if (api.Settings.Bool["clockGui"])
                    {
                        clock.TryOpen();
                    }

                    api.Event.UnregisterGameTickListener(id);
                }
                
            }, 1000);

            api.Input.SetHotKeyHandler("clock", a =>
            {
                clock.Toggle();
                return true;
            });
            
            api.RegisterCommand("clockconfig", "Configures VSHUD Clock", syntax, (id, args) => 
            {
                try
                {
                    string arg = args.PopWord()?.ToLowerInvariant();
                    switch (arg)
                    {
                        case "calendar":
                            Config.Calendar = args.PopBool() ?? !Config.Calendar;
                            break;
                        case "season":
                            Config.Season = args.PopBool() ?? !Config.Season;
                            break;
                        case "temperature":
                            Config.Temperature = args.PopBool() ?? !Config.Temperature;
                            break;
                        case "rainfall":
                            Config.Rainfall = args.PopBool() ?? !Config.Rainfall;
                            break;
                        case "windvelocity":
                            Config.WindVelocity = args.PopBool() ?? !Config.WindVelocity;
                            break;
                        case "localtemporalstability":
                            Config.LocalTemporalStability = args.PopBool() ?? !Config.LocalTemporalStability;
                            break;
                        case "playertemporalstability":
                            Config.PlayerTemporalStability = args.PopBool() ?? !Config.PlayerTemporalStability;
                            break;
                        case "temporalstorminfo":
                            Config.TemporalStormInfo = args.PopBool() ?? !Config.TemporalStormInfo;
                            break;
                        case "offset":
                            Config.ClockPosMod.X = args.PopFloat() ?? Config.ClockPosMod.X;
                            Config.ClockPosMod.Y = args.PopFloat() ?? Config.ClockPosMod.Y;
                            break;
                        case "timetype":
                            var a = args.PopWord();
                            Enum.TryParse(a, true, out EnumTimeType type);

                            Config.TimeType = type;
                            break;
                        case "color":
                            string hex = args.PopWord(ColorUtil.Int2Hex(ColorUtil.WhiteArgb));
                            if (hex.StartsWith("#") && hex.Length > 1)
                            {
                                int col = ColorUtil.Hex2Int(hex);
                                Config.ClockColor = col;
                            }
                            break;
                        default:
                            api.ShowChatMessage("Syntax: " + syntax);
                            break;
                    }
                    ConfigLoader.SaveConfig();
                }
                catch (Exception)
                {
                }
            });
        }
    }
}
