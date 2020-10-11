using Vintagestory.Client.NoObf;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using System.Collections.Concurrent;

namespace VSHUD
{
    class FloatyWaypointManagement : ClientSystem
    {
        public static ConcurrentStack<HudElementWaypoint> WaypointElements { get; set; } = new ConcurrentStack<HudElementWaypoint>();
        ICoreClientAPI capi;
        WaypointUtils utils;

        public FloatyWaypointManagement(ClientMain game, WaypointUtils utils) : base(game) 
        {
            capi = game.Api as ICoreClientAPI;
            this.utils = utils;
            capi.Input.RegisterHotKey("editfloatywaypoint", "Edit Floaty Waypoint", GlKeys.R, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("editfloatywaypoint", (k) =>
            {
                foreach (var val in WaypointElements)
                {
                    if (val.isAligned)
                    {
                        val.OpenEditDialog();
                        return true;
                    }
                }
                return false;
            });
            Populate();
        }

        public override string Name => "Floaty Waypoint Management";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.HudElement;

        public override void OnSeperateThreadGameTick(float dt) => Process();

        public void Process()
        {
            capi.Event.EnqueueMainThreadTask(() =>
            {
                Update();
            }, "");
        }

        public void Update()
        {
            if (utils.Config.FloatyWaypoints)
            {
                bool repopped;
                if (repopped = WaypointElements.Count == 0) Populate();

                if (utils.Waypoints.Count > 0 && utils.Waypoints.Count != WaypointElements.Count)
                {
                    if (!repopped) Populate();

                    foreach (var val in WaypointElements)
                    {
                        if (val.IsOpened() && (val.distance > utils.Config.DotRange || utils.Config.DisabledColors.Contains(val.waypoint.Color)) && (!val.DialogTitle.Contains("*") || val.waypoint.OwnWaypoint.Pinned)) val.TryClose();
                        else if (!val.IsOpened() && (val.distance < utils.Config.DotRange && !utils.Config.DisabledColors.Contains(val.waypoint.Color)) || (val.DialogTitle.Contains("*") || val.waypoint.OwnWaypoint.Pinned)) val.TryOpen();
                    }
                }
                else if (utils.Waypoints.Count < 1 && WaypointElements.Count > 0)
                {
                    for (int i = 0; i < WaypointElements.Count; i++)
                    {
                        if (WaypointElements.TryPop(out var elem))
                        {
                            elem.TryClose();
                            elem.Dispose();
                        }
                    }
                }
            }
            else
            {
                foreach (var val in WaypointElements)
                {
                    if (val.IsOpened())
                    {
                        val.TryClose();
                    }
                }
            }
        }

        public void Populate()
        {
            foreach (var val in utils.WaypointsRel)
            {
                WaypointElements.Push(new HudElementWaypoint(capi, val));
            }

            foreach (var val in WaypointElements)
            {
                val.TryClose();
            }
            WaypointElements.Clear();

            foreach (var val in utils.WaypointsRel)
            {
                HudElementWaypoint waypoint = new HudElementWaypoint(capi, val);
                waypoint.OnOwnPlayerDataReceived();

                WaypointElements.Push(waypoint);
            }
        }
    }
}
