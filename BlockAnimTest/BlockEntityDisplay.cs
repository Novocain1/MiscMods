using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BlockAnimTest
{
    class BlockEntityDisplay : BlockEntity
    {
        DisplayRenderer ownRenderer;
        ICoreClientAPI capi;
        Vec2i resolution;

        public TextureAnimation OwnAnimation { get; set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            
            OwnAnimation = Statics.TestAnimation;

            resolution = new Vec2i(32, 32);
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

        public void SetPixel(int x, int y, int color)
        {
            int index = MapUtil.Index2d(x, y, resolution.X);
            ownRenderer.ScreenColors[index] = color;
        }

        int frame = 0;
        Vec2i[] LastPixels;

        public void UpdateDisplay(float dt)
        {
            var val = OwnAnimation.Frames[frame];

            if (LastPixels != null)
            {
                for (int i = 0; i < LastPixels.Length; i++)
                {
                    if (LastPixels[i] == null) continue;
                    SetPixel(LastPixels[i].X, LastPixels[i].Y, ColorUtil.ToRgba(255, 0, 0, 0));
                }
            }

            LastPixels = new Vec2i[val.Length];

            for (int i = 0; i < val.Length; i++)
            {
                LastPixels[i] = new Vec2i();
                LastPixels[i].X = GameMath.Clamp((int)(val[i].X * resolution.X), 0, resolution.X - 1);
                LastPixels[i].Y = GameMath.Clamp((int)(val[i].Y * resolution.Y), 0, resolution.Y - 1);

                int colorR = (int)(OwnAnimation.Colors[frame][i].R * 255);
                int colorG = (int)(OwnAnimation.Colors[frame][i].G * 255);
                int colorB = (int)(OwnAnimation.Colors[frame][i].B * 255);

                SetPixel(LastPixels[i].X, LastPixels[i].Y, ColorUtil.ToRgba(255, colorR, colorG, colorB));
            }
            
            if (frame < OwnAnimation.Frames.Length - 1) frame++;
            else frame = 0;

            ownRenderer.MarkDirty();
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
