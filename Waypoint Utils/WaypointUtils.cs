using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Newtonsoft.Json;
using System.IO;

namespace WaypointUtils
{
    class WaypointUtilConfig
    {
        public double DotRange { get; set; } = 2000.0;
        public double TitleRange { get; set; } = 500.0;
        public bool PerBlockWaypoints { get; set; } = false;
        public int SetColorIndex { get; set; } = 0;
        public bool WaypointPrefix { get; set; } = true;
        public bool WaypointID { get; set; } = true;

        public bool LightLevels { get; set; } = false;
        public EnumLightLevelType LightLevelType { get; set; } = EnumLightLevelType.OnlyBlockLight;
        public int LightRadius { get; set; } = 8;
        public int MinLLID { get; set; } = 128;
        public float LightLevelAlpha { get; set; } = 0.8f;
        public int LightLevelRed { get; set; } = 8;
        public bool LUShowAbove { get; set; } = true;
        public bool LUSpawning { get; set; } = true;
        public bool LUOpaque { get; set; } = true;
    }

    class WaypointUtilSystem : ModSystem
    {
        long id;
        ICoreClientAPI capi;
        GuiDialogFloatyWaypoints floatyPoints;
        public static WaypointUtilConfig Config = new WaypointUtilConfig();

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            LoadConfig();
            WaypointFrontEnd frontEnd = new WaypointFrontEnd(capi);

