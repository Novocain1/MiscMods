using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using OpenTK.Graphics.OpenGL;

namespace CustomMeshMod
{
    public enum EnumNormalShading
    {
        None, Flat, Smooth
    }

    public class MeshRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        public Matrixf ModelMat = new Matrixf();
        public LoadCustomModels models { get => capi.ModLoader.GetModSystem<LoadCustomModels>(); }
        CustomMesh customMesh;
        MeshRef meshRef;
        TextureAtlasPosition texPos;
        TextureAtlasPosition normalPos;
        TextureAtlasPosition pbrPos;

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, CustomMesh customMesh, MeshRef meshRef)
        {
            try
            {
                this.capi = capi;
                this.pos = pos;
                this.customMesh = customMesh;
                this.meshRef = meshRef;

                if (models.customMeshTextures.TryGetValue(customMesh.FullPath, out TextureAtlasPosition tPos))
                {
                    texPos = tPos;
                }
                if (models.customNormalTextures.TryGetValue(customMesh.FullPath, out tPos))
                {
                    normalPos = tPos;
                }
                if (models.customMeshPBRs.TryGetValue(customMesh.FullPath, out tPos))
                {
                    pbrPos = tPos;
                }
                
            }
            catch (Exception)
            {
                Dispose();
            }
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            meshRef?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IRenderAPI render = capi.Render;
            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
            render.GlToggleBlend(true, EnumBlendMode.Standard);
            IShaderProgram activeShader = render.CurrentActiveShader;
            if (meshRef == null || meshRef.Disposed) return;
            activeShader?.Stop();
            if (customMesh.BackFaceCulling) render.GlEnableCullFace();

            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.NormalShaded = 1;
            prog.Tex2D = texPos?.atlasTextureId ?? 0;

            if (customMesh.Interpolation != TextureMagFilter.Nearest) GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)customMesh.Interpolation);
            
            prog.Uniform("shading", (int)customMesh.NormalShading);

            prog.Uniform("baseUVin", new Vec2f(texPos?.x1 ?? 0, texPos?.y1 ?? 0));
            prog.Uniform("nrmUVin", new Vec2f(normalPos?.x1 ?? 0, normalPos?.y1 ?? 0));
            prog.Uniform("pbrUVin", new Vec2f(pbrPos?.x1 ?? 0, pbrPos?.y1 ?? 0));

            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z).Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;
            capi.Render.RenderMesh(meshRef);
            prog.Stop();
            activeShader?.Use();
            if (customMesh.BackFaceCulling) render.GlDisableCullFace();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
    }
}