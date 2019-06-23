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
    class WaypointUtilSystem : ModSystem
    {
        long id;
        ICoreClientAPI capi;
        HudElementWaypoint floatyPoints;
        public ConfigLoader cL;
        public WaypointUtilConfig Config;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
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

                    api.World.RegisterCallback(d =>
                    {
                        if (Layer().ownWaypoints.Count > 0 && capi.Settings.Bool["floatywaypoints"]) OpenWaypoints();
                    }, 500);

                    api.World.RegisterGameTickListener(d =>
                    {
                        if (Layer().ownWaypoints.Count != guiDialogs.Count && guiDialogs.Count > 0) Repopulate();
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
                case "enableall":
                    Config.DisabledColors.Clear();
                    break;
                case "save":
                    break;
                default:
                    capi.ShowChatMessage(Lang.Get("Syntax: .wpcfg [dotrange|titlerange|perblockwaypoints|purge|waypointprefix|waypointid|enableall]"));
                    break;
            }
            cL.SaveConfig();
            Repopulate();
        }

        WaypointMapLayer Layer()
        {
            WorldMapManager modMapManager = capi.ModLoader.GetModSystem("Vintagestory.GameContent.WorldMapManager") as WorldMapManager;
            return modMapManager.MapLayers.Single(l => l is WaypointMapLayer) as WaypointMapLayer;
        }

        List<HudElementWaypoint> guiDialogs = new List<HudElementWaypoint>();
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

            guiDialogs = new List<HudElementWaypoint>();

            for (int i = 0; i < layer.ownWaypoints.Count; i++)
            {
                string wp = Config.WaypointPrefix ? "Waypoint: " : "";
                wp = Config.WaypointID ? wp + "ID: " + i + " | " : wp;
                string text = layer.ownWaypoints[i].Title != null ? wp + layer.ownWaypoints[i].Title : "Waypoint: ";
                int color = layer.ownWaypoints[i].Color;
                Vec3d wPos = Config.PerBlockWaypoints ? layer.ownWaypoints[i].Position.AsBlockPos.ToVec3d().SubCopy(0, 0.5, 0) : layer.ownWaypoints[i].Position;

                if (Config.DisabledColors.Contains(color)) continue;

                floatyPoints = new HudElementWaypoint(text, capi, wPos, color);

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
    }
}
