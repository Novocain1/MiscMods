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
using Vintagestory.API.Datastructures;

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
                bool? enabled = args.PopBool();
                switch (arg)
                {
                    case "enabled":
                        config.PRShow = enabled ?? !config.PRShow;
                        api.ShowChatMessage("Block preview set to " + config.PRShow);
                        break;
                    case "tinted":
                        config.PRTint = enabled ?? !config.PRTint;
                        api.ShowChatMessage("Block preview tinting set to " + config.PRTint);
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
        Item invItem { get => invStack?.Item;  }
        Block invBlock { get => invStack?.Block ?? (invItem is ItemStone ? capi.World.GetBlock(invItem.CodeWithPath("loosestones-" + invItem.LastCodePart() + "-free")) : null); }
        BlockSelection playerSelection { get => player?.CurrentBlockSelection; }
        BlockPos pos { get => playerSelection?.Position; }
        Vec3d camPos { get => player?.Entity.CameraPos; }
        Vec3d playerPos { get => player?.Entity.Pos.XYZ;  }
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
            MealMeshCache meshCache = capi.ModLoader.GetModSystem<MealMeshCache>();

            if (toBlock is BlockMeal)
            {
                mRef = meshCache.GetOrCreateMealInContainerMeshRef(toBlock, ((BlockMeal)toBlock).GetCookingRecipe(capi.World, invStack), ((BlockMeal)toBlock).GetNonEmptyContents(capi.World, invStack));
                shouldDispose = false;
                return;
            }
            else if (toBlock is BlockCookedContainer)
            {
                mRef = meshCache.GetOrCreateMealInContainerMeshRef(toBlock, ((BlockCookedContainer)toBlock).GetCookingRecipe(capi.World, invStack), ((BlockCookedContainer)toBlock).GetNonEmptyContents(capi.World, invStack), new Vec3f(0.0f, 2.5f / 16f, 0.0f));
                shouldDispose = false;
                return;
            }
            else if (toBlock is BlockChisel)
            {
                mRef = capi.ModLoader.GetModSystem<ChiselBlockModelCache>().GetOrCreateMeshRef(invStack);
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

            if (toBlock.RandomizeRotations)
            {
                if (toBlock.BlockMaterial == EnumBlockMaterial.Leaves)
                {
                    int index = GameMath.MurmurHash3Mod(altPos.X, altPos.Y, altPos.Z, JsonTesselator.randomRotationsLeaves.Length);
                    mesh = mesh.Clone().MatrixTransform(JsonTesselator.randomRotMatricesLeaves[index]);
                }
                else
                {
                    int index = GameMath.MurmurHash3Mod(altPos.X, altPos.Y, altPos.Z, JsonTesselator.randomRotations.Length);
                    mesh = mesh.Clone().MatrixTransform(JsonTesselator.randomRotMatrices[index]);
                }
            }
            if (mRef != null && shouldDispose) mRef.Dispose();
            shouldDispose = true;
            MeshData rotMesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, toBlock.GetRotY(playerPos, playerSelection), 0);
            mRef = rpi.UploadMesh(rotMesh);
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
                (invBlock?.HasBehavior<BlockBehaviorRightClickPickup>() ?? false) ||
                invBlock is BlockMeal ||
                invBlock is BlockBucket ||
                invItem is ItemStone
                )
                && !player.Entity.Controls.Sneak;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (playerSelection == null || invBlock == null || pos == null || !config.PRShow || SneakCheck) return;
            playerSelection.Position = playerSelection.Position.GetBlock(capi).IsReplacableBy(invBlock) ? playerSelection.Position : playerSelection.Position.Offset(playerSelection.Face);

            toBlock = ph.GetPlacedBlock(capi.World, player, invBlock, playerSelection);
            if (toBlock == null) return;
            
            BlockPos adjPos = playerSelection.Position;

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
            
            if (!config.PRTint)
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
