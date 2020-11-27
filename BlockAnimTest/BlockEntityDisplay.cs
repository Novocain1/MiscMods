using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace BlockAnimTest
{
    class Charsets8x8
    {
        public static readonly int[] A = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 1, 1, 0, 0, 0,
            0, 1, 1, 1, 1, 1, 1, 0,
            0, 1, 1, 0, 0, 1, 1, 0,
            0, 1, 1, 1, 1, 1, 1, 0,
            0, 1, 1, 1, 1, 1, 1, 0,
            0, 1, 1, 0, 0, 1, 1, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
        };

        public static readonly int[] Box = new int[]
        {
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 0, 0, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 0, 0, 0, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
        };
    }

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

        public static int ClampedRandomColorValueRGBA(ICoreAPI api, double min, double max)
        {
            int rgb = ClampedRandomColorValue(api, min, max);
            Vec3f floats = new Vec3f();
            ToRGBVec3f(rgb, ref floats);
            return ColorUtil.ToRgba(255, (int)(floats.R * 255), (int)(floats.G * 255), (int)(floats.B * 255));
        }
    }

    class BlockEntityDisplay : BlockEntity
    {
        DisplayRenderer ownRenderer;
        ICoreClientAPI capi;
        Vec2i resolution;

        public bool DemoMode { get; set; } = true;

        public bool DisplayDirty { get; set; } = true;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
            resolution = new Vec2i(128, 128);
            if (api.Side.IsClient())
            {
                capi = (ICoreClientAPI)api;
                ownRenderer = new DisplayRenderer(capi, Pos, Block, resolution.X, resolution.Y, 1000 / 144);
                capi.Event.RegisterRenderer(ownRenderer, EnumRenderStage.Opaque);
                RegisterGameTickListener(UpdateDisplay, 1000 / 15);
            }
            MarkDirty(true);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (Api.Side.IsClient())
            {
                ICoreClientAPI capi = (ICoreClientAPI)Api;
                capi.Event.UnregisterRenderer(ownRenderer, EnumRenderStage.Opaque);
                ownRenderer?.Dispose();
            }
        }

        public void SetPixels(int xOrigin, int yOrigin, int color, int[] pixs)
        {
            int imgSize = (int)Math.Sqrt(pixs.Length);
            for (int x = 0; x < imgSize; x++)
            {
                for (int y = 0; y < imgSize; y++)
                {
                    int pixIndex = MapUtil.Index2d(x, y, imgSize);
                    if (pixs[pixIndex] < 1) continue;

                    SetPixel(xOrigin + imgSize - x - 1, yOrigin + imgSize - y - 1, color);
                }
            }
        }

        public void SetPixel(int x, int y, int color)
        {
            int index = MapUtil.Index2d(x, y, resolution.X);
            ownRenderer.ScreenColors[index] = color;
        }

        int demoX, demoY, lastX, lastY, demoCol = ColorUtil.ToRgba(255, 255, 0, 0);
        
        Vec2d velocity = new Vec2d(0.5, 0.5);

        int[] clearing = (new int[64]).Fill(1);
        int clearCol = ColorUtil.ToRgba(255, 0, 0, 0);

        public void SetNewDemoCol()
        {
            demoCol = ColorStuff.ClampedRandomColorValueRGBA(capi, 0.5, 0.8);
        }

        public void UpdateDisplay(float dt)
        {
            if (DemoMode)
            {
                lastX = demoX; lastY = demoY;
                SetPixels(lastX, lastY, clearCol, clearing);

                demoX += (int)(velocity.X * 8);
                demoY += (int)(velocity.Y * 8);

                if (demoX >= resolution.X - 8 || demoY >= resolution.Y - 8)
                {
                    velocity = new Vec2d(-capi.World.Rand.NextDouble(), -capi.World.Rand.NextDouble());
                    demoX = demoX >= resolution.X - 8 ? resolution.X - 8 : demoX;
                    demoY = demoY >= resolution.Y - 8 ? resolution.Y - 8 : demoY;
                    SetNewDemoCol();
                }

                if (demoX <= 0 || demoY <= 0)
                {
                    velocity = new Vec2d(capi.World.Rand.NextDouble(), capi.World.Rand.NextDouble());

                    demoX = demoX <= 0 ? 0 : demoX;
                    demoY = demoY <= 0 ? 0 : demoY;
                    SetNewDemoCol();
                }

                SetPixels(demoX, demoY, demoCol, Charsets8x8.Box);

                MarkDisplayDirty();
            }

            if (DisplayDirty)
            {
                DisplayDirty = false;
                ownRenderer.MarkDirty();
            }
        }

        public void MarkDisplayDirty()
        {
            DisplayDirty = true;
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            
            if (Api.Side.IsClient())
            {
                ICoreClientAPI capi = (ICoreClientAPI)Api;
                capi.Event.UnregisterRenderer(ownRenderer, EnumRenderStage.Opaque);
                ownRenderer?.Dispose();
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            return true;
        }
    }

}
