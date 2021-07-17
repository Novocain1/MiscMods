using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Action = Vintagestory.API.Common.Action;

namespace VSHUD
{
    public class CommandFloatyWaypoints : VSHUDCommand
    {
        public CommandFloatyWaypoints(ICoreClientAPI capi, WaypointUtils utils) : base(capi)
        {
            VSHUDConfig config = ConfigLoader.Config;
            Command = "wpcfg";
            
            RegisterSubCommand("deathdebug", new SubCommand((player, groupId, args) =>
            {
                config.DebugDeathWaypoints = args.PopBool() ?? !config.DebugDeathWaypoints;
                capi.ShowChatMessage(string.Format("Death waypoint debbuging set to {0}", config.DebugDeathWaypoints));
            }, "Debug sending of death waypoint string, true/false."));

            RegisterSubCommand("dotrange", new SubCommand((player, groupId, args) =>
            {
                double? dr = args.PopDouble();
                config.DotRange = dr != null ? (double)dr : config.DotRange;
                capi.ShowChatMessage("Dot Range Set To " + config.DotRange + " Meters.");
            }, "Sets the dot range of floaty waypoints, any decimal value."));

            RegisterSubCommand("titlerange", new SubCommand((player, groupId, args) =>
            {
                double? tr = args.PopDouble();
                config.TitleRange = tr != null ? (double)tr : config.TitleRange;
                capi.ShowChatMessage("Title Range Set To " + config.TitleRange + " Meters.");
            }, "Sets the title range of floaty waypoints, any decimal value."));

            RegisterSubCommand("perblockwaypoints", new SubCommand((player, groupId, args) =>
            {
                bool? pb = args.PopBool();
                config.PerBlockWaypoints = pb != null ? (bool)pb : !config.PerBlockWaypoints;
                capi.ShowChatMessage("Per Block Waypoints Set To " + config.PerBlockWaypoints + ".");
            }, "Whether or not floaty waypoints are clamped to block positions, true/false."));

            RegisterSubCommand("pdw", new SubCommand((player, groupId, args) =>
            {
                utils.PurgeWaypointsByStrings("Player Death Waypoint", "*Player Death Waypoint*");
            }, "Purges death waypoints."));

            RegisterSubCommand("plt", new SubCommand((player, groupId, args) =>
            {
                utils.PurgeWaypointsByStrings("Limits Test");
            }, "Purges waypoints created by the testlimits debug command."));

            RegisterSubCommand("open", new SubCommand((player, groupId, args) =>
            {
                utils.ViewWaypoints(new KeyCombination());
            }, "Opens floaty waypoints uis."));

            RegisterSubCommand("waypointprefix", new SubCommand((player, groupId, args) =>
            {
                bool? wp = args.PopBool();
                config.WaypointPrefix = wp != null ? (bool)wp : !config.WaypointPrefix;
                capi.ShowChatMessage("Waypoint Prefix Set To " + config.WaypointPrefix + ".");
            }, "Whether or not waypoints are prefixed with \"Waypoint:\", true/false"));

            RegisterSubCommand("waypointid", new SubCommand((player, groupId, args) =>
            {
                bool? wi = args.PopBool();
                config.WaypointID = wi != null ? (bool)wi : !config.WaypointID;
                capi.ShowChatMessage("Waypoint ID Set To " + config.WaypointID + ".");
            }, "Whether or not floaty waypoints are prefixed with their ID, true/false"));

            RegisterSubCommand("purge", new SubCommand((player, groupId, args) =>
            {
                string s = args.PopWord();
                if (s == "reallyreallyconfirm") utils.Purge();
                else capi.ShowChatMessage(Lang.Get("Are you sure you want to do that? It will remove ALL your waypoints, type \"reallyreallyconfirm\" to confirm."));
            }, "Deletes ALL your waypoints."));

            RegisterSubCommand("enableall", new SubCommand((player, groupId, args) =>
            {
                config.DisabledColors.Clear();
            }, "Enables all waypoint colors."));

            RegisterSubCommand("save", new SubCommand((player, groupId, args) =>
            {
                ConfigLoader.SaveConfig(capi);
            }, "Force save configuration file."));

            RegisterSubCommand("export", new SubCommand((player, groupId, args) =>
            {
                string filePath = Path.Combine(GamePaths.DataPath, args.PopWord() ?? "waypoints");
                lock (MassFileExportSystem.toExport)
                {
                    MassFileExportSystem.toExport.Push(new ExportableJsonObject(utils.WaypointsRel, filePath));
                }
            }, "Exports waypoints as a JSON file located in your game data folder."));

            RegisterSubCommand("import", new SubCommand((player, groupId, args) =>
            {
                string path1 = Path.Combine(GamePaths.DataPath, args.PopWord() ?? "waypoints") + ".json";
                if (File.Exists(path1))
                {
                    using (TextReader reader = new StreamReader(path1))
                    {
                        DummyWaypoint[] relative = JsonConvert.DeserializeObject<DummyWaypoint[]>(reader.ReadToEnd(), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                        VSHUDTaskSystem.Actions.Enqueue(new Action(() =>
                        {
                            for (int j = 0; j < relative.Length; j++)
                            {
                                var val = relative[j];
                                if (utils.WaypointsRel.Any(w =>
                                val.Position.AsBlockPos.X == w.Position.AsBlockPos.X &&
                                val.Position.AsBlockPos.Y == w.Position.AsBlockPos.Y &&
                                val.Position.AsBlockPos.Z == w.Position.AsBlockPos.Z
                                )) continue;

                                string str = string.Format(CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", val.Icon, val.Position.X, val.Position.Y, val.Position.Z, val.Pinned, ColorUtil.Int2Hex(val.Color), val.Title);
                                capi.SendChatMessage(str);
                            }

                            capi.SendMyWaypoints();
                        }));
                        reader.Close();
                    }
                }
            }, "Imports waypoints from a JSON file located in your game data folder, default name waypoints.json, if not, provide filename excluding .json."));

            RegisterSubCommand("testlimits", new SubCommand((player, groupId, args) =>
            {
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

                            string icon = deathWaypoint ? "rocks" : WaypointUtils.iconKeys[(int)(capi.World.Rand.NextDouble() * (WaypointUtils.iconKeys.Length - 1))];
                            string title = deathWaypoint ? "Limits Test Player Death Waypoint" : "Limits Test Waypoint";

                            string str = string.Format(CultureInfo.InvariantCulture, "/waypoint addati {0} ={1} ={2} ={3} {4} #{5} {6}", icon, pos.X, pos.Y, pos.Z, deathWaypoint, ColorStuff.RandomHexColorVClamp(capi, 0.5, 0.8), title);

                            capi.SendChatMessage(str);
                        }
                        else i--;
                    }

                    capi.SendMyWaypoints();
                }));
            }, "Creates and uploads many waypoints to the server to debug the limits of VSHUD's floaty waypoint rendering system, inputs 3 integers (amount, maxY, radius)."));

            RegisterSubCommand("pillars", new SubCommand((player, groupId, args) =>
            {
                config.ShowPillars = args.PopBool() ?? !config.ShowPillars;
                capi.ShowChatMessage("Waypoint Pillars Set To " + config.ShowPillars + ".");
            }, "Toggles the rendering of the pillars that accompany the floaty waypoint UIs. True/False"));

            RegisterSubCommand("shuffle", new SubCommand((player, groupId, args) =>
            {
                lock (FloatyWaypointManagement.WaypointElements)
                {
                    if (FloatyWaypointManagement.WaypointElements != null && FloatyWaypointManagement.WaypointElements.Count > 1)
                    {
                        HudElementWaypoint[] wps = new HudElementWaypoint[FloatyWaypointManagement.WaypointElements.Count];
                        FloatyWaypointManagement.WaypointElements.TryPopRange(wps);
                        wps.Shuffle(new LCGRandom(468963));
                        FloatyWaypointManagement.WaypointElements.PushRange(wps);
                    }
                }
            }, "Shuffles the internal floaty waypoint UI stack to debug if changes are handled correctly."));

            RegisterSubCommand("echo", new SubCommand((player, groupId, args) =>
            {
                config.Echo = (bool)args.PopBool(!config.Echo);
                capi.ShowChatMessage(string.Format("Waypoint creation echoing set to {0}.", config.Echo));
            }, "Toggles the logging to chat of waypoint creation/deletion. Useful if you have a mod that creates many waypoints. True/False"));
        }
    }
}
