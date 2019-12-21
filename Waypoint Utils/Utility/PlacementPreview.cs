using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

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
        Block invBlock { get => player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Block; }
        BlockSelection playerSelection { get => player?.CurrentBlockSelection; }
        BlockPos pos { get => playerSelection?.Position; }
        Vec3d camPos { get => player?.Entity.CameraPos; }
        WaypointUtilConfig config { get => capi.ModLoader.GetModSystem<WaypointUtilSystem>().Config; }
        MeshRef mRef;
        string lastCode;
        IRenderAPI rpi;
        public Matrixf ModelMat = new Matrixf();
        Block toBlock;

        public PlacementPreviewHelper ph;

        public PlacementRenderer(ICoreClientAPI capi)
        {
            this.capi = capi;
            rpi = capi.Render;
            ph = new PlacementPreviewHelper();
            capi.Event.RegisterGameTickListener(dt =>
            {
                if (invBlock == null || pos == null) return;
                toBlock = ph.GetPlacedBlock(capi.World, player, invBlock, playerSelection);
                if (toBlock?.Code?.ToString() != lastCode)
                {
                    UpdateBlockMesh(toBlock);
                    lastCode = toBlock?.Code?.ToString();
                }
            }, 30);
        }

        public void UpdateBlockMesh(Block toBlock)
        {
            if (toBlock == null) return;
            capi.Tesselator.TesselateBlock(toBlock, out MeshData mesh);
            mesh.AddTintIndex(toBlock.TintIndex);
            if (mRef != null) mRef.Dispose();
            mRef = rpi.UploadMesh(mesh);
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            mRef?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (invBlock == null || toBlock == null || pos == null || mRef == null || !config.PRShow) return;
            BlockPos adjPos = pos.Copy();
            switch (playerSelection.Face.Code)
            {
                case "up":
                    adjPos.Up();
                    break;
                case "down":
                    adjPos.Down();
                    break;
                case "north":
                    adjPos.West();
                    break;
                case "south":
                    adjPos.East();
                    break;
                case "east":
                    adjPos.North();
                    break;
                case "west":
                    adjPos.South();
                    break;
                default:
                    break;
            }
            if (!capi.World.BlockAccessor.GetBlock(adjPos).IsReplacableBy(invBlock)) return;
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(adjPos.X, adjPos.Y, adjPos.Z);
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(adjPos.X - camPos.X, adjPos.Y - camPos.Y, adjPos.Z - camPos.Z)
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
