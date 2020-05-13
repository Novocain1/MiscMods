using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BlockAnimTest
{
    public class BlockAnimMod : ModSystem
    {
        ICoreClientAPI capi;
        IDisplayShaderProgram displayShaderProgram;

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("Display", typeof(BlockEntityDisplay));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            
            capi.Event.ReloadShader += LoadShader;
            LoadShader();
        }

        public bool LoadShader()
        {
            displayShaderProgram = new DisplayShaderProgram();
            capi.Shader.RegisterFileShaderProgram("display", displayShaderProgram);
            return displayShaderProgram.Compile();
        }

        public IDisplayShaderProgram PreparedDisplayShader(int posX, int posY, int posZ)
        {
            Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(posX, posY, posZ);
            IDisplayShaderProgram prog = displayShaderProgram;
            
            prog.Use();

            prog.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
            prog.RgbaTint = ColorUtil.WhiteArgbVec;
            prog.RgbaAmbientIn = capi.Render.AmbientColor;
            prog.RgbaLightIn = lightrgbs;
            prog.RgbaBlockIn = ColorUtil.WhiteArgbVec;
            prog.RgbaFogIn = capi.Render.FogColor;
            prog.NormalShaded = 1;
            prog.ExtraGlow = 0;
            prog.ExtraZOffset = 0;
            prog.OverlayOpacity = 0;
            prog.ExtraGodray = 0;
            prog.FogMinIn = capi.Render.FogMin;
            prog.FogDensityIn = capi.Render.FogDensity;
            prog.DontWarpVertices = 0;
            prog.AddRenderFlags = 0;

            return prog;
        }
    }

    public class TextureAnimation
    {
        public Vec2f[][] Frames { get; set; }
        public Vec3f[][] Colors { get; set; }
    }

    class BlockEntityDisplay : BlockEntity
    {
        DisplayRenderer ownRenderer;
        ICoreClientAPI capi;
        Vec2i resolution;

        TextureAnimation testAnimation = new TextureAnimation()
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

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
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
            var val = testAnimation.Frames[frame];

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

                int colorR = (int)(testAnimation.Colors[frame][i].R * 255);
                int colorG = (int)(testAnimation.Colors[frame][i].G * 255);
                int colorB = (int)(testAnimation.Colors[frame][i].B * 255);

                SetPixel(LastPixels[i].X, LastPixels[i].Y, ColorUtil.ToRgba(255, colorR, colorG, colorB));
            }
            
            if (frame < testAnimation.Frames.Length - 1) frame++;
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

    public class DisplayShaderProgram : ShaderProgramStandard, IDisplayShaderProgram
    {
        public Vec2f Resolution { set { Uniform("resolution", value); } }
        public int ScreenColors { set { BindTexture2D("screenColors", value); } }
        public float DisplayBrightness { set { Uniform("brightness", value); } }
    }

    public interface IDisplayShaderProgram : IStandardShaderProgram
    {
        Vec2f Resolution { set; }
        int ScreenColors { set; }
        float DisplayBrightness { set; }
    }

    class DisplayRenderer : IRenderer
    {
        ICoreClientAPI capi;
        BlockPos pos;
        long refresher;
        MeshRef meshRef;
        LoadedTexture display;

        BlockAnimMod blockAnimMod { get => capi.ModLoader.GetModSystem<BlockAnimMod>(); }
        
        public Matrixf ModelMat = new Matrixf();

        public DisplayRenderer(ICoreClientAPI capi, BlockPos pos, Block block, int XRes, int YRes, int refreshTime)
        {
            this.capi = capi;
            this.pos = pos;
            this.XRes = XRes;
            this.YRes = YRes;

            MeshData mesh;
            
            display = new LoadedTexture(capi) { Width = XRes, Height = YRes };

            ScreenColors = new int[XRes * YRes];
            for (int i = 0; i < ScreenColors.Length; i++)
            {
                ScreenColors[i] = ColorUtil.ToRgba(255, 0, 0, 0);
            }
            
            capi.Render.LoadOrUpdateTextureFromRgba(ScreenColors, false, 0, ref display);
            capi.Tesselator.TesselateBlock(block, out mesh);
            meshRef = capi.Render.UploadMesh(CubeMeshUtil.GetCube(0.5f, 0.5f, new Vec3f(0.5f,0.5f,0.5f)));
        }

        public int XRes { get; set; }
        public int YRes { get; set; }
        public Vec2f Resolution { get => new Vec2f(XRes, YRes); }
        public int[] ScreenColors { get; set; }

        public double RenderOrder => 1;

        public int RenderRange => 1;

        public void Dispose()
        {
            capi.Event.UnregisterGameTickListener(refresher);
            meshRef?.Dispose();
            display?.Dispose();
        }
        public void MarkDirty()
        {
            capi.Render.LoadOrUpdateTextureFromRgba(ScreenColors, false, 0, ref display);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IRenderAPI render = capi.Render;
            IShaderProgram activeShader = render.CurrentActiveShader;
            activeShader?.Stop();

            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;

            IDisplayShaderProgram prog = blockAnimMod.PreparedDisplayShader(pos.X, pos.Y, pos.Z);
            
            prog.Tex2D = capi.BlockTextureAtlas.AtlasTextureIds[0];
            prog.Resolution = Resolution;
            prog.ScreenColors = display.TextureId;
            prog.DisplayBrightness = 0.5f;

            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z).Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;
            capi.Render.RenderMesh(meshRef);
            prog.Stop();

            activeShader?.Use();
        }
    }
}
