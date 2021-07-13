using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace VSHUD
{
    class FloatyDamage : ClientModSystem
    {
        public override void StartClientSide(ICoreClientAPI api)
        {
            api.RegisterCommand("fdcfg", "Floaty damage configuration", "state", (id, args) =>
            {
                switch (args.PopWord())
                {
                    case "state":
                        ConfigLoader.Config.FDShow = args.PopBool() ?? !ConfigLoader.Config.FDShow;
                        api.ShowChatMessage(string.Format("Floaty Damage Hud Element Generation Set To {0}", ConfigLoader.Config.FDShow));
                        break;
                    case "range":
                        ConfigLoader.Config.FDRange = args.PopFloat() ?? ConfigLoader.Config.FDRange;
                        api.ShowChatMessage(string.Format("Floaty damage raycasting range set to {0}.", ConfigLoader.Config.FDRange));
                        break;
                    default:
                        break;
                }
                ConfigLoader.SaveConfig(api);
            });
        }
    }
}
