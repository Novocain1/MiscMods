using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Common.Entities;

namespace ShaderTestMod
{
    public class ShaderTest : ModSystem
    {
        public ScreenSpaceRenderer renderer;
        public ICoreClientAPI capi;
        public float[] controls;
        public Vec3f[] vec3s;
        public float[] floats;
        ITreeAttribute healthTree;

        public string[] orthoShaderKeys;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            api.Event.LevelFinalize += () => { capi.Shader.ReloadShaders();  StartShade(capi.World.Player); };
        }

        public void StartShade(IPlayer player)
        {
            healthTree = player.Entity.WatchedAttributes.GetTreeAttribute("health");

            controls = GetControls();
            vec3s = GetVec3s();
            floats = GetFloats();

            capi.Event.RegisterGameTickListener(dt =>
            {
                controls = GetControls();
                vec3s = GetVec3s();
                floats = GetFloats();
            }, 0);

            capi.Event.ReloadShader += LoadShaders;
            LoadShaders();
        }

        public bool LoadShaders()
        {
            if (renderer != null)
            {
                capi.Event.UnregisterRenderer(renderer, EnumRenderStage.Ortho);
                renderer.Dispose();
            }

            List<IShaderProgram> shaders = new List<IShaderProgram>();
            orthoShaderKeys = capi.Assets.TryGet("config/orthoshaderlist.json")?.ToObject<string[]>();
            if (orthoShaderKeys == null) return false;

            foreach (var val in orthoShaderKeys)
            {
                IShaderProgram shader = capi.Shader.NewShaderProgram();
                int program = capi.Shader.RegisterFileShaderProgram(val, shader);
                shader = capi.Render.GetShader(program);
                shader.Compile();
                shaders.Add(shader);
            }
            renderer = new ScreenSpaceRenderer(capi, shaders);
            capi.Event.RegisterRenderer(renderer, EnumRenderStage.Ortho);
            return true;
        }

        public float[] GetControls()
        {
            IPlayer player = capi.World.Player;
            EntityControls c = player.Entity.Controls;
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

        public float[] GetFloats()
        {
            IPlayer player = capi.World.Player;
            BlockPos pos = capi.World.Player.Entity.Pos.AsBlockPos;
            BlockPos lPos = player.CurrentBlockSelection != null ? player.CurrentBlockSelection.Position : new BlockPos(0, -1, 0);
            return new float[]
            {
                (float)capi.World.Calendar.MoonPhaseExact,
                capi.World.BlockAccessor.GetClimateAt(pos).Temperature,
                capi.World.BlockAccessor.GetClimateAt(pos).Rainfall,
                (float)healthTree.TryGetFloat("currenthealth"),
                (float)healthTree.TryGetFloat("maxhealth"),
                player.InventoryManager.ActiveHotbarSlot.Itemstack != null ? player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.Id : -1,
                capi.World.BlockAccessor.GetBlock(lPos).Id,
                player.CurrentEntitySelection != null ? player.CurrentEntitySelection.Entity.EntityId : -1,
                player.InventoryManager.ActiveTool != null ? (float)player.InventoryManager.ActiveTool : -1,
                (capi.World.BlockAccessor.GetClimateAt(pos).Temperature + 50.0f) * 0.01f,
            };
        }

        public Vec3f[] GetVec3s()
        {
            IPlayer player = capi.World.Player;
            BlockPos lPos = player.CurrentBlockSelection != null ? player.CurrentBlockSelection.Position : new BlockPos(0, -1, 0);
            return new Vec3f[]
            {
                capi.World.Calendar.SunPosition,
                capi.World.Calendar.MoonPosition,
                player.Entity.LocalPos.XYZ.ToVec3f(),
                lPos != new BlockPos(0, -1, 0) ? lPos.ToVec3f().Add(0.5f,0.5f,0.5f) : new Vec3f(),
                player.CurrentEntitySelection != null ? player.CurrentEntitySelection.Entity.Pos.XYZFloat : new Vec3f(),
            };
        }
    }

    public class ScreenSpaceRenderer : IRenderer
    {
        MeshRef quadRef;
        ICoreClientAPI capi;
        public List<IShaderProgram> progs;

        public Matrixf ModelMat = new Matrixf();

        public double RenderOrder => 0;

