using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace DeathWaypoints
{
    class AutoDeathWaypoints : ModSystem
    {
        long id;
        long id2;
        ICoreClientAPI capi;
        GuiDialogFloatyWaypoints floatyPoints;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            id = api.World.RegisterGameTickListener(dt =>
            {
                EntityPlayer player = api.World.Player.Entity;

                if (player != null)
                {
                    player.WatchedAttributes.RegisterModifiedListener("entityDead", () =>
                    {
                        if (player.WatchedAttributes["entityDead"].ToString() == "1")
                        {
                            api.SendChatMessage("/waypoint add #" + ColorStuff.RandomHexColorVClamp(api, 0.50, 0.80) + " Player Death Waypoint");
                        }
                    });

                    capi.Input.RegisterHotKey("viewwaypoints", "View Waypoints", GlKeys.U, HotkeyType.GUIOrOtherControls);
                    capi.Input.SetHotKeyHandler("viewwaypoints", ViewWaypoints);
                    double time = capi.World.Calendar.TotalHours + 0.2;

                    id2 = api.World.RegisterGameTickListener(dt2 => 
                    {
                        WorldMapManager modMapManager = capi.ModLoader.GetModSystem("Vintagestory.GameContent.WorldMapManager") as WorldMapManager;
                        WaypointMapLayer layer = modMapManager.MapLayers.Single(ml => ml is WaypointMapLayer) as WaypointMapLayer;
                        List<Waypoint> waypoints = HaxorMan.GetInstanceField(typeof(WaypointMapLayer), layer, "ownWaypoints") as List<Waypoint>;
                        if (waypoints.Count > 0 || capi.World.Calendar.TotalHours > time)
                        {
                            if (capi.Settings.Bool["floatywaypoints"]) OpenWaypoints();
                            api.World.UnregisterGameTickListener(id2);
                        }
                        
                    }, 500);


                    api.World.UnregisterGameTickListener(id);
                }
            }, 500);

        }

        List<GuiDialogFloatyWaypoints> guiDialogs = new List<GuiDialogFloatyWaypoints>();

        private bool ViewWaypoints(KeyCombination t1)
        {
            if (guiDialogs.Count > 0)
            {
                for (int i = 0; i < guiDialogs.Count; i++)
                {
                    guiDialogs[i].TryClose();
                }
                guiDialogs.Clear();
            }
            else
            {
                OpenWaypoints();
            }
            return true;
        }

        public void OpenWaypoints()
        {
            WorldMapManager modMapManager = capi.ModLoader.GetModSystem("Vintagestory.GameContent.WorldMapManager") as WorldMapManager;
            WaypointMapLayer layer = modMapManager.MapLayers.Single(ml => ml is WaypointMapLayer) as WaypointMapLayer;
            List<Waypoint> waypoints = HaxorMan.GetInstanceField(typeof(WaypointMapLayer), layer, "ownWaypoints") as List<Waypoint>;

            guiDialogs = new List<GuiDialogFloatyWaypoints>();

            for (int i = 0; i < waypoints.Count; i++)
            {
                string text = waypoints[i].Title != null ? "Waypoint: " + waypoints[i].Title : "Waypoint: ";
                int color = waypoints[i].Color;

                floatyPoints = new GuiDialogFloatyWaypoints(text, capi, waypoints[i].Position, color);

                floatyPoints.OnOwnPlayerDataReceived();
                if (floatyPoints.TryOpen())
                {
                    guiDialogs.Add(floatyPoints);
                }
            }
        }
    }

    public class GuiDialogFloatyWaypoints : HudElement
    {
        Vec3d waypointPos;
        string DialogTitle;
        int color;

        public GuiDialogFloatyWaypoints(string DialogTitle, ICoreClientAPI capi, Vec3d waypointPos, int color) : base(capi)
        {
            this.DialogTitle = DialogTitle;
            this.waypointPos = waypointPos;
            this.color = color;
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialogAtPos(0.0);
            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, 250, 50);
            
            double[] dColor = ColorUtil.ToRGBADoubles(color);

            CairoFont font = CairoFont.WhiteSmallText();
            font.Color = dColor;
            font = font.WithStroke(new double[] { 0, 0, 0, 1 }, 0.5);

            SingleComposer = capi.Gui
                .CreateCompo(DialogTitle + capi.Gui.OpenedGuis.Count + 1, dialogBounds)
                .AddDynamicText("", font, EnumTextOrientation.Center, textBounds, "text")
                .Compose()
            ;
            if (capi.Settings.Bool["floatywaypoints"]) TryOpen();
        }

        protected virtual double FloatyDialogPosition => 0.75;
        protected virtual double FloatyDialogAlign => 0.75;

        public void RenderWaypoint()
        {
            if (!capi.Settings.Bool["floatywaypoints"]) return;
            EntityPlayer entityPlayer = capi.World.Player.Entity;
            Vec3d aboveHeadPos = new Vec3d(waypointPos.X + 0.5, waypointPos.Y + FloatyDialogPosition, waypointPos.Z + 0.5);
            Vec3d pos = MatrixToolsd.Project(aboveHeadPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
            ElementBounds bounds = ElementBounds.Empty;
            if (pos.Z < 0) return;

            SingleComposer.Bounds.Alignment = EnumDialogArea.None;
            SingleComposer.Bounds.fixedOffsetX = 0;
            SingleComposer.Bounds.fixedOffsetY = 0;
            SingleComposer.Bounds.absFixedX = pos.X - SingleComposer.Bounds.OuterWidth / 2;
            SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - pos.Y - SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;
            SingleComposer.Bounds.absMarginX = 0;
            SingleComposer.Bounds.absMarginY = 0;

            if (Math.Sqrt(entityPlayer.Pos.SquareDistanceTo(waypointPos)) > 500)
            {
                SingleComposer.GetDynamicText("text").SetNewText("\n\u2022");
                return;
            }
            else
            {
                double distance = Math.Round(Math.Sqrt(entityPlayer.Pos.SquareDistanceTo(waypointPos)), 3);
                SingleComposer.GetDynamicText("text").SetNewText(DialogTitle + " " + distance + "m" + "\n\u2022");
            }


        }

        public override void OnRenderGUI(float deltaTime)
        {
            RenderWaypoint();
            base.OnRenderGUI(deltaTime);
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            capi.Settings.Bool["floatywaypoints"] = false;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            capi.Settings.Bool["floatywaypoints"] = true;
        }
    }

    class ColorStuff
    {
        public static int RandomColor(ICoreAPI api) => ColorUtil.HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255)
            );

        public static string RandomHexColor(ICoreAPI api) => RandomColor(api).ToString("X");
        public static string RandomHexColorVClamp(ICoreAPI api, double min, double max) => ClampedRandomColorValue(api, min, max).ToString("X");

        public static int ClampedRandomColorValue(ICoreAPI api, double min, double max)
        {
            return ColorUtil.HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(GameMath.Clamp(api.World.Rand.NextDouble(), min, max) * 255)
            );
        }
    }

    class HaxorMan
    {
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
    }
}
