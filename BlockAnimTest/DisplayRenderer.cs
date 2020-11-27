using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BlockAnimTest
{
    class HV
    {
        public float H { get; set; }
        public float V { get; set; }
    }

    class DisplayRenderer : IRenderer
    {
        ICoreClientAPI capi;
        BlockPos pos;
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
            meshRef = capi.Render.UploadMesh(CubeMeshUtil.GetCube(0.5f, 0.5f, new Vec3f(0.5f, 0.5f, 0.5f)));
        }

        public int XRes { get; set; }
        public int YRes { get; set; }
        public Vec2f Resolution { get => new Vec2f(XRes, YRes); }
        public int[] ScreenColors { get; set; }

        public double RenderOrder => 1;

        public int RenderRange => 1;

        public void Dispose()
        {
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

            IStandardShaderProgram prog = blockAnimMod.PreparedDisplayShader(pos.X, pos.Y, pos.Z);
            
            prog.Tex2D = display.TextureId;
            //prog.Resolution = Resolution;
            //prog.ScreenColors = display.TextureId;
            //prog.DisplayBrightness = 0.5f;

            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z).Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;
            capi.Render.RenderMesh(meshRef);
            prog.Stop();

            activeShader?.Use();
        }
    }
}
