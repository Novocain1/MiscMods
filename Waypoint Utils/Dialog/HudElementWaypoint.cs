using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace VSHUD
{
    class HudElementWaypoint : HudElement
    {
        public Vec3d waypointPos { get => config.PerBlockWaypoints ? absolutePos.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0) : absolutePos; }
        public Vec3d absolutePos;

        public string DialogTitle;
        public int color;
        public string dialogText = "";
        public double distance = 0;
        public int waypointID;
        public long id;
        WaypointUtilConfig config;
        Waypoint waypoint;
        WaypointUtilSystem system { get => capi.ModLoader.GetModSystem<WaypointUtilSystem>(); }

        public Dictionary<string, LoadedTexture> texturesByIcon { get => system.texturesByIcon; }
        public MeshRef quadModel;
        private Matrixf mvMat = new Matrixf();
        CairoFont font;
        public bool isAligned;

        public override float ZSize => 0.01f;

        public HudElementWaypoint(ICoreClientAPI capi, Waypoint waypoint, int waypointID) : base(capi)
        {
            this.waypoint = waypoint;
            DialogTitle = waypoint.Title;
            absolutePos = waypoint.Position.Clone();
            color = waypoint.Color;
            this.waypointID = waypointID;
            config = capi.ModLoader.GetModSystem<WaypointUtilSystem>().Config;
            quadModel = capi.Render.UploadMesh(QuadMeshUtil.GetQuad());
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialogAtPos(0.0);
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, 250, 50);

            double[] dColor = ColorUtil.ToRGBADoubles(color);

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

            UpdateDialog();
            id = capi.World.RegisterGameTickListener(dt => UpdateDialog(), 500 + capi.World.Rand.Next(0, 64));
        }

        public void UpdateDialog()
        {
            UpdateTitle();
            distance = capi.World.Player.Entity.Pos.RoundedDistanceTo(waypointPos, 3);
            bool km = distance >= 1000;

            dialogText = DialogTitle.UcFirst() + " " + (km ? Math.Round(distance / 1000, 3) : distance) + (km ? "km" : "m");
            order = (1.0 / distance) * 0.0001;
        }

        public void UpdateTitle()
        {
            string wp = config.WaypointPrefix ? "Waypoint: " : "";
            wp = config.WaypointID ? wp + "ID: " + waypointID + " | " : wp;
            DialogTitle = waypoint.Title != null ? wp + waypoint.Title : "Waypoint: ";
        }

        protected virtual double FloatyDialogPosition => 0.75;
        protected virtual double FloatyDialogAlign => 0.75;
        public double order;
        public override double DrawOrder => order;

        public override bool ShouldReceiveMouseEvents() => false;

        public override void OnRenderGUI(float deltaTime)
        {
            WaypointUtilConfig config = capi.ModLoader.GetModSystem<ConfigLoader>().Config;

            Vec3d aboveHeadPos = new Vec3d(waypointPos.X + 0.5, waypointPos.Y + FloatyDialogPosition, waypointPos.Z + 0.5);
            Vec3d pos = MatrixToolsd.Project(aboveHeadPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);

            if (pos.Z < 0 || (distance > config.DotRange && !dialogText.Contains("*")))
            {
                SingleComposer.GetDynamicText("text").SetNewText("");
                SingleComposer.Dispose();
                return;
            }
            else
            {
                SingleComposer.Compose();
            }

            SingleComposer.Bounds.absFixedX = pos.X - SingleComposer.Bounds.OuterWidth / 2;
            SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - pos.Y - SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;

            double yBounds = (SingleComposer.Bounds.absFixedY / capi.Render.FrameHeight) + 0.025;
            double xBounds = (SingleComposer.Bounds.absFixedX / capi.Render.FrameWidth) + 0.065;

            isAligned = ((yBounds > 0.49 && yBounds < 0.51) && (xBounds > 0.49 && xBounds < 0.51)) && !system.WaypointElements.Any(ui => ui.isAligned && ui != this);

            if (isAligned || distance < config.TitleRange || dialogText.Contains("*")) SingleComposer.GetDynamicText("text").SetNewText(dialogText);
            else SingleComposer.GetDynamicText("text").SetNewText("");

            if (texturesByIcon != null)
            {
                IShaderProgram engineShader = capi.Render.GetEngineShader(EnumShaderProgram.Gui);
                Vec4f newColor = new Vec4f();
                ColorUtil.ToRGBAVec4f(color, ref newColor);

                engineShader.Uniform("rgbaIn", newColor);
                engineShader.Uniform("extraGlow", 0);
                engineShader.Uniform("applyColor", 0);
                engineShader.Uniform("noTexture", 0.0f);
                float scale = isAligned ? 0.8f : 0.5f;

                LoadedTexture loadedTexture;
                if (!texturesByIcon.TryGetValue(waypoint.Icon, out loadedTexture)) return;
                engineShader.BindTexture2D("tex2d", texturesByIcon[waypoint.Icon].TextureId, 0);
                mvMat.Set(capi.Render.CurrentModelviewMatrix)
                    .Translate(SingleComposer.Bounds.absFixedX + 125, SingleComposer.Bounds.absFixedY + 30, pos.Z)
                    .Scale(loadedTexture.Width, loadedTexture.Height, 0.0f)
                    .Scale(scale, scale, 0.0f);
                engineShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
                engineShader.UniformMatrix("modelViewMatrix", mvMat.Values);
                capi.Render.RenderMesh(quadModel);
            }
            base.OnRenderGUI(deltaTime);
        }

        public override void Dispose()
        {
            base.Dispose();
            capi.World.UnregisterGameTickListener(id);
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
        }
    }
}
