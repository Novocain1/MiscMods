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
using Cairo;
using Vintagestory.API.Util;
using Path = System.IO.Path;
using Action = Vintagestory.API.Common.Action;
using System.Globalization;
using Vintagestory.Common;

namespace VSHUD
{
    class WaypointUtils : ClientModSystem
    {
        ICoreClientAPI capi;
        public VSHUDConfig Config;
        public WorldMapManager MapManager { get => capi.ModLoader.GetModSystem<WorldMapManager>(); }
        public WaypointMapLayer WPLayer { get => MapManager.MapLayers.OfType<WaypointMapLayer>().Single(); }
        public static Dictionary<string, LoadedTexture> texturesByIcon;
        public static string[] iconKeys;
        
        public static bool doingConfigAction = false;

        public static void PopulateTextures(ICoreClientAPI capi)
        {
            if (texturesByIcon == null | false)
            {
                texturesByIcon = new Dictionary<string, LoadedTexture>();
                ImageSurface surface = new ImageSurface(0, 64, 64);
                Context cr = new Context(surface);
                string[] strArray = new string[13]
                { "circle", "bee", "cave", "home", "ladder", "pick", "rocks", "ruins", "spiral", "star1", "star2", "trader", "vessel" };
                foreach (string text in strArray)
                {
                    cr.Operator = (Operator)0;
                    cr.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
                    cr.Paint();
                    cr.Operator = (Operator)2;
                    capi.Gui.Icons.DrawIcon(cr, "wp" + text.UcFirst(), 1.0, 1.0, 32.0, 32.0, ColorUtil.WhiteArgbDouble);
                    texturesByIcon[text] = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, true), 20, 20);
                }
                iconKeys = texturesByIcon.Keys.ToArray();