            capi.Input.RegisterHotKey("viewwaypoints", "View Waypoints", GlKeys.U, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("viewwaypoints", ViewWaypoints);
            capi.Input.RegisterHotKey("culldeathwaypoints", "Cull Death Waypoints", GlKeys.O, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("culldeathwaypoints", CullDeathWaypoints);
            capi.Input.RegisterHotKey("reloadwaypointconfig", "Reload Waypoint Util Config", GlKeys.L, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("reloadwaypointconfig", a => { LoadConfig(); Repopulate(); return true; });
            capi.Input.RegisterHotKey("waypointfrontend", "Open WaypointUtils GUI", GlKeys.P, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("waypointfrontend", a => { api.Event.RegisterCallback(d => frontEnd.Toggle(), 100); return true; });

            capi.RegisterCommand("wpcfg", "Waypoint Configurtion", "[dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid]", new ClientChatCommandDelegate(CmdWaypointConfig));
            capi.RegisterCommand("measure", "Tape Measure", "[start|end|calc]", new ClientChatCommandDelegate(CmdMeasuringTape));
            capi.RegisterCommand("lightutil", "Light Util", "[lightlevel|type|radius|alpha|red]", new ClientChatCommandDelegate(CmdLightUtil));

            id = api.World.RegisterGameTickListener(dt =>
            {
                EntityPlayer player = api.World.Player.Entity;

                if (player != null)
                {
                    frontEnd.OnOwnPlayerDataReceived();
                    player.WatchedAttributes.RegisterModifiedListener("entityDead", () =>
                    {
                        if (player.WatchedAttributes == null || player.WatchedAttributes["entityDead"] == null) return;

                        if (player.WatchedAttributes["entityDead"].ToString() == "1")
                        {
                            api.SendChatMessage("/waypoint add #" + ColorStuff.RandomHexColorVClamp(api, 0.50, 0.80) + " *Player Death Waypoint*");
                        }
                    });

                    api.World.RegisterCallback(d =>
                    {
                        if (Layer().ownWaypoints.Count > 0 && capi.Settings.Bool["floatywaypoints"]) OpenWaypoints();
                    }, 500);

                    api.World.RegisterGameTickListener(d =>
                    {
                        if (Layer().ownWaypoints.Count != guiDialogs.Count && guiDialogs.Count > 0) Repopulate();
                        if (Config.LightLevels)
                        {
                            LightHighlight(null, Config.LightLevelType);
                        }
                        else
                        {
                            ClearLightLevelHighlights();
                        }
                    }, 500);

                    api.World.UnregisterGameTickListener(id);
                }
            }, 500);

        }

        private void CmdWaypointConfig(int groupId, CmdArgs args)
        {
            string arg = args.PopWord();
            switch (arg)
            {
                case "dotrange":
                    double? dr = args.PopDouble();
                    Config.DotRange = dr != null ? (double)dr : Config.DotRange;
                    capi.ShowChatMessage("Dot Range Set To " + Config.DotRange + " Meters.");
                    break;
                case "titlerange":
                    double? tr = args.PopDouble();
                    Config.TitleRange = tr != null ? (double)tr : Config.TitleRange;
                    capi.ShowChatMessage("Title Range Set To " + Config.TitleRange + " Meters.");
                    break;
                case "perblockwaypoints":
                    bool? pb = args.PopBool();
                    Config.PerBlockWaypoints = pb != null ? (bool)pb : !Config.PerBlockWaypoints;
                    capi.ShowChatMessage("Per Block Waypoints Set To " + Config.PerBlockWaypoints + ".");
                    break;
                case "pdw":
                    CullDeathWaypoints(new KeyCombination());
                    break;
                case "open":
                    ViewWaypoints(new KeyCombination());
                    break;
                case "waypointprefix":
                    bool? wp = args.PopBool();
                    Config.WaypointPrefix = wp != null ? (bool)wp : !Config.WaypointPrefix;
                    capi.ShowChatMessage("Waypoint Prefix Set To " + Config.WaypointPrefix + ".");
                    break;
                case "waypointid":
                    bool? wi = args.PopBool();
                    Config.WaypointID = wi != null ? (bool)wi : !Config.WaypointID;
                    capi.ShowChatMessage("Waypoint ID Set To " + Config.WaypointID + ".");
                    break;
                case "purge":
                    string s = args.PopWord();

                    if (s == "reallyreallyconfirm") Purge();
                    else capi.ShowChatMessage(Lang.Get("Are you sure you want to do that? It will remove ALL your waypoints, type \"reallyreallyconfirm\" to confirm."));
                    break;
                case "save":
                    break;
                default:
                    capi.ShowChatMessage(Lang.Get("Syntax: .wpcfg [dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid]"));
                    break;
            }
            SaveConfig();
            Repopulate();
        }

        public void LoadConfig()
        {
            if (capi.LoadModConfig<WaypointUtilConfig>("waypointutils.json") == null) { SaveConfig(); return; }

            Config = capi.LoadModConfig<WaypointUtilConfig>("waypointutils.json");
            SaveConfig();
        }

        public void SaveConfig() => capi.StoreModConfig(Config, "waypointutils.json");


        WaypointMapLayer Layer()
        {
            WorldMapManager modMapManager = capi.ModLoader.GetModSystem("Vintagestory.GameContent.WorldMapManager") as WorldMapManager;
            return modMapManager.MapLayers.Single(l => l is WaypointMapLayer) as WaypointMapLayer;
        }

        List<GuiDialogFloatyWaypoints> guiDialogs = new List<GuiDialogFloatyWaypoints>();
        private bool ViewWaypoints(KeyCombination t1)
        {
            if (guiDialogs.Count != 0) CloseAndClear();
            else OpenWaypoints();
            return true;
        }

        public void CloseAndClear()
        {
            for (int i = 0; i < guiDialogs.Count; i++)
            {
                guiDialogs[i].TryClose();
            }
            guiDialogs.Clear();
        }

        public void OpenWaypoints()
        {
            WaypointMapLayer layer = Layer();

            guiDialogs = new List<GuiDialogFloatyWaypoints>();

            for (int i = 0; i < layer.ownWaypoints.Count; i++)
            {
                string wp = Config.WaypointPrefix ? "Waypoint: " : "";
                wp = Config.WaypointID ? wp + "ID: " + i + " | " : wp;
                string text = layer.ownWaypoints[i].Title != null ? wp + layer.ownWaypoints[i].Title : "Waypoint: ";
                int color = layer.ownWaypoints[i].Color;
                Vec3d wPos = Config.PerBlockWaypoints ? layer.ownWaypoints[i].Position.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0) : layer.ownWaypoints[i].Position;

                floatyPoints = new GuiDialogFloatyWaypoints(text, capi, wPos, color);

                floatyPoints.OnOwnPlayerDataReceived();
                if (floatyPoints.TryOpen())
                {
                    guiDialogs.Add(floatyPoints);
                }
            }
        }

        public bool CullDeathWaypoints(KeyCombination t1)
        {
            WaypointMapLayer layer = Layer();

            for (int i = layer.ownWaypoints.Count; i-- > 0;)
            {
                if (layer.ownWaypoints[i].Title.Contains("*Player Death Waypoint*"))
                {
                    capi.SendChatMessage("/waypoint remove " + i);
                }
            }
            Repopulate();
            return true;
        }

        public void Purge()
        {
            WaypointMapLayer layer = Layer();

            for (int i = layer.ownWaypoints.Count; i-- > 0;)
            {
                capi.SendChatMessage("/waypoint remove " + 0);
            }
            Repopulate();
        }

        public void Repopulate()
        {
            ViewWaypoints(new KeyCombination());
            ViewWaypoints(new KeyCombination());
        }

        #region TapeMeasture
        BlockPos start = new BlockPos(0, 0, 0);
        BlockPos end = new BlockPos(0, 0, 0);

        public void CmdMeasuringTape(int groupId, CmdArgs args)
        {
            string arg = args.PopWord();
            switch (arg)
            {
                case "start":
                    if (capi.World.Player.CurrentBlockSelection != null)
                    {
                        start = capi.World.Player.CurrentBlockSelection.Position;
                        //capi.ShowChatMessage("Okay, start set to: " + start);
                        MakeHighlights();
                    }
                    else capi.ShowChatMessage("Please look at a block.");
                    break;
                case "end":
                    if (capi.World.Player.CurrentBlockSelection != null)
                    {
                        end = capi.World.Player.CurrentBlockSelection.Position;
                        //capi.ShowChatMessage("Okay, end set to: " + end);
                        MakeHighlights();
                    }
                    else capi.ShowChatMessage("Please look at a block.");
                    break;
                case "calc":
                    string type = args.PopWord();
                    switch (type)
                    {
                        case "block":
                            capi.ShowChatMessage("Block Distance: " + Math.Round(start.DistanceTo(end) + 1));
                            break;
                        case "euclidian":
                            capi.ShowChatMessage("Euclidian Distance: " + start.DistanceTo(end));
                            break;
                        case "manhattan":
                            capi.ShowChatMessage("Manhattan Distance: " + start.ManhattenDistance(end));
                            break;
                        case "horizontal":
                            capi.ShowChatMessage("Horizontal Distance: " + Math.Sqrt(start.HorDistanceSqTo(end.X, end.Z)));
                            break;
                        case "horizontalmanhattan":
                            capi.ShowChatMessage("Horizontal Manhattan Distance: " + start.HorizontalManhattenDistance(end));
                            break;
                        default:
                            capi.ShowChatMessage("Syntax: .measure calc [block|euclidian|manhattan|horizontal|horizontalmanhattan]");
                            break;
                    }
                    break;
                default:
                    capi.ShowChatMessage("Syntax: .measure [start|end|calc]");
                    break;
            }
        }

        public void CmdLightUtil(int groupId, CmdArgs args)
        {
            string arg = args.PopWord();
            switch (arg)
            {
                case "lightlevel":
                    bool? cnd = args.PopBool();
                    if (cnd != null) Config.LightLevels = (bool)cnd;
                    else { Config.LightLevels = !Config.LightLevels; }
                    break;
                case "type":
                    int? type = args.PopInt();
                    if (type != null)
                    {
                        EnumLightLevelType leveltype = (EnumLightLevelType)type;
                        Config.LightLevelType = (EnumLightLevelType)type;
                    }
                    capi.ShowChatMessage("Light Util Type Set To " + Enum.GetName(typeof(EnumLightLevelType), Config.LightLevelType));
                    break;
                case "radius":
                    int? rad = args.PopInt();
                    if (rad != null)
                    {
                        Config.LightRadius = (int)rad;
                    }
                    capi.ShowChatMessage("Light Util Radius Set To " + Config.LightRadius);
                    break;
                case "alpha":
                    float? alpha = args.PopFloat();
                    Config.LightLevelAlpha = alpha != null ? (float)alpha : Config.LightLevelAlpha;
                    capi.ShowChatMessage("Light Util Opacity Set To " + Config.LightLevelAlpha);
                    break;
                case "red":
                    int? red = args.PopInt();
                    if (red != null)
                    {
                        Config.LightLevelRed = (int)red;
                    }
                    capi.ShowChatMessage("Red Level Set To " + Config.LightLevelRed);
                    break;
                case "above":
                    bool? ab = args.PopBool();
                    if (ab != null) Config.LightLevels = (bool)ab;
                    else { Config.LUShowAbove = !Config.LUShowAbove; }
                    capi.ShowChatMessage("Show Above Set To " + Config.LUShowAbove);
                    break;
                default:
                    capi.ShowChatMessage("Syntax: .lightutil [lightlevel|type|radius|alpha|red|above]");
                    break;
            }
            SaveConfig();
        }

        List<BlockPos> highlightedBlocks = new List<BlockPos>();
        Dictionary<BlockPos, int> prepared = new Dictionary<BlockPos, int>();

        public void LightHighlight(BlockPos pos = null, EnumLightLevelType type = EnumLightLevelType.OnlyBlockLight)
        {
            if (highlightedBlocks.Count != 0) ClearLightLevelHighlights();

            pos = pos == null ? capi.World.Player.Entity.LocalPos.AsBlockPos.UpCopy() : pos;
            int rad = Config.LightRadius;

            for (int x = -rad; x <= rad; x++)
            {
                for (int y = -rad; y <= rad; y++)
                {
                    for (int z = -rad; z <= rad; z++)
                    {
                        BlockPos iPos = pos.AddCopy(x, y, z);
                        Block block = capi.World.BlockAccessor.GetBlock(iPos);

                        bool rep = Config.LUSpawning ? capi.World.BlockAccessor.GetBlock(iPos.UpCopy()).IsReplacableBy(block) : true;
                        bool opq = Config.LUOpaque ? block.AllSidesOpaque : true;

                        if (block.BlockId != 0 && rep && opq && (x * x + y * y + z * z) <= (rad * rad))
                        {
                            highlightedBlocks.Add(iPos);
                        }
                    }
                }
            }
            BlockPos[] blocks = highlightedBlocks.ToArray();
            LightLevelHighlight(blocks, type);
        }

        public void LightLevelHighlight(BlockPos[] blocks, EnumLightLevelType type)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                BlockPos iPos = Config.LUShowAbove ? blocks[i].UpCopy() : blocks[i];
                int level = capi.World.BlockAccessor.GetLightLevel(iPos, type);

                if (level != 0)
                {
                    float fLevel = level / 32.0f;
                    int alpha = (int)Math.Round(Config.LightLevelAlpha * 255);
                    int c = level > Config.LightLevelRed ? ColorUtil.ToRgba(alpha, 0, (int)(fLevel * 255), 0) : ColorUtil.ToRgba(alpha, 0, 0, (int)(fLevel * 255));

                    List<BlockPos> highlight = new List<BlockPos>() { blocks[i].AddCopy(0, 1, 0), blocks[i].AddCopy(1, 0, 1) };
                    List<int> color = new List<int>() { c };

                    capi.World.HighlightBlocks(capi.World.Player, Config.MinLLID + i, highlight, color, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
                }
            }
        }

        public void ClearLightLevelHighlights()
        {
            for (int i = 0; i < highlightedBlocks.Count; i++)
            {
                capi.World.HighlightBlocks(capi.World.Player, Config.MinLLID + i, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
            }
            highlightedBlocks.Clear();
        }

        public void MakeHighlights()
        {
            List<BlockPos> startBlock = new List<BlockPos>() { start.AddCopy(0, 1, 0), start.AddCopy(1, 0, 1) };
            List<BlockPos> endBlock = new List<BlockPos>() { end.AddCopy(0, 1, 0), end.AddCopy(1, 0, 1) };
            List<int> startcolor = new List<int>() { ColorUtil.ToRgba((int)(0.5 * 255), 0, 255, 0) };
            List<int> endcolor = new List<int>() { ColorUtil.ToRgba((int)(0.5 * 255), 0, 0, 255) };

            capi.World.HighlightBlocks(capi.World.Player, 4, startBlock, startcolor, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
            capi.World.HighlightBlocks(capi.World.Player, 5, endBlock, endcolor, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);

            capi.World.RegisterCallback(dt => ClearMeasureHighlights(), 1000);
        }

        public void ClearMeasureHighlights()
        {
            capi.World.HighlightBlocks(capi.World.Player, 4, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
            capi.World.HighlightBlocks(capi.World.Player, 5, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
        }
        #endregion
    }

    public class WaypointFrontEnd : GuiDialog
    {
        public WaypointFrontEnd(ICoreClientAPI capi) : base(capi)
        {
        }
        string wpText = "";
        string color;

        public override string ToggleKeyCombinationCode => "waypointfrontend";
        public void VClose() => TryClose();
        public override bool PrefersUngrabbedMouse => false;

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();

            ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 30, 0, 380, 500);
            ElementBounds bgBounds = ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 0, -200, 380, 100);

            string[] colors;
            List<string> c = new List<string>() { "Random" };
            string[] names = Enum.GetNames(typeof(KnownColor));

            for (int i = 28; i < 166; i++)
            {
                c.Add(names[i]);
            }
            colors = c.ToArray();
            WaypointUtilSystem.Config.SetColorIndex = WaypointUtilSystem.Config.SetColorIndex < 0 ? 0 : WaypointUtilSystem.Config.SetColorIndex;

            color = colors[WaypointUtilSystem.Config.SetColorIndex];

            SingleComposer = capi.Gui.CreateCompo("waypointfrontend", dialogBounds)
                .AddDialogTitleBar("Waypoint Utils", VClose, CairoFont.WhiteSmallText())
                .AddDialogBG(bgBounds)
                .AddTextInput(ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 86.5, -200, 285, 20), OnTextChanged, null, "textinput")
                .AddDropDown(colors, colors, WaypointUtilSystem.Config.SetColorIndex, (newval, on) =>
                {
                    color = newval;
                    WaypointUtilSystem.Config.SetColorIndex = Array.BinarySearch(colors, newval);
                    capi.TriggerChatMessage(".wpcfg save");
                },
                ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 250, -170, 125, 25), "dropdown")
                .AddTextToggleButtons(new string[] { "Create WP", "Purge Death Waypoints", "Toggle Floaty Waypoints", "Toggle Block Waypoints" }, CairoFont.ButtonText().WithFontSize(10), i =>
                {
                    capi.Event.RegisterCallback(j =>
                    {
                        SingleComposer.Dispose();
                        OnOwnPlayerDataReceived();
                        SingleComposer.Compose();
                    }, 100);
                    switch (i)
                    {
                        case 0:
                            CreateWaypoint();
                            break;
                        case 1:
                            capi.TriggerChatMessage(".wpcfg pdw");
                            break;
                        case 2:
                            capi.TriggerChatMessage(".wpcfg open");
                            break;
                        case 3:
                            capi.TriggerChatMessage(".wpcfg perblockwaypoints");
                            break;
                        default:
                            break;
                    }

                }, new ElementBounds[] {
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 5, -200, 80, 25),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 5, -170, 80, 35),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 85, -170, 80, 35),
                    ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 165, -170, 80, 35),
                });
        }

        public void CreateWaypoint()
        {
            wpText = wpText != "" ? wpText : "Waypoint";
            if (color == "Random")
            {
                capi.SendChatMessage("/waypoint add #" + ColorStuff.RandomHexColorVClamp(capi, 0.50, 0.80) + " " + wpText);
            }
            else
            {
                capi.SendChatMessage("/waypoint add " + color + " " + wpText);
            }

        }

        public void OnTextChanged(string text)
        {
            wpText = text;
        }

        public override void OnGuiOpened()
        {
            SingleComposer.Compose();
        }

        public override void OnGuiClosed()
        {
            SingleComposer.Dispose();
        }

        bool inputbool = true;
        public override void OnRenderGUI(float deltaTime)
        {
            if (capi.Input.KeyboardKeyState[(int)GlKeys.Enter] && inputbool)
            {
                inputbool = false;
                capi.Event.RegisterCallback(j =>
                {
                    inputbool = true;
                    TryClose();
                }, 100);
                CreateWaypoint();
                capi.Gui.PlaySound("tick");
            }
            base.OnRenderGUI(deltaTime);
        }
    }

    public class GuiDialogFloatyWaypoints : HudElement
    {
        Vec3d waypointPos;
        string DialogTitle;
        int color;
        string dialogText = "";
        double distance = 0;
        long id;

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

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0);

            SingleComposer = capi.Gui
                .CreateCompo(DialogTitle + capi.Gui.OpenedGuis.Count + 1, dialogBounds)
                .AddDynamicText("", font, EnumTextOrientation.Center, textBounds, "text")
                .Compose()
            ;

            if (capi.Settings.Bool["floatywaypoints"]) TryOpen();

            UpdateDialog();
            id = capi.World.RegisterGameTickListener(dt => UpdateDialog(), 500 + capi.World.Rand.Next(0, 64));
        }

        public void UpdateDialog()
        {
            EntityPlayer entityPlayer = capi.World.Player.Entity;
            distance = Math.Round(Math.Sqrt(entityPlayer.Pos.SquareDistanceTo(waypointPos)), 3);
            dialogText = DialogTitle + " " + distance + "m" + "\n\u2022";
            order = 1.0 / distance;
        }

        protected virtual double FloatyDialogPosition => 0.75;
        protected virtual double FloatyDialogAlign => 0.75;
        public double order;
        public override double DrawOrder => order;

        public override bool ShouldReceiveMouseEvents() => false;

        public override void OnRenderGUI(float deltaTime)
        {
            if (!capi.Settings.Bool["floatywaypoints"]) return;

            WaypointUtilConfig config = WaypointUtilSystem.Config;

            Vec3d aboveHeadPos = new Vec3d(waypointPos.X + 0.5, waypointPos.Y + FloatyDialogPosition, waypointPos.Z + 0.5);
            Vec3d pos = MatrixToolsd.Project(aboveHeadPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
            ElementBounds bounds = ElementBounds.Empty;

            if (pos.Z < 0 || (distance > config.DotRange && !dialogText.Contains("*")))
            {
                SingleComposer.GetDynamicText("text").SetNewText("");
                return;
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
            capi.Settings.Bool["floatywaypoints"] = false;
            Dispose();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            capi.Settings.Bool["floatywaypoints"] = true;
        }
    }

    class ColorStuff : ColorUtil
    {
        public static int RandomColor(ICoreAPI api) => HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255)
            );

        public static string RandomHexColor(ICoreAPI api) => RandomColor(api).ToString("X");
        public static string RandomHexColorVClamp(ICoreAPI api, double min, double max) => ClampedRandomColorValue(api, min, max).ToString("X");

        public static int ClampedRandomColorValue(ICoreAPI api, double min, double max)
        {
            return HsvToRgb(
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(api.World.Rand.NextDouble() * 255),
            (int)(GameMath.Clamp(api.World.Rand.NextDouble(), min, max) * 255)
            );
        }
    }
}
