using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using OpenTK.Graphics.OpenGL;
using Vintagestory.Client.NoObf;

namespace LightingForReshade
{
    public class ReloadShaders : ModSystem
    {
        //public RenderToNewDepthBuffer DepthBufferRenderer { get; private set; }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                //DepthBufferRenderer = new RenderToNewDepthBuffer(api);
                api.Shader.ReloadShaders();
            };
        }
    }

    /*
    public class RenderToNewDepthBuffer : IRenderer
    {
        ICoreClientAPI capi;
        public FrameBufferRef BufferRef { get; private set; }
        public ClientPlatformWindows Platform { get; private set; }
        public ChunkRenderer ChunkRenderer { get; private set; }

        public RenderToNewDepthBuffer(ICoreClientAPI capi)
        {
            
            this.capi = capi;
            Platform = capi.World.GetField<ClientPlatformWindows>("Platform");
            ChunkRenderer = capi.World.GetField<ChunkRenderer>("chunkRenderer");

            SetupFrameBuffers();
            Platform.WindowResized += (a, b) => SetupFrameBuffers();

            capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
        }

        public void SetupFrameBuffers()
        {
            if (BufferRef != null)
            {
                Platform.DisposeFrameBuffer(BufferRef);
            }

            int fbWidth = (int)(capi.Render.FrameWidth * ClientSettings.SSAA);
            int fbHeight = (int)(capi.Render.FrameHeight * ClientSettings.SSAA);

            BufferRef = new FrameBufferRef()
            {
                FboId = GL.GenFramebuffer(), Width = fbWidth, Height = fbHeight, DepthTextureId = GL.GenTexture(), ColorTextureIds = new[] {GL.GenTexture()} 
            };
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, BufferRef.FboId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, BufferRef.DepthTextureId, 0);
            
            GL.BindTexture(TextureTarget.Texture2D, BufferRef.ColorTextureIds[0]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fbWidth, fbHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new[] { 1f, 1f, 1f, 1f });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, BufferRef.ColorTextureIds[0], 0);

            GL.DrawBuffer(DrawBufferMode.Back);

            CheckFboStatus();   
        }

        public void CheckFboStatus()
        {
            Platform.CallMethod("CheckFboStatus", FramebufferTarget.Framebuffer, BufferRef.FboId.ToString());
        }

        public double RenderOrder => 1.0;

        public int RenderRange => 1000;

        public void Dispose()
        {
            if (BufferRef != null)
            {
                Platform.DisposeFrameBuffer(BufferRef);
            }
        }

        [Obsolete]
        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            var active = capi.Render.CurrentActiveShader;
            active?.Stop();
            var opq = capi.Render.GetEngineShader(EnumShaderProgram.Chunkopaque);

            Platform.LoadFrameBuffer(BufferRef);
            Platform.ClearFrameBuffer(BufferRef, false);

            capi.Render.GlPushMatrix();
            capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);

            opq.Use();

            opq.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
            opq.UniformMatrix("modelViewMatrix", capi.Render.CurrentModelviewMatrix);

            foreach (var pass in ChunkRenderer.poolsByRenderPass)
            {
                foreach (var pool in pass)
                {
                    pool.Render(capi.World.Player.Entity.CameraPos, "origin");
                }
            }

            opq.Stop();

            capi.Render.GlPopMatrix();
            Platform.UnloadFrameBuffer(BufferRef);

            active?.Use();
        }
    }
    */
}