                cr.Dispose();
                surface.Dispose();
            }
        }

        public List<Waypoint> Waypoints { get => WPLayer.ownWaypoints; }
        public WaypointRelative[] WaypointsRelSorted { get => WaypointsRel.OrderBy(wp => wp.DistanceFromPlayer).ToArray(); }
        public WaypointRelative[] WaypointsRel {
            get
            {
                Queue<WaypointRelative> rel = new Queue<WaypointRelative>();
                int i = 0;
                var wps = Waypoints.ToArray();

                foreach (var val in wps)
                {
                    if (val == null) continue;

                    WaypointRelative relative = new WaypointRelative(capi, val, i)
                    {
                        Color = val.Color,
                        OwningPlayerGroupId = val.OwningPlayerGroupId,
                        OwningPlayerUid = val.OwningPlayerUid,
                        Position = val.Position,
                        Text = val.Text,
                        Title = val.Title,
                        Icon = val.Icon,
                    };
                    rel.Enqueue(relative);
                    i++;
                }
                return rel.ToArray();
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
            PopulateTextures(capi);
            HudElementWaypoint.quadModel = capi.Render.UploadMesh(QuadMeshUtil.GetQuad());

            Config = ConfigLoader.Config;

            GuiDialogWaypointFrontEnd frontEnd = new GuiDialogWaypointFrontEnd(capi);

            capi.Input.RegisterHotKey("viewwaypoints", "View Waypoints", GlKeys.U, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("viewwaypoints", ViewWaypoints);
            capi.Input.RegisterHotKey("culldeathwaypoints", "Cull Death Waypoints", GlKeys.O, HotkeyType.GUIOrOtherControls);
            capi.Input.RegisterHotKey("waypointfrontend", "Open WaypointUtils GUI", GlKeys.P, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("waypointfrontend", a => { api.Event.RegisterCallback(d => frontEnd.Toggle(), 100); return true; });

            capi.RegisterCommand("wpcfg", "Waypoint Configurtion", "[dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid|enableall|import|export|pillars]", new ClientChatCommandDelegate(CmdWaypointConfig));

            capi.Event.LevelFinalize += () =>
            {
                var pillar = CubeMeshUtil.GetCube(0.1f, capi.World.BlockAccessor.MapSizeY, new Vec3f());
                HudElementWaypoint.pillar = capi.Render.UploadMesh(pillar);

                EntityPlayer player = api.World.Player.Entity;
                frontEnd.OnOwnPlayerDataReceived();

                player.WatchedAttributes.RegisterModifiedListener("entityDead", () =>
                {
                    if (player?.WatchedAttributes?["entityDead"] == null) return;
                    Vec3d playerPos = player.Pos.XYZ;

                    if (player.WatchedAttributes.GetInt("entityDead") == 1)
                    {
                        string str = string.Format(CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", "rocks", playerPos.X, playerPos.Y, playerPos.Z, true, ColorStuff.RandomHexColorVClamp(capi, 0.5, 0.8), "Player Death Waypoint");

                        capi.SendChatMessage(str);
                        if (Config.DebugDeathWaypoints) capi.ShowChatMessage("DEBUG: Sent Command: " + str);
                    }
                });

                //Trick server into sending waypoints to the client even if they don't have their map opened.
                MapManager.GetField<IClientNetworkChannel>("clientChannel").SendPacket(new OnViewChangedPacket() { NowVisible = new List<Vec2i>(), NowHidden = new List<Vec2i>() });

                capi.InjectClientThread("WaypointDialogUpdate", 20, new WaypointTextUpdateSystem(capi.World as ClientMain));
                capi.InjectClientThread("Floaty Waypoint Management", 30, new FloatyWaypointManagement(capi.World as ClientMain, api.ModLoader.GetModSystem<WaypointUtils>()));
            };
        }

        private void CmdWaypointConfig(int groupId, CmdArgs args)
        {
            doingConfigAction = true;
            string arg = args.PopWord();
            switch (arg)
            {
                case "deathdebug":
                    Config.DebugDeathWaypoints = args.PopBool() ?? !Config.DebugDeathWaypoints;
                    capi.ShowChatMessage(string.Format("Death waypoint debbuging set to {0}", Config.DebugDeathWaypoints));
                    break;
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
                    PurgeWaypointsByStrings("Player Death Waypoint", "*Player Death Waypoint*");
                    break;
                case "plt":
                    PurgeWaypointsByStrings("Limits Test");
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
                case "enableall":
                    Config.DisabledColors.Clear();
                    break;
                case "save":
                    break;
                case "export":
                    string path = (args.PopWord() ?? "waypoints") + ".json";
                    using (TextWriter tw = new StreamWriter(path))
                    {
                        tw.Write(JsonConvert.SerializeObject(WaypointsRel, Formatting.Indented));
                        tw.Close();
                    }
                    break;
                case "import":
                    string path1 = (args.PopWord() ?? "waypoints") + ".json";
                    if (File.Exists(path1))
                    {
                        using (TextReader reader = new StreamReader(path1))
                        {
                            DummyWaypoint[] relative = JsonConvert.DeserializeObject<DummyWaypoint[]>(reader.ReadToEnd(), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore});
                            
                            VSHUDTaskSystem.Actions.Enqueue(new Action(() =>
                            {
                                for (int j = 0; j < relative.Length; j++)
                                {
                                    var val = relative[j];
                                    if (WaypointsRel.Any(w =>
                                    val.Position.AsBlockPos.X == w.Position.AsBlockPos.X &&
                                    val.Position.AsBlockPos.Y == w.Position.AsBlockPos.Y &&
                                    val.Position.AsBlockPos.Z == w.Position.AsBlockPos.Z
                                    )) return;

                                    string str = string.Format(CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", val.Icon, val.Position.X, val.Position.Y, val.Position.Z, val.Pinned, ColorUtil.Int2Hex(val.Color), val.Title);
                                    capi.SendChatMessage(str);
                                }
                            }));
                            reader.Close();
                        }
                    }
                    break;
                case "testlimits":
                    int amount = args.PopInt() ?? 30;
                    int maxY = args.PopInt() ?? 10;
                    double radius = (args.PopInt() ?? 1000);

                    VSHUDTaskSystem.Actions.Enqueue(new Action(() =>
                    {
                        for (int i = 0; i < amount; i++)
                        {
                            double
                            x = (capi.World.Rand.NextDouble() - 0.5) * radius,
                            z = (capi.World.Rand.NextDouble() - 0.5) * radius,
                            y = capi.World.BlockAccessor.GetRainMapHeightAt((int)x, (int)z);

                            double r = x * x + z * z;
                            if (r <= (radius * 0.5) * (radius * 0.5))
                            {
                                Vec3d pos = capi.World.Player.Entity.Pos.XYZ.AddCopy(new Vec3d(x, y, z));
                                bool deathWaypoint = capi.World.Rand.NextDouble() > 0.8;

                                string icon = deathWaypoint ? "rocks" : iconKeys[(int)(capi.World.Rand.NextDouble() * (iconKeys.Length - 1))];
                                string title = deathWaypoint ? "Limits Test Player Death Waypoint" : "Limits Test Waypoint";

                                string str = string.Format(CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", icon, pos.X, pos.Y, pos.Z, deathWaypoint, ColorStuff.RandomHexColorVClamp(capi, 0.5, 0.8), title);

                                capi.SendChatMessage(str);
                            }
                            else i--;
                        }
                    }));
                    break;
                case "pillars":
                    Config.ShowPillars = args.PopBool() ?? !Config.ShowPillars;
                    capi.ShowChatMessage("Waypoint Pillars Set To " + Config.ShowPillars + ".");
                    break;
                default:
                    capi.ShowChatMessage(Lang.Get("Syntax: .wpcfg [dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid|enableall|import|export|pillars]"));
                    break;
            }

            doingConfigAction = false;

            ConfigLoader.SaveConfig(capi);
            
            //Trick server into sending waypoints to the client even if they don't have their map opened.
            capi.Event.RegisterCallback(dt => MapManager.GetField<IClientNetworkChannel>("clientChannel").SendPacket(new OnViewChangedPacket() { NowVisible = new List<Vec2i>(), NowHidden = new List<Vec2i>() }), 500);
        }

        
        private bool ViewWaypoints(KeyCombination t1)
        {
            Config.FloatyWaypoints = !Config.FloatyWaypoints;
            ConfigLoader.SaveConfig(capi);

            foreach (var val in FloatyWaypointManagement.WaypointElements)
            {
                if (Config.FloatyWaypoints) val.TryOpen();
                else val.TryClose();
            }
            return true;
        }

        public bool PurgeWaypointsByStrings(params string[] containsStrings)
        {
            VSHUDTaskSystem.Actions.Enqueue(new Action(() =>
            {
                Stack<int> rmIDs = new Stack<int>();
                for (int i = 0; i < Waypoints.Count; i++)
                {
                    bool contains = false;
                    foreach (var contained in containsStrings)
                    {
                        if (contains = Waypoints[i].Title.Contains(contained)) break;
                    }
                    if (contains)
                    {
                        rmIDs.Push(i);
                    }
                }
                var arr = rmIDs.ToArray();
                Array.Sort(arr, delegate (int a, int b) { return b.CompareTo(a); });

                for (int i = 0; i < arr.Length; i++)
                {
                    var wpId = arr[i];
                    capi.SendChatMessage(string.Format("/waypoint remove {0}", wpId));
                }
            }));
            return true;
        }

        private void Purge()
        {
            VSHUDTaskSystem.Actions.Enqueue(new Action(() =>
            {
                for (int i = 0; i < Waypoints.Count; i++) capi.SendChatMessage("/waypoint remove 0");
            }));
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var val in texturesByIcon)
            {
                val.Value.Dispose();
                val.Value.SetField("capi", null);
            }
            texturesByIcon.Clear();
            texturesByIcon = null;
        }
    }

    public class WaypointRelative : Waypoint
    {
        ICoreAPI api;

        public WaypointRelative(ICoreAPI api, Waypoint ownWaypoint, int index)
        {
            this.api = api;
            OwnWaypoint = ownWaypoint;
            Index = index;
        }

        public int OwnColor { get => OwnWaypoint?.Color ?? -1; }
        public int Index { get; }
        public Waypoint OwnWaypoint { get; }
        
        public Vec3d RelativeToSpawn { get => Position.SubCopy(api.World.DefaultSpawnPosition.XYZ); }
        public double? DistanceFromPlayer { get => (api as ICoreClientAPI)?.World.Player.Entity.Pos.DistanceTo(Position); }
    }

    public class DummyWaypoint
    {
        public string Icon { get; set; }
        public Vec3d Position { get; set; }
        public bool Pinned { get; set; }
        public int Color { get; set; }
        public string Title { get; set; }
    }
}
