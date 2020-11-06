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
        public static bool mainThreadProcessing = false;

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
            Update();
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

        public void OpenOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() => 
            {
                wp.TryOpen();
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }

        public void CloseOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() => 
            { 
                wp.TryClose();
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }

        public void DisposeOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() => 
            { 
                wp.Dispose();
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }

        public void RecomposeTextOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() =>
            {
                if (wp.Dirty)
                {
                    wp.waypoint = utils.WaypointsRel[wp.waypointID];
                    wp.UpdateEditDialog();
                    var dynText = wp.SingleComposer.GetDynamicText("text");
                    dynText.Font.Color = wp.dColor;
                    dynText.RecomposeText();

                    WaypointTextUpdateSystem.EnqueueIfNotAlreadyFast(wp);
                    wp.Dirty = false;
                }
                else if (wp.displayText || wp.IsOpened())
                {
                    WaypointTextUpdateSystem.EnqueueIfNotAlready(wp);
                }
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }
       
        public void CreateHudElemOnMainThread(int i, HudElementWaypoint[] arr)
        {
            mainThreadProcessing = true;
            var wpRel = utils.WaypointsRel;

            if (i >= wpRel.Length || wpRel.Length == 0)
            {
                mainThreadProcessing = false;
                return;
            }

            WaypointRelative wp = wpRel[i];

            capi.Event.EnqueueMainThreadTask(() => 
            {
                var notif = capi.ModLoader.GetModSystem<ModSystemNotification>();
                var elem = notif.CreateNotification(string.Format("Building Dialogs... {0}%", ((double)i / wpRel.Length * 100).ToString("F2")));
                elem.expiryTime = 0.01f;

                if (!(i >= arr.Length && arr.Length > 0))
                {
                    if (arr[i] != null) arr[i].waypoint = wp;
                    else
                    {
                        arr[i] = new HudElementWaypoint(capi, wp);
                    }
                    arr[i].UpdateEditDialog();
                    WaypointElements.Push(arr[i]);
                }
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
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
                            CloseOnMainThread(elem);
                            DisposeOnMainThread(elem);
                        }
                    }
                }
                else if (utils.Waypoints.Count > 0 && utils.Waypoints.Count != OpenedCount())
                {
                    if (!repopped) Repopulate();

                    foreach (var val in WaypointElements)
                    {
                        if (val.IsOpened() && (val.distance > utils.Config.DotRange || utils.Config.DisabledColors.Contains(val.waypoint.Color)) && (!val.DialogTitle.Contains("*") || val.waypoint.OwnWaypoint.Pinned)) CloseOnMainThread(val);
                        else if (!val.IsOpened() && (val.distance < utils.Config.DotRange && !utils.Config.DisabledColors.Contains(val.waypoint.Color)) || (val.DialogTitle.Contains("*") || val.waypoint.OwnWaypoint.Pinned)) OpenOnMainThread(val);
                    }
                }
                foreach (var val in WaypointElements)
                {
                    RecomposeTextOnMainThread(val);
                }
            }
            else
            {
                foreach (var val in WaypointElements)
                {
                    if (val.IsOpened())
                    {
                        CloseOnMainThread(val);
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
                    CloseOnMainThread(elem);
                    DisposeOnMainThread(elem);
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
                CreateHudElemOnMainThread(i, arr);
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
