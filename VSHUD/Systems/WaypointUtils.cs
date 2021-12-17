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
using System.Globalization;
using Vintagestory.Common;

namespace VSHUD
{
    public class WaypointUtils : ClientModSystem
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

            capi.Input.RegisterHotKey("vshud.viewwaypoints", "(VSHUD) View Waypoints", GlKeys.U, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("vshud.viewwaypoints", ViewWaypoints);
            capi.Input.RegisterHotKey("vshud.culldeathwaypoints", "(VSHUD) Cull Death Waypoints", GlKeys.O, HotkeyType.GUIOrOtherControls);
            capi.Input.RegisterHotKey("vshud.waypointfrontend", "(VSHUD) Open WaypointUtils GUI", GlKeys.P, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("vshud.waypointfrontend", a => { api.Event.RegisterCallback(d => frontEnd.Toggle(), 100); return true; });

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

                capi.SendMyWaypoints();

                capi.InjectClientThread("WaypointDialogUpdate", 20, new WaypointTextUpdateSystem(capi.World as ClientMain));
                capi.InjectClientThread("Floaty Waypoint Management", 30, new FloatyWaypointManagement(capi.World as ClientMain, api.ModLoader.GetModSystem<WaypointUtils>()));
            };
        }
        
        public bool ViewWaypoints(KeyCombination t1)
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

        internal bool PurgeWaypointsByStrings(params string[] containsStrings)
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

                capi.SendMyWaypoints();
            }));

            return true;
        }

        internal void Purge()
        {
            VSHUDTaskSystem.Actions.Enqueue(new Action(() =>
            {
                for (int i = 0; i < Waypoints.Count; i++) capi.SendChatMessage("/waypoint remove 0");
                
                capi.SendMyWaypoints();
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
