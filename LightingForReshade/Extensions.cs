using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace RandomTests
{
    public static class Extensions
    {
        public static ClientMain ClientMain(this IClientWorldAccessor world)
        {
            return world as ClientMain;
        }
    }
}
