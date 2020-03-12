using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API;
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
        public float rotateX { get; set; } = 0;
        public float rotateY { get; set; } = 0;
        public float rotateZ { get; set; } = 0;

        public float scaleX { get; set; } = 1;
        public float scaleY { get; set; } = 1;
        public float scaleZ { get; set; } = 1;

        public Vec3f rot { get => new Vec3f(rotateX, rotateY, rotateZ); }
        public Vec3f scale { get => new Vec3f(scaleX, scaleY, scaleZ); }
    }

    public class BlockCustomMesh : Block
    {
        public CustomMesh customMesh;
        public MeshData mesh;
        public MeshRef meshRef;
        public LoadCustomModels customModels { get => api.ModLoader.GetModSystem<LoadCustomModels>(); }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side.IsClient())
            {
                customMesh = Attributes["customMesh"].AsObject<CustomMesh>();
                customMesh.Texture.Bake(api.Assets);
                mesh = customModels.meshes[customMesh.fullPath].Translate(0.5f, 0.5f, 0.5f);
                mesh = customMesh.Texture != null ? mesh.WithTexPos((api as ICoreClientAPI).BlockTextureAtlas[customMesh.Texture.Base]) : mesh;
                meshRef = (api as ICoreClientAPI).Render.UploadMesh(mesh);
            }
        }

        public override void OnJsonTesselation(ref MeshData sourceMesh, BlockPos pos, int[] chunkExtIds, ushort[] chunkLightExt, int extIndex3d)
        {
            sourceMesh.Clear();
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            renderinfo.ModelRef = meshRef;
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
        {
            int[] rndColors = capi.BlockTextureAtlas[customMesh.Texture.Base].RndColors;

            return rndColors[(int)Math.Round(capi.World.Rand.NextDouble() * (rndColors.Length - 1))];
        }
    }

    public class BEBehaviorCustomMesh : BlockEntityBehavior
    {
        ICoreClientAPI capi;
        MeshRenderer myRenderer;

        BlockCustomMesh blockCustomMesh { get => Blockentity.Block as BlockCustomMesh; }
        public BEBehaviorCustomMesh(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            var c = blockCustomMesh.customMesh;
            capi = api as ICoreClientAPI;

            if (capi != null) myRenderer = new MeshRenderer(capi, Blockentity.Pos, c.fullPath, c.rot, c.scale, out bool failed, blockCustomMesh.meshRef);
            capi?.Event.RegisterRenderer(myRenderer, EnumRenderStage.Opaque);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            OnBlockUnloaded();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            capi?.Event.UnregisterRenderer(myRenderer, EnumRenderStage.Opaque);
        }
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

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, AssetLocation location, Vec3f rotation, Vec3f scale, out bool failed, MeshRef meshRef = null)
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
                this.meshRef = meshRef ?? capi.Render.UploadMesh(mesh);
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
            
            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.NormalShaded = 1;
            if (models.gltfTextures.TryGetValue(location, out TextureAtlasPosition tex))
            {
                prog.Tex2D = tex.atlasTextureId;
            }
            else prog.Tex2D = 0;

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
        }
    }
}