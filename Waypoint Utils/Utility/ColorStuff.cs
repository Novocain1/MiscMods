using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    class ColorStuff : ColorUtil
    {
        public static int RandomColor(ICoreAPI api) => HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255)
            );

        public static string RandomHexColor(ICoreAPI api) => RandomColor(api).ToString("X");
        public static string RandomHexColorVClamp(ICoreAPI api, double min, double max) => ClampedRandomColorValue(api, min, max).ToString("X");

        public static int ClampedRandomColorValue(ICoreAPI api, double min, double max)
        {
            return HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(GameMath.Clamp(api.World.Rand.NextDouble(), min, max) * 255)
            );
        }
    }

    static class Extensions
    {
        public static double DistanceTo(this EntityPos aPos, Vec3d bPos)
        {
            return Math.Sqrt(aPos.SquareDistanceTo(bPos));
        }

        public static double RoundedDistanceTo(this EntityPos aPos, Vec3d bPos, int digits) => Math.Round(aPos.DistanceTo(bPos), digits);
    }
}
