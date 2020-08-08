using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

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

            api.RegisterCommand("clockconfig", "Configures VSHUD Clock", "[Calendar|Season|Temperature|Rainfall|WindVelocity|LocalTemporalStability|PlayerTemporalStability|TemporalStormInfo] true/false", (id, args) => 
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

    class GuiDialogClock : HudElement
    {
        ClockShowConfig Config { get => capi.ModLoader.GetModSystem<ConfigLoader>().Config.ClockShowConfig; }

        long id;

        public GuiDialogClock(ICoreClientAPI capi) : base(capi)
        {
            this.capi = capi;
        }

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();

            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 5, 5, 400, 256);

            double[] stroke = new double[] { 0, 0, 0, 1 };

            SingleComposer = capi.Gui.CreateCompo("clock", textBounds)
                .AddDynamicText("", CairoFont.WhiteSmallText().WithStroke(stroke, 2), EnumTextOrientation.Justify, textBounds.ForkChild(), "clock")
                .Compose();

            id = capi.World.RegisterGameTickListener(dt => UpdateText(), 100);

        }

        public void UpdateText()
        {
            BlockPos entityPos = capi.World.Player.Entity.Pos.AsBlockPos;
            ClimateCondition climate = capi.World.BlockAccessor.GetClimateAt(entityPos, EnumGetClimateMode.NowValues);

            GameCalendar cal = (GameCalendar)capi.World.Calendar;
           
            float stability = (float)capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 0.0);
            var stabilitySystem = capi.ModLoader.GetModSystem<SystemTemporalStability>();
            var data = stabilitySystem.GetField<object>("data");

            double nextStormDays = data.GetField<double>("nextStormTotalDays") - capi.World.Calendar.TotalDays;
            EnumTempStormStrength nextStormStrength = data.GetField<EnumTempStormStrength>("nextStormStrength");
            float stormGlitchStrength = data.GetField<float>("stormGlitchStrength");
            double stormActiveDays = data.GetField<double>("stormActiveTotalDays") - capi.World.Calendar.TotalDays;
            bool nowStormActive = data.GetField<bool>("nowStormActive");

            string hour = cal.FullHourOfDay < 10 ? "0" + cal.FullHourOfDay : "" + cal.FullHourOfDay;
            int m = (int)(60 * (cal.HourOfDay - cal.FullHourOfDay));
            string dot = m % 2 == 0 ? ":" : " ";
            string minute = m < 10 ? "0" + m : "" + m;
            string time = hour + dot + minute;

            StringBuilder stringBuilder = new StringBuilder();
            
            if (Config.Calendar) stringBuilder.AppendLine(string.Format("Date: {0} {1}, {2}, {3}", cal.MonthName, cal.DayOfMonth.DisplayWithSuffix(), cal.Year, time));
            if (Config.Season) stringBuilder.AppendLine(string.Format("Season: {0}", cal.GetSeason(entityPos)));
            if (Config.Temperature) stringBuilder.AppendLine(string.Format("Temperature: {0}°C", Math.Round(climate.Temperature)));
            if (Config.Rainfall) stringBuilder.AppendLine(string.Format("Rainfall: {0}%, Fertility: {1}%", Math.Round(climate.Rainfall, 3) * 100, Math.Round(climate.Fertility, 3) * 100));
            if (Config.WindVelocity) stringBuilder.AppendLine(string.Format("Wind Velocity: {0}", GlobalConstants.CurrentWindSpeedClient.Sanitize()));
            if (Config.LocalTemporalStability) stringBuilder.AppendLine(string.Format("Temporal Stability: {0}", Math.Round(capi.ModLoader.GetModSystem<SystemTemporalStability>().GetTemporalStability(entityPos), 3)));
            if (Config.PlayerTemporalStability) stringBuilder.AppendLine(string.Format("Player Temporal Stability: {0}%", Math.Round(stability, 3) * 100));
            if (Config.TemporalStormInfo)
            {
                stringBuilder.AppendLine(nowStormActive ? string.Format("Magnitude {0} Temporal Storm Ends In {1} Hours.", Math.Round(stormGlitchStrength * 10, 1), Math.Round(stormActiveDays * 24, 2)) :
                string.Format("{0} Temporal Storm In {1} {2}.", nextStormStrength, Math.Round(nextStormDays > 1 ? nextStormDays : nextStormDays * 24, 2), nextStormDays > 1 ? "Days" : "Hours"));
            }
            
            SingleComposer.GetDynamicText("clock").SetNewText(stringBuilder.ToString());
            SingleComposer.GetDynamicText("clock").Bounds.absOffsetX = Config.ClockPosMod.X;
            SingleComposer.GetDynamicText("clock").Bounds.absOffsetY = Config.ClockPosMod.Y;
        }

        public override bool TryOpen()
        {
            if (base.TryOpen())
            {
                OnOwnPlayerDataReceived();
                capi.Settings.Bool["clockGui"] = true;
                return true;
            }
            return false;
        }

        public override bool TryClose()
        {
            if (base.TryClose())
            {
                capi.Settings.Bool["clockGui"] = false;
                Dispose();
                return true;
            }
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            capi.World.UnregisterGameTickListener(id);
        }

        public override string ToggleKeyCombinationCode => "clock";
    }
}
