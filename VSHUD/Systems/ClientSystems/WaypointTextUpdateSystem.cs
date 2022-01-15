using System;
using System.Collections.Concurrent;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using System.Linq;

namespace VSHUD
{
    public class WaypointTextUpdateSystem : ClientSystem
    {
        ICoreClientAPI capi;

        public void EnqueueIfNotAlreadyFast(HudElementWaypoint wp)
        {
            if (!priority.Contains(wp)) priority.Push(wp);
        }
        
        public void EnqueueIfNotAlready(HudElementWaypoint wp)
        {
            if (!textTasks.Contains(wp)) textTasks.Enqueue(wp);
        }

        private ConcurrentQueue<HudElementWaypoint> textTasks = new ConcurrentQueue<HudElementWaypoint>();
        private ConcurrentStack<HudElementWaypoint> priority = new ConcurrentStack<HudElementWaypoint>();

        public WaypointTextUpdateSystem(ClientMain game) : base(game)
        {
            capi = game.Api as ICoreClientAPI;
        }

        public override string Name => "WaypointTextUdate";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt)
        {
            for (int i = 0; i < priority.Count; i++)
            {
                if (priority.TryPop(out var elem))
                {
                    UpdateDialog(elem);
                }
            }
            
            for (int i = 0; i < textTasks.Count; i++)
            {
                if (textTasks.TryDequeue(out var elem))
                {
                    lock (elem)
                    {
                        UpdateDialog(elem);
                    }
                }
            }
        }

        public void UpdateDialog(HudElementWaypoint hudElemWaypoint)
        {
            if (!hudElemWaypoint.IsOpened() || hudElemWaypoint.waypoint.OwnWaypoint == null) return;

            UpdateTitle(hudElemWaypoint);
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
            for (int i = 0; i < textTasks.Count; )
            {
                if (textTasks.TryDequeue(out var a)) i++;
            }
        }
    }
}
