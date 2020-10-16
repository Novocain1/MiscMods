using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    class HudElementFloatyDamage : HudElement
    {
        float expiryTime = 2.0f;
        Vec3d pos;
        CairoFont font;
        double[] color;

        public override bool ShouldReceiveMouseEvents() => false;

        public HudElementFloatyDamage(ICoreClientAPI capi, double damage, Vec3d pos) : base(capi)
        {
            this.pos = pos.Clone();

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialogAtPos(0.0);
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, 250, 50);
            
            font = CairoFont.WhiteDetailText();

            color = new double[4];
            color[3] = 1.0;
            color[0] = damage > 0 ? 1.0 : 0.0;
            color[1] = damage > 0 ? 0.0 : 1.0;
            font.Color = color;

            string dmg = Math.Abs(damage).ToString("F3");

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0).WithWeight(Cairo.FontWeight.Bold).WithFontSize(15);

            SingleComposer = capi.Gui
                .CreateCompo("floatyDmg" + damage + capi.Gui.OpenedGuis.Count + 1 + GetHashCode(), dialogBounds)
                .AddDynamicText(dmg, font, EnumTextOrientation.Center, textBounds, "text")
                .Compose();

            SingleComposer.Bounds.Alignment = EnumDialogArea.None;
            SingleComposer.Bounds.fixedOffsetX = 0;
            SingleComposer.Bounds.fixedOffsetY = 0;
            SingleComposer.Bounds.absMarginX = 0;
            SingleComposer.Bounds.absMarginY = 0;

            TryOpen();
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            pos.Y += deltaTime / 1.5;
            var dynText = SingleComposer.GetDynamicText("text");

            Vec3d projectedPos = MatrixToolsd.Project(pos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
            if (projectedPos.Z < 0) dynText.SetNewText("");
            
            SingleComposer.Bounds.absFixedX = projectedPos.X - SingleComposer.Bounds.OuterWidth / 2;
            SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - projectedPos.Y - SingleComposer.Bounds.OuterHeight;

            dynText.Font = font.WithColor(new double[] { color[0], color[1], color[2], expiryTime / 2.0}).WithStroke(new double[] { 0.0, 0.0, 0.0, expiryTime / 2.0 }, 1.0);
            dynText.RecomposeText();

            expiryTime -= deltaTime;
            if (expiryTime < 0)
            {
                TryClose();
                Dispose();
            }
        }
    }
}
