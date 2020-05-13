using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace BlockAnimTest
{
    class Statics
    {
        public static readonly TextureAnimation TestAnimation = new TextureAnimation()
        {
            Frames = new Vec2f[][]
            {
                new Vec2f[]
                {
                    new Vec2f(0, 0),
                },
                new Vec2f[]
                {
                    new Vec2f(0, 0),
                    new Vec2f(0.25f, 0.25f),
                },
                new Vec2f[]
                {
                    new Vec2f(0, 0),
                    new Vec2f(0.25f, 0.25f),
                    new Vec2f(0.50f, 0.50f),
                },
                new Vec2f[]
                {
                    new Vec2f(0, 0),
                    new Vec2f(0.25f, 0.25f),
                    new Vec2f(0.50f, 0.50f),
                    new Vec2f(0.75f, 0.75f),
                },
                new Vec2f[]
                {
                    new Vec2f(0, 0),
                    new Vec2f(0.25f, 0.25f),
                    new Vec2f(0.50f, 0.50f),
                    new Vec2f(0.75f, 0.75f),
                    new Vec2f(1, 1),
                }
            },
            Colors = new Vec3f[][]
            {
                new Vec3f[]
                {
                    new Vec3f(0, 1, 0),
                },
                new Vec3f[]
                {
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                },
                new Vec3f[]
                {
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                },
                new Vec3f[]
                {
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                },
                new Vec3f[]
                {
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                    new Vec3f(0, 1, 0),
                }
            }
        };
    }
}
