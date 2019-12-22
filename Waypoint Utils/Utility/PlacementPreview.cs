using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace VSHUD
{
    class PlacementPreview : ModSystem
    {
        PlacementRenderer renderer;
        public override void StartClientSide(ICoreClientAPI api)
        {
            renderer = new PlacementRenderer(api);
            api.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
            api.RegisterCommand("pconfig", "Config Placement Preview System", "[enabled|textured]", (id, args) =>
            {
                WaypointUtilConfig config = api.ModLoader.GetModSystem<WaypointUtilSystem>().Config;
                string arg = args.PopWord();
                switch (arg)
                {
                    case "enabled":
                        config.PRShow = !config.PRShow;
                        break;
                    case "tinted":
                        config.PRTex = !config.PRTex;
                        break;
                    default:
                        break;
                }
                api.ModLoader.GetModSystem<ConfigLoader>().SaveConfig();
            });
            api.Event.LevelFinalize += () => api.Shader.ReloadShaders();
        }
    }

    class PlacementRenderer : IRenderer
    {
        ICoreClientAPI capi;
        IClientPlayer player { get => capi?.World?.Player; }
        ItemStack invStack { get => player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;  }
        Block invBlock { get => invStack?.Block; }
        BlockSelection playerSelection { get => player?.CurrentBlockSelection; }
        BlockPos pos { get => playerSelection?.Position; }
        Vec3d camPos { get => player?.Entity.CameraPos; }
        WaypointUtilConfig config { get => capi.ModLoader.GetModSystem<WaypointUtilSystem>().Config; }
        ShapeTesselatorManager tesselatormanager { get => capi.TesselatorManager as ShapeTesselatorManager; }
        bool shouldDispose = true;

        MeshRef mRef;
        IRenderAPI rpi;
        public Matrixf ModelMat = new Matrixf();
        Block toBlock;

        public PlacementPreviewHelper ph;

        public PlacementRenderer(ICoreClientAPI capi)
        {
            this.capi = capi;
            rpi = capi.Render;
            ph = new PlacementPreviewHelper();
        }

        public void UpdateBlockMesh(Block toBlock, BlockPos altPos)
        {
            if (toBlock == null) return;
            MeshData mesh;
            if (toBlock is BlockChisel && (mRef = capi.ModLoader.GetModSystem<ChiselBlockModelCache>().GetOrCreateMeshRef(invStack)) != null)
            {
                shouldDispose = false;
                return;
            }
            else if (toBlock.HasAlternates)
            {
                long alternateIndex = toBlock.RandomizeAxes == EnumRandomizeAxes.XYZ ?
                    GameMath.MurmurHash3Mod(altPos.X, altPos.Y, altPos.Z, tesselatormanager.altblockModelDatas[toBlock.Id].Length) : GameMath.MurmurHash3Mod(altPos.X, 0, altPos.Z, tesselatormanager.altblockModelDatas[toBlock.Id].Length);
                mesh = tesselatormanager.altblockModelDatas[toBlock.Id][alternateIndex];
            }
            else mesh = tesselatormanager.blockModelDatas[toBlock.Id];

            mesh.AddTintIndex(toBlock.TintIndex);
            if (mRef != null && shouldDispose) mRef.Dispose();
            shouldDispose = true;
            mRef = rpi.UploadMesh(mesh);
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            mRef?.Dispose();
        }

        public bool SneakCheck
        {
            get =>
                (
                invBlock?.HasBehavior<BlockBehaviorRightClickPickup>() ?? false ||
                invBlock is BlockMeal ||
                invBlock is BlockBucket
                )
                && !player.Entity.Controls.Sneak;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (invBlock == null || pos == null || !config.PRShow || SneakCheck) return;
            toBlock = ph.GetPlacedBlock(capi.World, player, invBlock, playerSelection);
            if (toBlock == null) return;
            BlockPos adjPos = playerSelection.GetRecommendedPos(capi, toBlock);

            UpdateBlockMesh(toBlock, adjPos);
            if (mRef == null) return;
            
            if (!capi.World.BlockAccessor.GetBlock(adjPos).IsReplacableBy(invBlock)) return;
            rpi.GlToggleBlend(true);
            if (toBlock is BlockPlant) rpi.GlDisableCullFace();
            Vec2f offset = adjPos.GetOffset(toBlock);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(adjPos.X, adjPos.Y, adjPos.Z);
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(adjPos.X - camPos.X, adjPos.Y - camPos.Y, adjPos.Z - camPos.Z)
                .Translate(offset.X, 0, offset.Y)
                .Values;
            
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            prog.RgbaTint = new Vec4f(1, 1, 1, 0.5f);
            if (!config.PRTex)
            {
                prog.Tex2dOverlay2D = capi.Render.GetOrLoadTexture(new AssetLocation("block/blue.png"));
                prog.OverlayOpacity = 0.5f;
            }
            prog.ExtraGlow = 255 / 2;
            
            rpi.RenderMesh(mRef);
            prog.Stop();
        }
    }
}
