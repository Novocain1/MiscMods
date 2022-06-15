using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace VSHUD
{
    public class HudElementWaypoint : HudElement
    {
        public bool Dirty { get; set; } = false;

        public void MarkDirty() => Dirty = true;

        public Vec3d waypointPos { get => config.PerBlockWaypoints ? absolutePos.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0).Add(0.5) : absolutePos; }
        public Vec3d absolutePos { get => waypoint.Position.Clone(); }
        public string DialogTitle { get; set; }
        public string dialogText = "";
        public double distance = 0;
        public int waypointID { get => waypoint.Index; }
        public VSHUDConfig config;
        public WaypointRelative waypoint;
        public override bool Focused => false;
        GuiDialogEditWayPoint waypointEditDialog;

        public static Dictionary<string, LoadedTexture> texturesByIcon { get => WaypointUtils.texturesByIcon; }
        public static MeshRef quadModel;
        public static MeshRef pillar;
        
        public PillarRenderer renderer;

        private Matrixf mvMat = new Matrixf();
        CairoFont font;
        public bool isAligned;
        public bool displayText;

        public float ZDepth { get; set; }
        public override float ZSize => 0.00001f;
        public double DistanceFromPlayer { get => capi.World.Player.Entity.Pos.DistanceTo(waypointPos); }
        public double[] dColor { get => ColorUtil.ToRGBADoubles(waypoint.OwnColor); }
        
        public bool Closeable { get => opened && !ShouldBeVisible; }
        public bool Openable { get => !opened && ShouldBeVisible; }
        
        WaypointUtils utils;

        public bool ShouldBeVisible { get => 
                (distance <= config.DotRange && !ColorCheck) 
                || DialogTitle.Contains("*") || waypoint.OwnWaypoint.Pinned; }

        public bool ColorCheck { get => config.DisabledColors.Contains(waypoint.Color); }

        public void UpdateEditDialog()
        {
            waypointEditDialog?.Dispose();

            waypointEditDialog = new GuiDialogEditWayPoint(capi, waypoint.OwnWaypoint, waypointID);

            waypointEditDialog.OnClosed += () =>
            {
                Dirty = true;
            };
        }

        public HudElementWaypoint(ICoreClientAPI capi, WaypointRelative waypoint) : base(capi)
        {
            this.waypoint = waypoint;
            DialogTitle = waypoint.Title;

            utils = capi.ModLoader.GetModSystem<WaypointUtils>();
            config = utils.Config;
            UpdateEditDialog();

            renderer = new PillarRenderer(capi, this);

            capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialogAtPos(0.0);
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, 250, 50);

            font = CairoFont.WhiteSmallText();
            font.Color = dColor;

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0).WithWeight(Cairo.FontWeight.Bold).WithFontSize(15).WithOrientation(EnumTextOrientation.Center);

            SingleComposer = capi.Gui
                .CreateCompo(DialogTitle + capi.Gui.OpenedGuis.Count + 1, dialogBounds)
                .AddDynamicText("", font, textBounds, "text")
                .Compose()
            ;

            SingleComposer.Bounds.Alignment = EnumDialogArea.None;
            SingleComposer.Bounds.fixedOffsetX = 0;
            SingleComposer.Bounds.fixedOffsetY = 0;
            SingleComposer.Bounds.absMarginX = 0;
            SingleComposer.Bounds.absMarginY = 0;


            utils.textUpdateSystem.EnqueueIfNotAlready(this);
        }

        protected virtual double FloatyDialogPosition => 0.75;
        protected virtual double FloatyDialogAlign => 0.75;
        public override double DrawOrder => 0;

        public override bool ShouldReceiveMouseEvents() => false;

        public override void OnRenderGUI(float deltaTime)
        {
            if (!IsOpened() || waypoint.OwnWaypoint == null) return;
            distance = capi.World.Player.Entity.Pos.RoundedDistanceTo(waypointPos, 3);

            var dynText = SingleComposer.GetDynamicText("text");
            dynText.Font.Color = dColor;

            Vec4f newColor = new Vec4f();
            ColorUtil.ToRGBAVec4f(waypoint.OwnColor, ref newColor);

            float h = isAligned ? 2.0f : 1.0f;

            newColor.Mul(new Vec4f(h, h, h, 1.0f));

            ElementBounds bounds = SingleComposer.GetDynamicText("text").Bounds;

            Vec3d pos = MatrixToolsd.Project(waypointPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);

            double[] clamps = new double[]
            {
                capi.Render.FrameWidth * 0.007, capi.Render.FrameWidth * 1.001,
                capi.Render.FrameHeight * -0.05, capi.Render.FrameHeight * 0.938
            };

            if (dialogText.Contains("*") || waypoint.OwnWaypoint.Pinned)
            {
                pos.X = GameMath.Clamp(pos.X, clamps[0], clamps[1]);
                pos.Y = GameMath.Clamp(pos.Y, clamps[2], clamps[3]);
            }

            bool isClamped = pos.X == clamps[0] || pos.X == clamps[1] || pos.Y == clamps[2] || pos.Y == clamps[3];

            if (pos.Z < 0 || (distance > config.DotRange && (!dialogText.Contains("*") || waypoint.OwnWaypoint.Pinned)))
            {
                dynText.SetNewText("");
                SingleComposer.Dispose();
                return;
            }
            else
            {
                SingleComposer.Compose();
            }

            SingleComposer.Bounds.absFixedX = pos.X - SingleComposer.Bounds.OuterWidth / 2;
            SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - pos.Y - SingleComposer.Bounds.OuterHeight;
            
            double xPos = (double)capi.Input.MouseX / capi.Render.FrameWidth;
            double yPos = (double)capi.Input.MouseY / capi.Render.FrameHeight;
            double fX = (SingleComposer.Bounds.absFixedX + (SingleComposer.Bounds.InnerWidth / 2)) / capi.Render.FrameWidth;
            double fY = SingleComposer.Bounds.absFixedY / capi.Render.FrameHeight - 0.01;

            bool alignTest = (xPos < fX + 0.01 && xPos > fX - 0.01 && yPos < fY + 0.01 && yPos > fY - 0.01);
            
            isAligned = alignTest && !FloatyWaypointManagement.WaypointElements.Any(ui => ui.isAligned && ui != this);
            displayText = !isClamped && (isAligned || distance < config.TitleRange || dialogText.Contains("*") || waypoint.OwnWaypoint.Pinned);
            
            if (displayText) 
                dynText.SetNewText(dialogText);

            else dynText.SetNewText("");

            if (texturesByIcon != null)
            {
                IShaderProgram engineShader = capi.Render.GetEngineShader(EnumShaderProgram.Gui);

                engineShader.Uniform("rgbaIn", newColor);
                engineShader.Uniform("extraGlow", 0);
                engineShader.Uniform("applyColor", 0);
                engineShader.Uniform("noTexture", 0.0f);
                float scale = isAligned || isClamped ? 0.8f : 0.5f;

                LoadedTexture loadedTexture;
                if (!texturesByIcon.TryGetValue(waypoint.OwnWaypoint.Icon, out loadedTexture)) return;
                engineShader.BindTexture2D("tex2d", texturesByIcon[waypoint.Icon].TextureId, 0);
                mvMat.Set(capi.Render.CurrentModelviewMatrix)
                    .Translate(SingleComposer.Bounds.absFixedX + 125, SingleComposer.Bounds.absFixedY, ZSize)
                    .Scale(loadedTexture.Width, loadedTexture.Height, 0.0f)
                    .Scale(scale, scale, 0.0f);
                engineShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
                engineShader.UniformMatrix("modelViewMatrix", mvMat.Values);
                capi.Render.RenderMesh(quadModel);
            }

            base.OnRenderGUI(deltaTime);
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
        }

        public override void Dispose()
        {
            base.Dispose();
            SingleComposer?.Dispose();
            waypointEditDialog.Dispose();
            capi.Event.UnregisterRenderer(renderer, EnumRenderStage.Opaque);
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
        }

        public void OpenEditDialog()
        {
            waypointEditDialog.ignoreNextKeyPress = true;
            waypointEditDialog.TryOpen();
            waypointEditDialog.Focus();
        }
    }

    public class PillarRenderer : IRenderer
    {
        ICoreClientAPI capi;
        WaypointRelative waypoint { get => ownDialog.waypoint; }
        HudElementWaypoint ownDialog;

        public PillarRenderer(ICoreClientAPI capi, HudElementWaypoint ownDialog)
        {
            this.capi = capi;
            this.ownDialog = ownDialog;
        }

        public double RenderOrder => 0.5;
        public VSHUDConfig config { get => ConfigLoader.Config; }
        public int RenderRange => 24;

        public MeshRef pillar { get => HudElementWaypoint.pillar; }
        private Matrixf mvMat = new Matrixf();
        float counter = 0;

        public void Dispose()
        {
            
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (config.ShowPillars && ownDialog.IsOpened() && ownDialog.ShouldBeVisible && !capi.HideGuis)
            {
                counter += deltaTime;
                Vec3d pos = config.PerBlockWaypoints ? waypoint.Position.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0).Add(0.5) : waypoint.Position;
                IStandardShaderProgram prog = capi.Render.PreparedStandardShader((int)pos.X, (int)pos.Y, (int)pos.Z);
                Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
                
                capi.Render.GlToggleBlend(true, EnumBlendMode.Standard);
                int texID = capi.Render.GetOrLoadTexture(new AssetLocation("block/creative/col78.png"));

                Vec4f newColor = new Vec4f();
                ColorUtil.ToRGBAVec4f(waypoint.OwnColor, ref newColor);
                float dist = (float)(waypoint.DistanceFromPlayer ?? 1.0f);
                float scale = GameMath.Max(dist / ClientSettings.FieldOfView, 1.0f);

                float h = ownDialog.isAligned ? 2.0f : 1.0f;

                newColor.Mul(new Vec4f(h, h, h, 1.0f));

                newColor.A = 0.9f;
                prog.NormalShaded = 0;
                prog.RgbaGlowIn = newColor;
                prog.ExtraGlow = 64;
                
                prog.RgbaTint = newColor;

                prog.Tex2D = texID;
                prog.ModelMatrix = mvMat.Identity().Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z)
                    .Scale(scale, scale, scale)
                    .RotateYDeg(counter * 50 % 360.0f).Values;
                prog.ViewMatrix = capi.Render.CameraMatrixOriginf;
                prog.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
                //prog.BindTexture2D("tex", 0, 0);

                capi.Render.RenderMesh(pillar);

                prog.Stop();
            }
        }
    }
}
