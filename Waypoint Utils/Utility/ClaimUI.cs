using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace VSHUD
{
    class ClaimUI : ModSystem
    {
        private long id;

        public override void StartClientSide(ICoreClientAPI api)
        {
            GuiDialogClaimUI gui = new GuiDialogClaimUI(api);

            api.Input.RegisterHotKey("claimgui", "Open Claim GUI", GlKeys.L, HotkeyType.GUIOrOtherControls);

            id = api.Event.RegisterGameTickListener(dt => 
            {
                if (api.World.Player?.Entity != null)
                {
                    if (api.Settings.Bool["claimGui"])
                    {
                        gui.TryOpen();
                    }

                    api.Event.UnregisterGameTickListener(id);
                }
                
            }, 1000);

            api.Input.SetHotKeyHandler("claimgui", a =>
            {
                gui.Toggle();
                return true;
            });
        }
    }

    class GuiDialogClaimUI : GuiDialog
    {
        public bool op = false;
        public GuiDialogClaimUI(ICoreClientAPI capi) : base(capi)
        {
            this.capi = capi;
        }

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();

            //ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 30, 0, 465, 500);
            //ElementBounds bgBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, -150, 465, 200);
            ElementBounds radialRoot = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, 0, 25, 25);
            ElementBounds dialogBounds = radialRoot.CopyOffsetedSibling(0, 0, 200, 100);
            ElementBounds bgBounds = dialogBounds.CopyOffsetedSibling();
            radialRoot = radialRoot.WithFixedOffset(165, 15);

            SingleComposer = capi.Gui.CreateCompo("claim", dialogBounds)
                .AddDialogTitleBar("Claims", () => TryClose(), CairoFont.WhiteSmallText())
                .AddDialogBG(bgBounds)
                .AddTextToggleButtons(new string[] { "New", "Start", "End", "Add", "Cancel", "Save", "U", "D", "N", "S", "E", "W" }, CairoFont.ButtonText().WithFontSize(10), 
                i => 
                {
                    int m = op ? -1 : 1;
                    switch (i)
                    {
                        case 0:
                            capi.SendChatMessage("/land claim new");
                            break;
                        case 1:
                            capi.SendChatMessage("/land claim start");
                            break;
                        case 2:
                            capi.SendChatMessage("/land claim end");
                            break;
                        case 3:
                            capi.SendChatMessage("/land claim add");
                            break;
                        case 4:
                            capi.SendChatMessage("/land claim cancel");
                            break;
                        case 5:
                            capi.SendChatMessage("/land claim save " + capi.World.Claims.All.Count);
                            break;
                        case 6:
                            capi.SendChatMessage("/land claim gu " + m);
                            break;
                        case 7:
                            capi.SendChatMessage("/land claim gd " + m);
                            break;
                        case 8:
                            capi.SendChatMessage("/land claim gn " + m);
                            break;
                        case 9:
                            capi.SendChatMessage("/land claim gs " + m);
                            break;
                        case 10:
                            capi.SendChatMessage("/land claim ge " + m);
                            break;
                        case 11:
                            capi.SendChatMessage("/land claim gw " + m);
                            break;
                        default:
                            break;
                    }
                    capi.Event.RegisterCallback(dt => SingleComposer.GetToggleButton("buttons-" + i).On = false, 50);
                }, 
                new ElementBounds[] 
                {
                    radialRoot.CopyOffsetedSibling(-150, -25, 25),
                    radialRoot.CopyOffsetedSibling(-150, 0, 25),
                    radialRoot.CopyOffsetedSibling(-150, 25, 25),
                    radialRoot.CopyOffsetedSibling(-100, 0, 25),
                    radialRoot.CopyOffsetedSibling(-100, -25, 25),
                    radialRoot.CopyOffsetedSibling(-100, 25, 25),
                    radialRoot.CopyOffsetedSibling(-50, -25),
                    radialRoot.CopyOffsetedSibling(-50, 25),
                    radialRoot.CopyOffsetedSibling(0, -25),
                    radialRoot.CopyOffsetedSibling(0, 25),
                    radialRoot.CopyOffsetedSibling(25, 0),
                    radialRoot.CopyOffsetedSibling(-25, 0),
                    radialRoot,
                }, "buttons")
                .AddToggleButton("OP", CairoFont.ButtonText().WithFontSize(10), 
                b => 
                {
                    op = b;
                }, radialRoot)
                .Compose();
        }

        public override bool TryOpen()
        {
            if (base.TryOpen())
            {
                OnOwnPlayerDataReceived();
                capi.Settings.Bool["claimGui"] = true;
                return true;
            }
            return false;
        }

        public override bool TryClose()
        {
            if (base.TryClose())
            {
                capi.Settings.Bool["claimGui"] = false;
                op = false;
                Dispose();
                return true;
            }
            return false;
        }

        public override string ToggleKeyCombinationCode => "claimgui";
    }
}
