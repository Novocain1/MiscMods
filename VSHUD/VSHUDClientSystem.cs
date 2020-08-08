using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

[assembly: ModInfo("VSHUD",
    Description = "Automatically creates waypoints on player death, floaty waypoints, and other misc client side things",
    Side = "Client",
    Authors = new[] { "Novocain" },
    Version = "1.5.4.hotfix")]

namespace VSHUD
{
    class VSHUDClientSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;
    }
}
