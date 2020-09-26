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
}
