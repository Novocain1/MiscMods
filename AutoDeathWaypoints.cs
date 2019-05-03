using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DeathWaypoints
{
    class AutoDeathWaypoints : ModSystem
    {
        long id;
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            id = api.World.RegisterGameTickListener(dt => {
                if (api.World.Player.Entity != null)
                {
                    api.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("entityDead", () =>
                    {
                        if (api.World.Player.Entity.Alive)
                        {
                            int color = ColorUtil.HsvToRgb((int)(api.World.Rand.NextDouble() * 255), (int)(api.World.Rand.NextDouble() * 255), (int)(api.World.Rand.NextDouble() * 255));
                            string hex = color.ToString("X");
                            api.SendChatMessage("/waypoint add #" + hex + " Player Death Waypoint");
                        }
                    });
                    api.World.UnregisterGameTickListener(id);
                }
            }, 500);

        }
    }
}
