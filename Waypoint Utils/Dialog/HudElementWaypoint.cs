﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace WaypointUtils
{
    class HudElementWaypoint : HudElement
    {
        public Vec3d waypointPos;
        public string DialogTitle;
        public int color;
        public string dialogText = "";
        public double distance = 0;
        public int waypointID;
        public long id;
        WaypointUtilConfig config;
        Waypoint waypoint;

        public HudElementWaypoint(ICoreClientAPI capi, Waypoint waypoint, int waypointID) : base(capi)
        {
            this.waypoint = waypoint;
            this.DialogTitle = waypoint.Title;
            this.waypointPos = waypoint.Position;
            this.color = waypoint.Color;
            this.waypointID = waypointID;
            config = capi.ModLoader.GetModSystem<WaypointUtilSystem>().Config;
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialogAtPos(0.0);
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, 250, 50);

            double[] dColor = ColorUtil.ToRGBADoubles(color);

            CairoFont font = CairoFont.WhiteSmallText();
            font.Color = dColor;

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0);

            SingleComposer = capi.Gui
                .CreateCompo(DialogTitle + capi.Gui.OpenedGuis.Count + 1, dialogBounds)
                .AddDynamicText("", font, EnumTextOrientation.Center, textBounds, "text")
                .Compose()
            ;

            UpdateDialog();
            id = capi.World.RegisterGameTickListener(dt => UpdateDialog(), 500 + capi.World.Rand.Next(0, 64));
        }

        public void UpdateDialog()
        {
            UpdateTitle();
            waypointPos = config.PerBlockWaypoints ? waypointPos.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0) : waypointPos;
            distance = capi.World.Player.Entity.Pos.RoundedDistanceTo(waypointPos, 3);
            dialogText = DialogTitle + " " + distance + "m" + "\n\u2022";
            order = 1.0 / distance;
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

            SingleComposer.Bounds.Alignment = EnumDialogArea.None;
            SingleComposer.Bounds.fixedOffsetX = 0;
            SingleComposer.Bounds.fixedOffsetY = 0;
            SingleComposer.Bounds.absFixedX = pos.X - SingleComposer.Bounds.OuterWidth / 2;
            SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - pos.Y - SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;
            SingleComposer.Bounds.absMarginX = 0;
            SingleComposer.Bounds.absMarginY = 0;

            double yBounds = (SingleComposer.Bounds.absFixedY / capi.Render.FrameHeight) + 0.025;
            double xBounds = (SingleComposer.Bounds.absFixedX / capi.Render.FrameWidth) + 0.065;

            bool isAligned = (yBounds > 0.49 && yBounds < 0.51) && (xBounds > 0.49 && xBounds < 0.51);

            if (isAligned || distance < config.TitleRange || dialogText.Contains("*")) SingleComposer.GetDynamicText("text").SetNewText(dialogText);
            else SingleComposer.GetDynamicText("text").SetNewText("\n\u2022");

            base.OnRenderGUI(deltaTime);
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            capi.World.UnregisterGameTickListener(id);
            Dispose();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
        }
    }
}
