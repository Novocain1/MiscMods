using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace WaypointUtils
{
    class ClockDialogSystem : ModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            GuiDialogClock clock = new GuiDialogClock(api);

            api.Input.RegisterHotKey("clock", "Open Clock GUI", GlKeys.ControlRight, HotkeyType.GUIOrOtherControls);

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
            GameCalendar cal = (GameCalendar)capi.World.Calendar;

            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 5, -250, 500, 500);
            BlockPos entityPos = capi.World.Player.Entity.Pos.AsBlockPos;
            ClimateCondition climate = capi.World.BlockAccessor.GetClimateAt(entityPos);

            string hour = cal.FullHourOfDay < 10 ? "0" + cal.FullHourOfDay : "" + cal.FullHourOfDay;
            int m = (int)(60 * (cal.HourOfDay - cal.FullHourOfDay));
            string minute = m < 10 ? "0" + m : "" + m;
            StringBuilder stringBuilder = new StringBuilder();
            
            stringBuilder.AppendLine("Date: " + cal.DayOfYear + "/" + cal.DaysPerYear + ", " + cal.Year)
                .AppendLine("Time: " + hour + ":" + minute)
                .AppendLine("Global Season: " + cal.Season)
                .AppendLine("Local Temperature: " + Math.Round(climate.Temperature, 3))
                .AppendLine("Local Rainfall: " + Math.Round(climate.Rainfall, 3));

            double[] stroke = new double[] { 0, 0, 0, 1 };

            SingleComposer = capi.Gui.CreateCompo("clock", textBounds)
                .AddDynamicText(stringBuilder.ToString(), CairoFont.WhiteSmallText().WithStroke(stroke, 2), EnumTextOrientation.Justify, textBounds, "clock")
                .Compose();

            bool dot = false;

            id = capi.World.RegisterGameTickListener(dt => 
            {
                entityPos = capi.World.Player.Entity.Pos.AsBlockPos;
                climate = capi.World.BlockAccessor.GetClimateAt(entityPos);

                cal = (GameCalendar)capi.World.Calendar;

                dot = !dot;
                string d = dot ? ":" : " ";

                hour = cal.FullHourOfDay < 10 ? "0" + cal.FullHourOfDay : "" + cal.FullHourOfDay;
                m = (int)(60 * (cal.HourOfDay - cal.FullHourOfDay));
                minute = m < 10 ? "0" + m : "" + m;

                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Date: " + cal.DayOfYear + "/" + cal.DaysPerYear + ", " + cal.Year)
                    .AppendLine("Time: " + hour + ":" + minute)
                    .AppendLine("Global Season: " + cal.Season)
                    .AppendLine("Local Temperature: " + Math.Round(climate.Temperature, 3))
                    .AppendLine("Local Rainfall: " + Math.Round(climate.Rainfall, 3));

                SingleComposer.GetDynamicText("clock").SetNewText(stringBuilder.ToString());
            }, 1000);
        }

        public override bool TryOpen()
        {
            if (base.TryOpen())
            {
                OnOwnPlayerDataReceived();
                return true;
            }
            return false;
        }

        public override bool TryClose()
        {
            if (base.TryClose())
            {
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
