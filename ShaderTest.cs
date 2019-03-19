using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ShaderTestMod
{
    public class ShaderTest : ModSystem
    {
        IShaderProgram basicshader;
        ICoreClientAPI capi;
        TestRenderer testRenderer;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            api.Event.ReloadShader += LoadShader;
            LoadShader();

            testRenderer = new TestRenderer(api, basicshader);
            api.Event.RegisterRenderer(testRenderer, EnumRenderStage.Ortho, "basicshader");
        }

        public bool LoadShader()
        {
            basicshader = capi.Shader.NewShaderProgram();
            int program = capi.Shader.RegisterFileShaderProgram("basicshader", basicshader);
            basicshader = capi.Render.GetShader(program);
            basicshader.PrepareUniformLocations(
                "iTime", "iResolution", "iMouse", "iCamera", "iSunPos", "iMoonPos", "iMoonPhase", "iPlayerPosition",
                "iTemperature", "iRainfall"
                );
            basicshader.Compile();

            if (testRenderer != null)
            {
                testRenderer.prog = basicshader;
            }

            return true;
        }
    }

    public class TestRenderer : IRenderer
    {
        MeshRef quadRef;
        ICoreClientAPI capi;
        public IShaderProgram prog;


        public Matrixf ModelMat = new Matrixf();

        public double RenderOrder => 1.1;

        public int RenderRange => 1;

        public TestRenderer(ICoreClientAPI api, IShaderProgram prog)
        {
            this.prog = prog;
            capi = api;

            MeshData quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
            quadMesh.Rgba = null;

            quadRef = capi.Render.UploadMesh(quadMesh);
        }

        public void Dispose()
        {
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!capi.World.Player.Entity.Controls.Sneak) return;
            BlockPos pos = capi.World.Player.Entity.Pos.AsBlockPos;
            IShaderProgram curShader = capi.Render.CurrentActiveShader;
            curShader.Stop();

            prog.Use();
            
            capi.Render.GlToggleBlend(true);
            prog.Uniform("iTime", capi.World.ElapsedMilliseconds / 500f);
            prog.Uniform("iResolution", new Vec2f(capi.Render.FrameWidth, capi.Render.FrameHeight));
            prog.Uniform("iMouse", new Vec2f(capi.Input.MouseX, capi.Input.MouseY));
            prog.Uniform("iCamera", new Vec2f(capi.World.Player.CameraPitch, capi.World.Player.CameraYaw));
            prog.Uniform("iSunPos", capi.World.Calendar.SunPosition);
            prog.Uniform("iMoonPos", capi.World.Calendar.MoonPosition);
            prog.Uniform("iMoonPhase", (float)capi.World.Calendar.MoonPhaseExact);
            prog.Uniform("iPlayerPosition", capi.World.Player.Entity.LocalPos.XYZ.ToVec3f());
            prog.Uniform("iTemperature", capi.World.BlockAccessor.GetClimateAt(pos).Temperature);
            prog.Uniform("iRainfall", capi.World.BlockAccessor.GetClimateAt(pos).Rainfall);

            capi.Render.RenderMesh(quadRef);
            prog.Stop();
            curShader.Use();
        }
    }
}
