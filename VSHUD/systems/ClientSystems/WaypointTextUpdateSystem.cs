using System;
using System.Collections.Concurrent;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    class WaypointTextUpdateSystem : ClientSystem
    {
        ICoreClientAPI capi;
        public static ConcurrentStack<HudElementWaypoint> TextTasks = new ConcurrentStack<HudElementWaypoint>();

        public WaypointTextUpdateSystem(ClientMain game) : base(game)
        {
            capi = game.Api as ICoreClientAPI;
        }

        public override string Name => "WaypointTextUdate";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt)
        {
            for (int i = 0; i < TextTasks.Count; i++)
            {
                if (TextTasks.TryPop(out HudElementWaypoint hudElemWaypoint))
                {
                    UpdateDialog(hudElemWaypoint);
                }
            }
        }

        public void UpdateDialog(HudElementWaypoint hudElemWaypoint)
        {
            if (!hudElemWaypoint.IsOpened() || hudElemWaypoint.waypoint.OwnWaypoint == null) return;

            UpdateTitle(hudElemWaypoint);
            hudElemWaypoint.distance = capi.World.Player.Entity.Pos.RoundedDistanceTo(hudElemWaypoint.waypointPos, 3);
            bool km = hudElemWaypoint.distance >= 1000;

            hudElemWaypoint.dialogText = hudElemWaypoint.DialogTitle.UcFirst() + " " + (km ? Math.Round(hudElemWaypoint.distance / 1000, 3) : hudElemWaypoint.distance).ToString("F3") + (km ? "km" : "m");
        }

        public void UpdateTitle(HudElementWaypoint hudElemWaypoint)
        {
            string wp = hudElemWaypoint.config.WaypointPrefix ? "Waypoint: " : "";
            wp = hudElemWaypoint.config.WaypointID ? wp + "ID: " + hudElemWaypoint.waypointID + " | " : wp;
            hudElemWaypoint.DialogTitle = hudElemWaypoint.waypoint.OwnWaypoint.Title != null ? wp + hudElemWaypoint.waypoint.OwnWaypoint.Title : "Waypoint: ";
        }
        
        public void Dispose() => Dispose(capi.World as ClientMain);

        public override void Dispose(ClientMain game)
        {
            base.Dispose(game);
            TextTasks.Clear();
        }
    }
}
