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
        Flat, Smooth
    }

    public class MeshRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        private MeshRef meshRef;
        public Matrixf ModelMat = new Matrixf();
        public LoadCustomModels models { get => capi.ModLoader.GetModSystem<LoadCustomModels>(); }
        AssetLocation location;
        Vec3f rotation;
        Vec3f scale;
        EnumNormalShading shading;
        TextureMagFilter interpolation;
        bool backfaceCulling;

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, AssetLocation location, Vec3f rotation, Vec3f scale, out bool failed, MeshRef meshRef = null, EnumNormalShading shading = EnumNormalShading.Flat, bool backfaceCulling = true, TextureMagFilter interpolation = TextureMagFilter.Nearest)
        {
            failed = false;
            try
            {
                this.capi = capi;
                this.pos = pos;
                this.rotation = rotation;
                this.location = location;
                this.scale = scale;
                MeshData mesh = models.meshes[location];
                if (models.customMeshTextures.TryGetValue(location, out TextureAtlasPosition tPos))
                {
                    mesh = mesh.WithTexPos(tPos);
                }
                this.meshRef = meshRef ?? capi.Render.UploadMesh(mesh);
                this.shading = shading;
                this.backfaceCulling = backfaceCulling;
                this.interpolation = interpolation;
            }
            catch (Exception)
            {
                failed = true;
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
            if (backfaceCulling) render.GlEnableCullFace();

            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.NormalShaded = 1;
            
            if (models.customMeshTextures.TryGetValue(location, out TextureAtlasPosition tex))
            {
                prog.Tex2D = tex.atlasTextureId;
            }
            else prog.Tex2D = 0;
            if (interpolation != TextureMagFilter.Nearest) GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)interpolation);

            prog.Uniform("shading", (int)shading);

            prog.ModelMatrix = ModelMat.Identity()
                .Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z)
                .RotateDeg(rotation)
                .Scale(scale.X, scale.Y, scale.Z)
                .Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;
            capi.Render.RenderMesh(meshRef);
            prog.Stop();
            activeShader?.Use();
            if (backfaceCulling) render.GlDisableCullFace();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
    }
}