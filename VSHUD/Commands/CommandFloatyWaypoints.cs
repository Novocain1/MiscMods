using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Action = Vintagestory.API.Common.Action;

namespace VSHUD.Commands
{
    public class VSHUDCommandsRegistry : ClientModSystem
    {
        public override double ExecuteOrder() => 1.0;

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.RegisterCommand(new CommandFloatyWaypoints(typeof(EnumCmdArgsFloatyWaypoints), api, api.ModLoader.GetModSystem<WaypointUtils>()));
        }
    }

    public enum EnumCmdArgsFloatyWaypoints
    {
        deathdebug, dotrange, titlerange, perblockwaypoints, pdw, plt, open, waypointprefix, waypointid, purge, enableall, save, export, import, testlimits, pillars, shuffle
    }

    public class ClientChatCommandArgProvided : ClientChatCommand
    {
        public readonly Type ArgType;

        public override string GetSyntax()
        {
            StringBuilder bdr = new StringBuilder('[');
            var vals = Enum.GetValues(ArgType);
            int i = 0;
            foreach (var val in vals)
            {
                bdr.Append(Enum.GetName(ArgType, val));
                if (i < vals.Length) bdr.Append('|');
                i++;
            }
            bdr.Append(']');

            return bdr.ToString();
        }

        public override string GetHelpMessage()
        {
            return string.Format("Syntax: .{0} {1}", Command, GetSyntax());
        }

        public ICoreClientAPI capi;
        public VSHUDConfig Config;

        public ClientChatCommandArgProvided(Type argType, ICoreClientAPI capi)
        {
            if (!argType.IsEnum) throw new ArgumentException();
            this.capi = capi;
            Config = ConfigLoader.Config;

            ArgType = argType;
        }

        public virtual void GetEnumFromArgs<T>(ref CmdArgs args, out object value)
        {
            var word = args.PopWord();
            value = (word == null ? null : Enum.Parse(ArgType, word.ToLowerInvariant()));
        }

        public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
        {
            ConfigLoader.SaveConfig(capi);
        }
    }

    public class CommandFloatyWaypoints : ClientChatCommandArgProvided
    {
        WaypointUtils utils;

        public CommandFloatyWaypoints(Type argType, ICoreClientAPI capi, WaypointUtils utils) : base(argType, capi)
        {
            this.utils = utils;
            this.Command = "wpcfg";
        }

        public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
        {
            WaypointUtils.doingConfigAction = true;
            GetEnumFromArgs<EnumCmdArgsFloatyWaypoints>(ref args, out var arg);
            switch (arg)
            {
                case EnumCmdArgsFloatyWaypoints.deathdebug:
                    Config.DebugDeathWaypoints = args.PopBool() ?? !Config.DebugDeathWaypoints;
                    capi.ShowChatMessage(string.Format("Death waypoint debbuging set to {0}", Config.DebugDeathWaypoints));
                    break;
                case EnumCmdArgsFloatyWaypoints.dotrange:
                    double? dr = args.PopDouble();
                    Config.DotRange = dr != null ? (double)dr : Config.DotRange;
                    capi.ShowChatMessage("Dot Range Set To " + Config.DotRange + " Meters.");
                    break;
                case EnumCmdArgsFloatyWaypoints.titlerange:
                    double? tr = args.PopDouble();
                    Config.TitleRange = tr != null ? (double)tr : Config.TitleRange;
                    capi.ShowChatMessage("Title Range Set To " + Config.TitleRange + " Meters.");
                    break;
                case EnumCmdArgsFloatyWaypoints.perblockwaypoints:
                    bool? pb = args.PopBool();
                    Config.PerBlockWaypoints = pb != null ? (bool)pb : !Config.PerBlockWaypoints;
                    capi.ShowChatMessage("Per Block Waypoints Set To " + Config.PerBlockWaypoints + ".");
                    break;
                case EnumCmdArgsFloatyWaypoints.pdw:
                    utils.PurgeWaypointsByStrings("Player Death Waypoint", "*Player Death Waypoint*");
                    break;
                case EnumCmdArgsFloatyWaypoints.plt:
                    utils.PurgeWaypointsByStrings("Limits Test");
                    break;
                case EnumCmdArgsFloatyWaypoints.open:
                    utils.ViewWaypoints(new KeyCombination());
                    break;
                case EnumCmdArgsFloatyWaypoints.waypointprefix:
                    bool? wp = args.PopBool();
                    Config.WaypointPrefix = wp != null ? (bool)wp : !Config.WaypointPrefix;
                    capi.ShowChatMessage("Waypoint Prefix Set To " + Config.WaypointPrefix + ".");
                    break;
                case EnumCmdArgsFloatyWaypoints.waypointid:
                    bool? wi = args.PopBool();
                    Config.WaypointID = wi != null ? (bool)wi : !Config.WaypointID;
                    capi.ShowChatMessage("Waypoint ID Set To " + Config.WaypointID + ".");
                    break;
                case EnumCmdArgsFloatyWaypoints.purge:
                    string s = args.PopWord();
                    if (s == "reallyreallyconfirm") utils.Purge();
                    else capi.ShowChatMessage(Lang.Get("Are you sure you want to do that? It will remove ALL your waypoints, type \"reallyreallyconfirm\" to confirm."));
                    break;
                case EnumCmdArgsFloatyWaypoints.enableall:
                    Config.DisabledColors.Clear();
                    break;
                case EnumCmdArgsFloatyWaypoints.save:
                    break;
                case EnumCmdArgsFloatyWaypoints.export:
                    string filePath = Path.Combine(GamePaths.DataPath, args.PopWord() ?? "waypoints");
                    lock (MassFileExportSystem.toExport)
                    {
                        MassFileExportSystem.toExport.Push(new ExportableJsonObject(utils.WaypointsRel, filePath));
                    }
                    break;
                case EnumCmdArgsFloatyWaypoints.import:
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
                    break;
                case EnumCmdArgsFloatyWaypoints.testlimits:
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
                    break;
                case EnumCmdArgsFloatyWaypoints.pillars:
                    Config.ShowPillars = args.PopBool() ?? !Config.ShowPillars;
                    capi.ShowChatMessage("Waypoint Pillars Set To " + Config.ShowPillars + ".");
                    break;
                case EnumCmdArgsFloatyWaypoints.shuffle:
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
                    break;
                default:
                    capi.ShowChatMessage(GetHelpMessage());
                    break;
            }
            base.CallHandler(player, groupId, args);
            WaypointUtils.doingConfigAction = false;

            capi.SendMyWaypoints();
        }
    }
}
