using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CustomMeshMod
{
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
                customMesh.Texture?.Bake(api.Assets);
                mesh = customModels.meshes[customMesh.FullPath].Clone().Translate(0.5f, 0.5f, 0.5f).Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 
                    GameMath.DEG2RAD * customMesh.RotateX, 
                    GameMath.DEG2RAD * customMesh.RotateY, 
                    GameMath.DEG2RAD * customMesh.RotateZ
                    );
                mesh = customMesh.Texture != null ? 
                    mesh.WithTexPos((api as ICoreClientAPI).BlockTextureAtlas[customMesh.Texture.Base]) : 
                    mesh.WithTexPos(customModels.customMeshTextures[customMesh.FullPath]);
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
            int[] rndColors = new int[32];

            if (customMesh.Texture != null)
            {
                rndColors = capi.BlockTextureAtlas[customMesh.Texture.Base].RndColors;
            }
            else
            {
                rndColors = customModels.customMeshTextures[customMesh.FullPath].RndColors;
            }

            return rndColors[(int)Math.Round(capi.World.Rand.NextDouble() * (rndColors.Length - 1))];
        }
    }
}