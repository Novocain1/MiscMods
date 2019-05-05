using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DeathWaypoints
{
    class AutoDeathWaypoints : ModSystem
    {
        long id;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            
            id = api.World.RegisterGameTickListener(dt => {
                EntityPlayer player = api.World.Player.Entity;

                if (player != null)
                {
                    player.WatchedAttributes.RegisterModifiedListener("entityDead", () =>
                    {
                        if (player.WatchedAttributes["entityDead"].ToString() == "1")
                        {
                            api.SendChatMessage("/waypoint add #" + ColorStuff.RandomHexColor(api) + " Player Death Waypoint");
                        }
                    });
                    api.World.UnregisterGameTickListener(id);
                }
            }, 500);
        }
    }

    class ColorStuff
    {
        public static int RandomColor(ICoreAPI api) => ColorUtil.HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255)
            );

        public static string RandomHexColor(ICoreAPI api) => RandomColor(api).ToString("X");
    }
}
