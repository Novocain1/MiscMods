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

namespace VSHUD
{
    class WaypointUtilSystem : ModSystem
    {
        long id;
        ICoreClientAPI capi;
        public ConfigLoader cL;
        public WaypointUtilConfig Config;
        public WaypointMapLayer WPLayer { get => capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<WaypointMapLayer>().Single(); }

        public Dictionary<string, LoadedTexture> texturesByIcon;

        public void PopulateTextures()
        {
            if (this.texturesByIcon == null | false)
            {
                this.texturesByIcon = new Dictionary<string, LoadedTexture>();
                ImageSurface surface = new ImageSurface((Format)0, 25, 25);
                Context cr = new Context((Surface)surface);
                string[] strArray = new string[13]
                { "circle", "bee", "cave", "home", "ladder", "pick", "rocks", "ruins", "spiral", "star1", "star2", "trader", "vessel" };
                foreach (string text in strArray)
                {
                    cr.Operator = (Operator)0;
                    cr.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
                    cr.Paint();
                    cr.Operator = (Operator)2;
                    capi.Gui.Icons.DrawIcon(cr, "wp" + text.UcFirst(), 1.0, 1.0, 15.0, 15.0, ColorUtil.WhiteArgbDouble);
                    this.texturesByIcon[text] = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, false), 20, 20);
                }
                cr.Dispose();
                ((Surface)surface).Dispose();
            }
        }

        public List<Waypoint> Waypoints { get => WPLayer.ownWaypoints; }
        public List<WaypointRelative> WaypointsRel {
            get
            {
                List<WaypointRelative> rel = new List<WaypointRelative>();
                foreach (var val in Waypoints)
                {
                    WaypointRelative relative = new WaypointRelative(capi)
                    {
                        Color = val.Color,
                        OwningPlayerGroupId = val.OwningPlayerGroupId,
                        OwningPlayerUid = val.OwningPlayerUid,
                        Position = val.Position,
                        Text = val.Text,
                        Title = val.Title
                    };
                    rel.Add(relative);
                }
                return rel;
            }
        }

        public List<HudElementWaypoint> WaypointElements { get; set; } = new List<HudElementWaypoint>();

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
            PopulateTextures();

            cL = capi.ModLoader.GetModSystem<ConfigLoader>();
            Config = cL.Config;

            GuiDialogWaypointFrontEnd frontEnd = new GuiDialogWaypointFrontEnd(capi);

            capi.Input.RegisterHotKey("viewwaypoints", "View Waypoints", GlKeys.U, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("viewwaypoints", ViewWaypoints);
            capi.Input.RegisterHotKey("culldeathwaypoints", "Cull Death Waypoints", GlKeys.O, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("culldeathwaypoints", CullDeathWaypoints);
            capi.Input.RegisterHotKey("waypointfrontend", "Open WaypointUtils GUI", GlKeys.P, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("waypointfrontend", a => { api.Event.RegisterCallback(d => frontEnd.Toggle(), 100); return true; });

            capi.RegisterCommand("wpcfg", "Waypoint Configurtion", "[dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid|enableall]", new ClientChatCommandDelegate(CmdWaypointConfig));

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

                    api.World.RegisterCallback(d => RepopulateDialogs(), 100);

                    api.World.RegisterGameTickListener(d =>
                    {
                        Update();
                    }, 500);

                    api.World.UnregisterGameTickListener(id);
                }
            }, 500);

        }

        private void CmdWaypointConfig(int groupId, CmdArgs args)
        {
            bool repop = true;
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
                case "enableall":
                    Config.DisabledColors.Clear();
                    break;
                case "save":
                    break;
                case "export":
                    repop = false;
                    using (TextWriter tw = new StreamWriter("waypoints.json"))
                    {
                        tw.Write(JsonConvert.SerializeObject(WaypointsRel, Formatting.Indented));
                        tw.Close();
                    }
                    break;
                case "testlimits":
                    for (int i = 0; i < 130; i++)
                    {
                        capi.SendChatMessage("/waypoint add blue test");
                    }
                    break;
                default:
                    capi.ShowChatMessage(Lang.Get("Syntax: .wpcfg [dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid|enableall]"));
                    break;
            }
            cL.SaveConfig();
            if (repop) RepopulateDialogs();
        }

        
        private bool ViewWaypoints(KeyCombination t1)
        {
            capi.Settings.Bool["floatywaypoints"] = !capi.Settings.Bool["floatywaypoints"];
            return true;
        }

        public void Update()
        {
            if (WaypointElements.Count == 0) RepopulateDialogs();
            if (WaypointElements.Count > 0)
            {
                if (Waypoints.Count != WaypointElements.Count)
                {
                    RepopulateDialogs();
                }
                if (capi.Settings.Bool["floatywaypoints"])
                {
                    foreach (var val in WaypointElements)
                    {
                        if (val.IsOpened() && val.distance > Config.DotRange && !val.DialogTitle.Contains("*")) val.TryClose();
                        else if (!val.IsOpened() && val.distance < Config.DotRange || val.DialogTitle.Contains("*")) val.TryOpen();
                    }
                }
                else
                {
                    foreach (var val in WaypointElements)
                    {
                        if (val.IsOpened()) val.TryClose();
                    }
                }
            }
        }

        public void RepopulateDialogs()
        {
            foreach (var val in WaypointElements)
            {
                val.TryClose();
                val.Dispose();
            }
            WaypointElements.Clear();

            int i = 0;
            foreach (var val in Waypoints)
            {
                HudElementWaypoint waypoint = new HudElementWaypoint(capi, val, i);
                waypoint.OnOwnPlayerDataReceived();

                WaypointElements.Add(waypoint);
                i++;
            }
        }

        public bool CullDeathWaypoints(KeyCombination t1)
        {
            for (int i = Waypoints.Count; i-- > 0;)
            {
                if (Waypoints[i].Title.Contains("*Player Death Waypoint*"))
                {
                    capi.SendChatMessage("/waypoint remove " + i);
                    BlockPos rel = Waypoints[i].Position.AsBlockPos.SubCopy(capi.World.DefaultSpawnPosition.AsBlockPos);
                    string str = Waypoints[i].Title + " Deleted, Rel: " + rel.ToString() + ", Abs: " + Waypoints[i].Position.ToString();
                    StringBuilder builder = new StringBuilder(str).AppendLine();
                    FileInfo info = new FileInfo(Path.Combine(GamePaths.Logs, "waypoints-log.txt"));
                    GamePaths.EnsurePathExists(info.Directory.FullName);
                    File.AppendAllText(info.FullName, builder.ToString());
                    capi.Logger.Event(str);
                }
            }
            RepopulateDialogs();
            return true;
        }

        private void Purge()
        {
            for (int i = Waypoints.Count; i-- > 0;)
            {
                capi.SendChatMessage("/waypoint remove " + 0);
            }
            RepopulateDialogs();
        }
    }

    public class WaypointRelative : Waypoint
    {
        ICoreAPI api;
        public WaypointRelative(ICoreAPI api)
        {
            this.api = api;
        }

        public Vec3d RelativeToSpawn { get => Position.SubCopy(api.World.DefaultSpawnPosition.XYZ); }
    }
}
