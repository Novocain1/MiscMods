using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    class ClockDialogSystem : VSHUDClientSystem
    {
        private long id;
        ICoreClientAPI capi;

        ConfigLoader ConfigLoader { get => capi.ModLoader.GetModSystem<ConfigLoader>(); }
        ClockShowConfig Config { get => ConfigLoader.Config.ClockShowConfig; }

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

            api.RegisterCommand("clockconfig", "Configures VSHUD Clock", "[Calendar|Season|Temperature|Rainfall|WindVelocity|LocalTemporalStability|PlayerTemporalStability|TemporalStormInfo] true/false [Offset] x/y", (id, args) => 
            {
                string arg = args.PopWord().ToLowerInvariant();
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
                    default:
                        break;
                }
                ConfigLoader.SaveConfig();
            });
        }
    }
}
