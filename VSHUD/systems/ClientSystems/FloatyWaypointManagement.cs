using Vintagestory.Client.NoObf;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using System.Collections.Concurrent;
using System.Linq;

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
            Update();
        }

        public override string Name => "Floaty Waypoint Management";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => Process();

        public void Process()
        {
            capi.Event.EnqueueMainThreadTask(() =>
            {
                Update();
            }, "");
        }

        public int OpenedCount()
        {
            int i = 0;
            foreach (var val in WaypointElements)
            {
                if (val.IsOpened()) i++;
            }
            return i;
        }

        public void Update()
        {
            if (utils.Config.FloatyWaypoints)
            {
                bool repopped;
                if (repopped = WaypointElements.Count == 0) Repopulate();

                if (utils.Waypoints.Count < 1 && WaypointElements.Count > 0)
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
                else if (utils.Waypoints.Count > 0 && utils.Waypoints.Count != OpenedCount())
                {
                    if (!repopped) Repopulate();

                    foreach (var val in WaypointElements)
                    {
                        if (val.IsOpened() && (val.distance > utils.Config.DotRange || utils.Config.DisabledColors.Contains(val.waypoint.Color)) && (!val.DialogTitle.Contains("*") || val.waypoint.OwnWaypoint.Pinned)) val.TryClose();
                        else if (!val.IsOpened() && (val.distance < utils.Config.DotRange && !utils.Config.DisabledColors.Contains(val.waypoint.Color)) || (val.DialogTitle.Contains("*") || val.waypoint.OwnWaypoint.Pinned)) val.TryOpen();
                    }
                }
                foreach (var val in WaypointElements)
                {
                    if (val.Dirty)
                    {
                        val.waypoint = utils.WaypointsRel[val.waypointID];
                        val.UpdateEditDialog();
                        var dynText = val.SingleComposer.GetDynamicText("text");
                        dynText.Font.Color = val.dColor;
                        dynText.RecomposeText();
                        
                        WaypointTextUpdateSystem.EnqueueIfNotAlreadyFast(val);
                        val.Dirty = false;
                    }
                    else if (val.displayText || val.IsOpened())
                    {
                        WaypointTextUpdateSystem.EnqueueIfNotAlready(val);
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

        public void Repopulate()
        {
            HudElementWaypoint[] arr = new HudElementWaypoint[utils.WaypointsRel.Length];

            for (int i = utils.WaypointsRel.Length; i < WaypointElements.Count; )
            {
                if (WaypointElements.TryPop(out var elem))
                {
                    elem.TryClose();
                    elem.Dispose();
                    i++;
                }
            }

            for (int i = 0; WaypointElements.Count > 0 && i < arr.Length; )
            {
                if (WaypointElements.TryPop(out var elem))
                {
                    arr[i] = elem;
                    i++;
                }
            }

            for (int i = 0; i < utils.WaypointsRel.Length; i++)
            {
                if (arr[i] != null) arr[i].waypoint = utils.WaypointsRel[i];
                else
                {
                    arr[i] = new HudElementWaypoint(capi, utils.WaypointsRel[i]);
                }
                arr[i].UpdateEditDialog();
                WaypointElements.Push(arr[i]);
            }
        }

        public void Dispose() => Dispose(capi.World as ClientMain);

        public override void Dispose(ClientMain game)
        {
            base.Dispose(game);
            foreach (var val in WaypointElements)
            {
                val.TryClose();
                val.Dispose();
            }
            WaypointElements.Clear();
        }
    }
}