        public int RenderRange => 1000;

        public ScreenSpaceRenderer(ICoreClientAPI api, List<IShaderProgram> progs)
        {
            this.progs = progs;
            capi = api;
            MeshData quadMesh = QuadMeshUtil.GetQuad();
            quadMesh.Rgba = null;
            quadRef = capi.Render.UploadMesh(quadMesh);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            capi.Render.GlToggleBlend(true);
            IShaderProgram curShader = capi.Render.CurrentActiveShader;

            curShader?.Stop();
            foreach (var prog in progs)
            {
                if (prog.Disposed) continue;
                prog.Use();
                prog.SetDefaultUniforms(capi);
                prog.BindTexture2D("iDepthBuffer", capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].DepthTextureId, 0);
                prog.BindTexture2D("iColor", capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].ColorTextureIds[0], 1);
                prog.BindTexture2D("iLight", capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].ColorTextureIds[1], 2);
                prog.BindTexture2D("iGodrays", capi.Render.FrameBuffers[(int)EnumFrameBuffer.GodRays].ColorTextureIds[0], 3);
                prog.BindTexture2D("iShadowMapNear", capi.Render.FrameBuffers[(int)EnumFrameBuffer.ShadowmapNear].DepthTextureId, 4);
                prog.BindTexture2D("iShadowMapFar", capi.Render.FrameBuffers[(int)EnumFrameBuffer.ShadowmapFar].DepthTextureId, 5);
                capi.Render.RenderMesh(quadRef);
                prog.Stop();
            }
            curShader?.Use();
        }

        public void Dispose()
        {
            quadRef.Dispose();
        }
    }

    public static class DefaultUniforms
    {
        public static void SetDefaultUniforms(this IShaderProgram prog, ICoreClientAPI capi)
        {
            ShaderTest shaderTest = capi.ModLoader.GetModSystem<ShaderTest>();
            float[] controls = shaderTest.controls;
            Vec3f[] vec3s = shaderTest.vec3s;
            float[] floats = shaderTest.floats;

            prog.Uniform("iTime", capi.World.ElapsedMilliseconds / 500f);
            prog.Uniform("iResolution", new Vec2f(capi.Render.FrameWidth, capi.Render.FrameHeight));
            prog.Uniform("iMouse", new Vec2f(capi.Input.MouseX, capi.Input.MouseY));
            prog.Uniform("iCamera", new Vec2f(capi.World.Player.CameraPitch, capi.World.Player.CameraYaw));
            prog.Uniform("iCameraPos", EntityPos.GetViewVector(capi.Input.MousePitch, capi.Input.MouseYaw));

            prog.Uniform("iControls1", new Vec4f(controls[0], controls[1], controls[2], controls[3]));
            prog.Uniform("iControls2", new Vec4f(controls[4], controls[5], controls[6], controls[7]));
            prog.Uniform("iControls3", new Vec4f(controls[8], controls[9], controls[10], controls[11]));
            prog.Uniform("iControls4", new Vec2f(controls[12], controls[13]));

            prog.Uniform("iSunPos", vec3s[0]);
            prog.Uniform("iMoonPos", vec3s[1]);
            prog.Uniform("iPlayerPosition", vec3s[2]);
            prog.Uniform("iLookBlockPos", vec3s[3]);
            prog.Uniform("iLookEntityPos", vec3s[4]);

            prog.Uniform("iMoonPhase", floats[0]);
            prog.Uniform("iTemperature", floats[1]);
            prog.Uniform("iRainfall", floats[2]);
            prog.Uniform("iCurrentHealth", floats[3]);
            prog.Uniform("iMaxHealth", floats[4]);
            prog.Uniform("iActiveItem", floats[5]);
            prog.Uniform("iLookingAtBlock", floats[6]);
            prog.Uniform("iLookingAtEntity", floats[7]);
            prog.Uniform("iActiveTool", floats[8]);
            prog.Uniform("iTempScalar", floats[9]);
        }

        public static void ReRegisterRenderer(this IClientEventAPI events, IRenderer renderer, EnumRenderStage stage)
        {
            renderer.Dispose();
            events.UnregisterRenderer(renderer, stage);
            events.RegisterRenderer(renderer, stage);
        }
    }
}