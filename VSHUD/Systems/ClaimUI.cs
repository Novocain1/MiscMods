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
    class ClaimUI : ClientModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            GuiDialogClaimUI gui = new GuiDialogClaimUI(api);

            api.Input.RegisterHotKey("claimgui", "Open Claim GUI", GlKeys.L, HotkeyType.GUIOrOtherControls);

            api.Event.LevelFinalize += () =>
            {
                if (api.Settings.Bool["claimGui"])
                {
                    gui.TryOpen();
                }
            };

            api.Input.SetHotKeyHandler("claimgui", a =>
            {
                gui.Toggle();
                return true;
            });
        }
    }
}
