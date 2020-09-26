using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSHUD
{
    class HudElementWaypoint : HudElement
    {
        public Vec3d waypointPos { get => config.PerBlockWaypoints ? absolutePos.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0) : absolutePos; }
        public Vec3d absolutePos;
        public long id;
        public string DialogTitle;
        public string dialogText = "";
        public double distance = 0;
        public int waypointID;
        public VSHUDConfig config;
        public WaypointRelative waypoint;
        FloatyWaypoints system { get => capi.ModLoader.GetModSystem<FloatyWaypoints>(); }
        public override bool Focused => false;
        GuiDialogEditWayPoint waypointEditDialog;

        public Dictionary<string, LoadedTexture> texturesByIcon { get => system.texturesByIcon; }
        public static MeshRef quadModel;
        private Matrixf mvMat = new Matrixf();
        CairoFont font;
        public bool isAligned;

        public float ZDepth { get; set; }
        public override float ZSize => 0.00001f;
        public double DistanceFromPlayer { get => capi.World.Player.Entity.Pos.DistanceTo(waypointPos); }
        double[] dColor { get => ColorUtil.ToRGBADoubles(waypoint.OwnColor); }

        public HudElementWaypoint(ICoreClientAPI capi, WaypointRelative waypoint) : base(capi)
        {
            this.waypoint = waypoint;
            DialogTitle = waypoint.Title;
            absolutePos = waypoint.Position.Clone();
            this.waypointID = waypoint.Index;
            config = capi.ModLoader.GetModSystem<FloatyWaypoints>().Config;
            waypointEditDialog = new GuiDialogEditWayPoint(capi, waypoint.OwnWaypoint, waypointID);
            
            waypointEditDialog.OnClosed += () =>
            {
                capi.Event.RegisterCallback(dt => {
                    SingleComposer.GetDynamicText("text").Font.Color = dColor;
                    SingleComposer.ReCompose();
                }, 100);
            };
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialogAtPos(0.0);
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, 250, 50);

            font = CairoFont.WhiteSmallText();
            font.Color = dColor;

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0).WithWeight(Cairo.FontWeight.Bold).WithFontSize(15);

            SingleComposer = capi.Gui
                .CreateCompo(DialogTitle + capi.Gui.OpenedGuis.Count + 1, dialogBounds)
                .AddDynamicText("", font, EnumTextOrientation.Center, textBounds, "text")
                .Compose()
            ;

            SingleComposer.Bounds.Alignment = EnumDialogArea.None;
            SingleComposer.Bounds.fixedOffsetX = 0;
            SingleComposer.Bounds.fixedOffsetY = 0;
            SingleComposer.Bounds.absMarginX = 0;
            SingleComposer.Bounds.absMarginY = 0;

            WaypointTextUpdateSystem.TextTasks.Push(this);
        }

        public void RegisterDialogUpdate()
        {
            id = capi.Event.RegisterGameTickListener(dt =>
            {
                WaypointTextUpdateSystem.TextTasks.Push(this);
            }, 30);
        }

        protected virtual double FloatyDialogPosition => 0.75;
        protected virtual double FloatyDialogAlign => 0.75;
        public double order;
        public override double DrawOrder => order;

        public override bool ShouldReceiveMouseEvents() => false;

        public override void OnRenderGUI(float deltaTime)
        {
            if (!IsOpened() || waypoint.OwnWaypoint == null) return;
            var dynText = SingleComposer.GetDynamicText("text");

            ElementBounds bounds = SingleComposer.GetDynamicText("text").Bounds;

            VSHUDConfig config = capi.ModLoader.GetModSystem<ConfigLoader>().Config;
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

            double yBounds = (SingleComposer.Bounds.absFixedY / capi.Render.FrameHeight) + 0.025;
            double xBounds = (SingleComposer.Bounds.absFixedX / capi.Render.FrameWidth) + 0.065;

            isAligned = ((yBounds > 0.52 && yBounds < 0.54) && (xBounds > 0.49 && xBounds < 0.51)) && !system.WaypointElements.Any(ui => ui.isAligned && ui != this);

            if (!isClamped && (isAligned || distance < config.TitleRange || dialogText.Contains("*") || waypoint.OwnWaypoint.Pinned)) dynText.SetNewText(dialogText);
            else dynText.SetNewText("");

            if (texturesByIcon != null)
            {
                IShaderProgram engineShader = capi.Render.GetEngineShader(EnumShaderProgram.Gui);
                Vec4f newColor = new Vec4f();
                ColorUtil.ToRGBAVec4f(waypoint.OwnColor, ref newColor);

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
            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
            capi.Event.UnregisterGameTickListener(id);
            SingleComposer.Dispose();
        }

        public override bool UnregisterOnClose => true;

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            RegisterDialogUpdate();
        }

        public void OpenEditDialog()
        {
            waypointEditDialog.ignoreNextKeyPress = true;
            waypointEditDialog.TryOpen();
            waypointEditDialog.Focus();
        }
    }
}
