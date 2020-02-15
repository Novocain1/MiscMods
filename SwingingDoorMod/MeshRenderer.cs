using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace SwingingDoor
{
    public enum MeshType
    {
        gltf, obj, meshdata
    }

    public class CustomMesh
    {
        public string Base { get; set; } = "";
        public MeshType meshType { get; set; }
        public CompositeTexture Texture { get; set; }
        public AssetLocation fullPath { get => new AssetLocation(Base + "." + (meshType == MeshType.meshdata ? "json" : Enum.GetName(typeof(MeshType), meshType))); }
    }

    public class BlockCustomMesh : Block
    {
        public LoadCustomModels customModels { get => api.ModLoader.GetModSystem<LoadCustomModels>(); }
        public override void OnJsonTesselation(ref MeshData sourceMesh, BlockPos pos, int[] chunkExtIds, ushort[] chunkLightExt, int extIndex3d)
        {
            var cM = Attributes["customMesh"].AsObject<CustomMesh>();
            sourceMesh = customModels.meshes[cM.fullPath].WithTexPos((api as ICoreClientAPI).BlockTextureAtlas[cM.Texture.Base]);

            base.OnJsonTesselation(ref sourceMesh, pos, chunkExtIds, chunkLightExt, extIndex3d);
        }
    }

    public class MeshRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        private MeshRef meshRef;
        public Matrixf ModelMat = new Matrixf();
        public LoadCustomModels models { get => capi.ModLoader.GetModSystem<LoadCustomModels>(); }
        public bool shouldRender;
        AssetLocation location;
        Vec3f rotation;
        Vec3f scale;

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, AssetLocation location, Vec3f rotation, Vec3f scale, out bool failed)
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
                if (models.gltfTextures.TryGetValue(location, out TextureAtlasPosition tPos))
                {
                    mesh = mesh.WithTexPos(tPos);
                }
                meshRef = capi.Render.UploadMesh(mesh);
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
            if (meshRef == null) return;
            IRenderAPI render = capi.Render;
            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
            render.GlToggleBlend(true, EnumBlendMode.Standard);
            
            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.NormalShaded = 0;
            if (models.gltfTextures.TryGetValue(location, out TextureAtlasPosition tex))
            {
                prog.Tex2D = tex.atlasTextureId;
            }
            else prog.Tex2D = 0;

            prog.ModelMatrix = ModelMat.Identity()
                .Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z)
                .Translate(0.5, 0.5, 0.5)
                .RotateDeg(rotation)
                .Scale(scale.X, scale.Y, scale.Z)
                .Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;

            capi.Render.RenderMesh(meshRef);
            prog.Stop();
        }
    }
}