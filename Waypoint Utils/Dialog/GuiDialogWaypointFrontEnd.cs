using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace VSHUD
{
    public class GuiDialogWaypointFrontEnd : GuiDialog
    {
        public GuiDialogWaypointFrontEnd(ICoreClientAPI capi) : base(capi)
        {
        }
        string wpText = "";
        string color;
        int intColor;
        WaypointUtilConfig config;

        public override string ToggleKeyCombinationCode => "waypointfrontend";
        public void VClose() => TryClose();
        public override bool PrefersUngrabbedMouse => false;

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();
            config = capi.ModLoader.GetModSystem<ConfigLoader>().Config;

            ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 30, 0, 465, 500);
            ElementBounds bgBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, -200, 465, 100);

            string[] colors;
            List<string> c = new List<string>() { "Random" };
            string[] names = Enum.GetNames(typeof(KnownColor));

            for (int i = 28; i < 166; i++)
            {
                c.Add(names[i]);
            }
            colors = c.ToArray();
            config.SetColorIndex = config.SetColorIndex < 0 ? 0 : config.SetColorIndex;

            color = colors[config.SetColorIndex];
            UpdateDropDown(colors, color);

            SingleComposer = capi.Gui.CreateCompo("waypointfrontend", dialogBounds)
                .AddDialogTitleBar("Waypoint Utils", VClose, CairoFont.WhiteSmallText())
                .AddDialogBG(bgBounds)
                .AddTextInput(ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 86.5, -200, 370, 20), OnTextChanged, null, "textinput")
                .AddDropDown(colors, colors, config.SetColorIndex, (newval, on) =>
                {
                    UpdateDropDown(colors, newval);
                    capi.TriggerChatMessage(".wpcfg save");
                },
                ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 250, -170, 125, 25), "dropdown")
                .AddTextToggleButtons(new string[] { "Create WP", "Purge Death Waypoints", "Toggle Floaty Waypoints", "Toggle Block Waypoints", "Toggle This Color" }, CairoFont.ButtonText().WithFontSize(10), i =>
                {
                    capi.Event.RegisterCallback(j =>
                    {
                        SingleComposer.Dispose();
                        OnOwnPlayerDataReceived();
                        SingleComposer.Compose();
                        capi.TriggerChatMessage(".wpcfg save");
                        capi.ModLoader.GetModSystem<WaypointUtilSystem>().RepopulateDialogs();
                    }, 100);
                    switch (i)
                    {
                        case 0:
                            CreateWaypoint();
                            break;
                        case 1:
                            capi.TriggerChatMessage(".wpcfg pdw");
                            break;
                        case 2:
                            capi.TriggerChatMessage(".wpcfg open");
                            break;
                        case 3:
                            capi.TriggerChatMessage(".wpcfg perblockwaypoints");
                            break;
                        case 4:
                            if (config.DisabledColors.Contains(intColor)) config.DisabledColors.Remove(intColor);
                            else config.DisabledColors.Add(intColor);
                            break;
                        default:
                            break;
                    }

                }, new ElementBounds[] {
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 5, -200, 80, 25),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 5, -170, 80, 35),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 85, -170, 80, 35),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 165, -170, 80, 35),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 380, -170, 80, 35),
                });
        }

        public void UpdateDropDown(string[] colors, string value)
        {
            color = value;
            intColor = Color.FromName(value).ToArgb();
            config.SetColorIndex = Array.BinarySearch(colors, value);
        }

        public void CreateWaypoint()
        {
            wpText = wpText != "" ? wpText : "Waypoint";
            if (color == "Random")
            {
                capi.SendChatMessage("/waypoint add #" + ColorStuff.RandomHexColorVClamp(capi, 0.50, 0.80) + " " + wpText);
            }
            else
            {
                capi.SendChatMessage("/waypoint add " + color + " " + wpText);
            }

        }

        public void OnTextChanged(string text)
        {
            wpText = text;
        }

        public override void OnGuiOpened()
        {
            SingleComposer.Compose();
        }

        public override void OnGuiClosed()
        {
            SingleComposer.Dispose();
        }

        bool inputbool = true;
        public override void OnRenderGUI(float deltaTime)
        {
            if (capi.Input.KeyboardKeyState[(int)GlKeys.Enter] && inputbool)
            {
                inputbool = false;
                capi.Event.RegisterCallback(j =>
                {
                    inputbool = true;
                    TryClose();
                }, 100);
                CreateWaypoint();
                capi.Gui.PlaySound("tick");
            }
            base.OnRenderGUI(deltaTime);
        }
    }
}
