using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace VSHUD
{
    class GuiDialogClock : HudElement
    {
        ClockShowConfig Config { get => ConfigLoader.Config.ClockShowConfig; }

        long id;

        public GuiDialogClock(ICoreClientAPI capi) : base(capi)
        {
            this.capi = capi;
        }

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();
            lastCol = Config.ClockColor;

            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 5, 5, 400, 256);

            double[] stroke = new double[] { 0, 0, 0, 1 };

            SingleComposer = capi.Gui.CreateCompo("clock", textBounds)
                .AddDynamicText("", CairoFont.WhiteSmallText().WithStroke(stroke, 2).WithOrientation(EnumTextOrientation.Justify), textBounds.ForkChild(), "clock")
                .Compose();

            id = capi.World.RegisterGameTickListener(dt => UpdateText(), 100);

        }

        int lastCol;

        public void UpdateText()
        {
            if (lastCol != Config.ClockColor)
            {
                SingleComposer.GetDynamicText("clock").Font.Color = ColorUtil.ToRGBADoubles(Config.ClockColor);
                SingleComposer.ReCompose();
            }
            lastCol = Config.ClockColor;

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
            
            int h = Config.TimeType == EnumTimeType.TwelveHour ? (cal.FullHourOfDay > 12 ? cal.FullHourOfDay - 12 : cal.FullHourOfDay) : cal.FullHourOfDay;
            if (Config.TimeType == EnumTimeType.TwelveHour) h = h == 0 ? 12 : h;

            string hour = h < 10 ? "0" + h : "" + h;
            int m = (int)(60 * (cal.HourOfDay - cal.FullHourOfDay));
            string dot = m % 2 == 0 ? ":" : " ";
            string minute = m < 10 ? "0" + m : "" + m;
            string ampm = Config.TimeType == EnumTimeType.TwelveHour ? (cal.FullHourOfDay < 12 ? "AM" : "PM") : "";
            string time = string.Format("{0}{1}{2} {3}", hour, dot, minute, ampm);

            StringBuilder stringBuilder = new StringBuilder();
            
            if (Config.Calendar) stringBuilder.AppendLine(string.Format("Date: {0} {1}, {2}, {3}", cal.MonthName, cal.DayOfMonth.DisplayWithSuffix(), cal.Year, time));
            if (Config.Season) stringBuilder.AppendLine(string.Format("Season: {0}", cal.GetSeason(entityPos)));
            if (Config.Temperature) stringBuilder.AppendLine(string.Format("Temperature: {0}°C", Math.Round(climate.Temperature)));
            if (Config.Rainfall) stringBuilder.AppendLine(string.Format("Rainfall: {0}%, Fertility: {1}%", Math.Round(climate.Rainfall, 3) * 100, Math.Round(climate.Fertility, 3) * 100));
            if (Config.WindVelocity) stringBuilder.AppendLine(string.Format("Wind Velocity: {0}", GlobalConstants.CurrentWindSpeedClient.Sanitize()));
            if (Config.LocalTemporalStability) stringBuilder.AppendLine(string.Format("Local Temporal Stability: {0}%", Math.Round(capi.ModLoader.GetModSystem<SystemTemporalStability>().GetTemporalStability(entityPos) * 100, 3)));
            if (Config.PlayerTemporalStability) stringBuilder.AppendLine(string.Format("Player Temporal Stability: {0}%", Math.Round(stability * 100, 3)));
            if (Config.TemporalStormInfo)
            {
                double daysOrHours = Math.Round(nextStormDays > 1 ? nextStormDays : nextStormDays * 24, 2);
                string TTL = daysOrHours < 0 ? "∞" : daysOrHours.ToString("F2");

                stringBuilder.AppendLine(nowStormActive ? string.Format("Magnitude {0} Temporal Storm Ends In {1} Hours.", Math.Round(stormGlitchStrength * 10, 1), Math.Round(stormActiveDays * 24, 2)) :
                string.Format("{0} Temporal Storm In {1} {2}.", nextStormStrength, TTL, nextStormDays > 1 ? "Days" : "Hours"));
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
