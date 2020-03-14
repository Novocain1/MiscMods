using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CustomMeshMod
{
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
}