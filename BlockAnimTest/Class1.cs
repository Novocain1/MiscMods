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
        public TextureAtlasPosition[] Frames { get; set; }
    }

    class BlockEntityDisplay : BlockEntity
    {
        DisplayRenderer ownRenderer;
        ICoreClientAPI capi;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsClient())
            {
                capi = (ICoreClientAPI)api;
                ownRenderer = new DisplayRenderer(capi, Pos, Block, 32, 32, 1000 / 144);
                capi.Event.RegisterRenderer(ownRenderer, EnumRenderStage.Opaque);
                RegisterGameTickListener(UpdateDisplay, 1000 / 144);
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

        public void UpdateDisplay(float dt)
        {
            for (int i = 0; i < ownRenderer.ScreenColors.Length; i++)
            {
                Vec2i xy = new Vec2i();
                MapUtil.PosInt2d(i, 32, xy);
                double sign = Math.Abs(Math.Sin(capi.World.Calendar.TotalHours * 16 * xy.X * xy.Y));

                ownRenderer.ScreenColors[i] = ColorUtil.ToRgba(255, 0, (int)(255 * sign), 0);
            }
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
            meshRef = capi.Render.UploadMesh(CubeMeshUtil.GetCube().Scale(new Vec3f(1f, 1f, 1f), 0.5f, 0.5f, 0.5f));
            refresher = capi.Event.RegisterGameTickListener(Refresh, refreshTime);
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

        void Refresh(float dt)
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
