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
                "iTemperature", "iRainfall", "iControls1", "iControls2", "iControls3", "iControls4"
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

            float[] c = ControlsAsFloats(capi.World.Player.Entity);
            prog.Uniform("iControls1", new Vec4f(c[0], c[1], c[2], c[3]));
            prog.Uniform("iControls2", new Vec4f(c[4], c[5], c[6], c[7]));
            prog.Uniform("iControls3", new Vec4f(c[8], c[9], c[10], c[11]));
            prog.Uniform("iControls4", new Vec2f(c[12], c[13]));

            capi.Render.RenderMesh(quadRef);
            prog.Stop();
            curShader.Use();
        }

        public float[] ControlsAsFloats(EntityPlayer entity)
        {
            EntityControls c = entity.Controls;
            return new float[]
            {
                c.Backward ? 1 : 0, //0
                c.Down ? 1 : 0, //1
                c.FloorSitting ? 1 : 0, //2
                c.Forward ? 1 : 0, //3
                c.Jump ? 1 : 0, //4
                c.Left ? 1 : 0, //5
                c.LeftMouseDown ? 1 : 0, //6
                c.Right ? 1 : 0, //7
                c.RightMouseDown ? 1 : 0, //8
                c.Sitting ? 1 : 0, //9
                c.Sneak ? 1 : 0, //10
                c.Sprint ? 1 : 0, //11
                c.TriesToMove ? 1 : 0, // 12
                c.Up ? 1 : 0, //13
            };
        }
    }
}
