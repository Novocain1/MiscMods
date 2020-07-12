using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace VSHUD
{
    class ClockDialogSystem : ModSystem
    {
        private long id;

        public override void StartClientSide(ICoreClientAPI api)
        {
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
        }
    }

    class GuiDialogClock : HudElement
    {
        long id;

        public GuiDialogClock(ICoreClientAPI capi) : base(capi)
        {
            this.capi = capi;
        }

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();

            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 5, 5, 300, 256);

            double[] stroke = new double[] { 0, 0, 0, 1 };

            SingleComposer = capi.Gui.CreateCompo("clock", textBounds)
                .AddDynamicText("", CairoFont.WhiteSmallText().WithStroke(stroke, 2), EnumTextOrientation.Justify, textBounds.ForkChild(), "clock")
                .Compose();

            id = capi.World.RegisterGameTickListener(dt => UpdateText(), 100);

        }

        public void UpdateText()
        {
            BlockPos entityPos = capi.World.Player.Entity.Pos.AsBlockPos;
            ClimateCondition climate = capi.World.BlockAccessor.GetClimateAt(entityPos);

            GameCalendar cal = (GameCalendar)capi.World.Calendar;
            float stability = (float)capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 0.0);

            string hour = cal.FullHourOfDay < 10 ? "0" + cal.FullHourOfDay : "" + cal.FullHourOfDay;
            int m = (int)(60 * (cal.HourOfDay - cal.FullHourOfDay));
            string dot = m % 2 == 0 ? ":" : " ";
            string minute = m < 10 ? "0" + m : "" + m;
            string time = hour + dot + minute;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(string.Format("Date: {0} {1}, {2}, {3}", cal.MonthName, cal.DayOfMonth.DisplayWithSuffix(), cal.Year, time))
                .AppendLine(string.Format("Season: {0}", cal.GetSeason(entityPos)))
                .AppendLine(string.Format("Temperature: {0}°C", Math.Round(climate.Temperature)))
                .AppendLine(string.Format("Rainfall: {0}%, Fertility: {1}%", Math.Round(climate.Rainfall, 3) * 100, Math.Round(climate.Fertility, 3) * 100))
                .AppendLine(string.Format("Wind Velocity: {0}", GlobalConstants.CurrentWindSpeedClient.Sanitize()))
                .AppendLine(string.Format("Temporal Stability: {0}", Math.Round(capi.ModLoader.GetModSystem<SystemTemporalStability>().GetTemporalStability(entityPos), 3)))
                .AppendLine(string.Format("Player Temporal Stability: {0}%", Math.Round(stability, 3) * 100));

            SingleComposer.GetDynamicText("clock").SetNewText(stringBuilder.ToString());
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
