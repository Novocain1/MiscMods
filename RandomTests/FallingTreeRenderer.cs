using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace RandomTests
{
    public class FallingTreeRenderer : IRenderer
    {
        public ICoreClientAPI capi;
        public MeshRef treeMesh;
        public Matrixf ModelMat = new Matrixf();
        public BlockPos pos;
        public BlockFacing fallDirection;
        public float startFallTime;
        public float fallTime;
        public bool isLeaves;

        public FallingTreeRenderer(ICoreClientAPI capi, BlockPos pos, bool isLeaves, MeshData treeMesh, float fallTime, BlockFacing fallDirection, EnumRenderStage pass)
        {
            if (pos == null || treeMesh == null) return;

            this.capi = capi;
            this.pos = pos;
            this.treeMesh = capi.Render.UploadMesh(treeMesh);
            this.fallDirection = fallDirection;
            this.isLeaves = isLeaves;
            startFallTime = this.fallTime = fallTime;
            capi.Event.RegisterRenderer(this, pass);
        }

        public double RenderOrder => 1.0;

        public int RenderRange => 0;

        public void Dispose()
        {
            treeMesh?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (fallTime < -1)
            {
                capi.Event.UnregisterRenderer(this, stage);
                Dispose();
                return;
            }

            float percent = fallTime / startFallTime;

            IRenderAPI rpi = capi.Render;
            Vec3d camPos = capi.World.Player.Entity.CameraPos;

            if (isLeaves) rpi.GlDisableCullFace();

            rpi.GlToggleBlend(true);

            bool shadowPass = stage != EnumRenderStage.Opaque;

            IShaderProgram prevProg = rpi.CurrentActiveShader;
            prevProg?.Stop();

            IShaderProgram sProg = shadowPass ? rpi.GetEngineShader(EnumShaderProgram.Shadowmapentityanimated) : rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);

            var mat = ModelMat
                    .Identity()
                    .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                ;

            switch (fallDirection.Code)
            {
                case "north":
                    mat.RotateXDeg(GameMath.Max(percent * 90.0f - 90.0f, -90.0f));
                    break;
                case "south":
                    mat.Translate(0, 0, 1);
                    mat.RotateXDeg(GameMath.Min(percent * -90.0f + 90.0f, 90.0f));
                    mat.Translate(0, 0, -1);
                    break;
                case "east":
                    mat.Translate(1, 0, 1);
                    mat.RotateZDeg(GameMath.Max(percent * 90.0f - 90.0f, -90.0f));
                    mat.Translate(-1, 0, -1);
                    break;
                case "west":
                    mat.Translate(0, 0, 1);
                    mat.RotateZDeg(GameMath.Min(percent * -90.0f + 90.0f, 90.0f));
                    mat.Translate(0, 0, -1);
                    break;
                default:
                    break;
            }

            var matVals = mat.Values;

            if (!shadowPass)
            {
                var prog = (IStandardShaderProgram)sProg;

                prog.Tex2D = capi.BlockTextureAtlas.AtlasTextureIds[0];
                prog.ModelMatrix = matVals;

                prog.AlphaTest = 0.4f;

                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                if (fallTime < 0) 
                    prog.RgbaTint = new Vec4f(1, 1, 1, 1.0f - Math.Abs(fallTime));
            }
            else
            {
                sProg.Use();
                sProg.BindTexture2D("entityTex", capi.BlockTextureAtlas.AtlasTextureIds[0], 1);
                sProg.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
                sProg.UniformMatrix("modelViewMatrix", Mat4f.Mul(new float[16], capi.Render.CurrentModelviewMatrix, matVals));
            }
            
            rpi.RenderMesh(treeMesh);
            sProg.Stop();
            
            prevProg?.Use();

            fallTime -= deltaTime;
        }
    }
}
