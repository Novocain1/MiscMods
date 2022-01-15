using Vintagestory.Client.NoObf;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using System.Collections.Concurrent;
using System.Linq;
using System;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    public class FloatyWaypointManagement : ClientSystem
    {
        public static ConcurrentStack<HudElementWaypoint> WaypointElements { get => instance.waypointElements; }
        public ConcurrentStack<HudElementWaypoint> waypointElements = new ConcurrentStack<HudElementWaypoint>();
        public static bool mainThreadProcessing = false;

        ICoreClientAPI capi;
        WaypointUtils utils;
        public static bool forceRepop = false;

        public static void TriggerRepopulation() => forceRepop = true;

        public static FloatyWaypointManagement instance;

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
            instance = this;
        }

        public static string GetWaypointsHash()
        {
            lock (WaypointElements)
            {
                if (WaypointElements != null && WaypointElements.Count > 0)
                {
                    string str = "";
                    var wps = new HudElementWaypoint[WaypointElements.Count];
                    WaypointElements.CopyTo(wps, 0);
                    Array.Sort(wps, delegate (HudElementWaypoint a, HudElementWaypoint b) {
                        var c = (a?.waypoint?.Index ?? 0).CompareTo(b?.waypoint?.Index ?? 0);
                        return c; 
                    });

                    foreach (var wp in wps)
                    {
                        str += wp?.waypoint?.Title ?? "";
                        str += wp?.waypoint?.Index ?? 00;
                    }
                    return ExtraMath.Sha512Hash(str);
                }
                else return null;
            }
        }

        public override string Name => "Floaty Waypoint Management";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => Process();

        public void Process()
        {
            Update();
        }


        public bool Dirty { get => WaypointElements.Any(a => a.Dirty); }


        public HudElementWaypoint[] Openable
        {
            get
            {
                Stack<HudElementWaypoint> openable = new Stack<HudElementWaypoint>();
                foreach (var val in WaypointElements)
                {
                    if (val.Openable) openable.Push(val);
                }
                return openable.ToArray();
            }
        }

        public HudElementWaypoint[] Closeable
        {
            get
            {
                Stack<HudElementWaypoint> closeable = new Stack<HudElementWaypoint>();
                foreach (var val in WaypointElements)
                {
                    if (val.Closeable) closeable.Push(val);
                }
                return closeable.ToArray();
            }
        }

        public HudElementWaypoint[] Opened
        {
            get
            {
                Stack<HudElementWaypoint> opened = new Stack<HudElementWaypoint>();
                foreach (var val in WaypointElements)
                {
                    if (val.IsOpened()) opened.Push(val);
                }
                return opened.ToArray();
            }
        }

        public HudElementWaypoint[] ShouldBeVisible
        {
            get
            {
                Stack<HudElementWaypoint> visible = new Stack<HudElementWaypoint>();
                foreach (var val in WaypointElements)
                {
                    if (val.ShouldBeVisible) visible.Push(val);
                }
                return visible.ToArray();
            }
        }

        public void OpenOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() => 
            {
                wp?.TryOpen();
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }

        public void CloseOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() => 
            { 
                wp?.TryClose();
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }

        public void DisposeOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() => 
            { 
                wp?.Dispose();
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }

        public void RecomposeTextOnMainThread(HudElementWaypoint wp)
        {
            mainThreadProcessing = true;
            capi.Event.EnqueueMainThreadTask(() =>
            {
                if (wp != null)
                {
                    if (wp.Dirty)
                    {
                        if (wp.waypointID <= utils.WaypointsRel.Length && utils.WaypointsRel.Length > 0) 
                        {
                            try
                            {
                                wp.waypoint = utils.WaypointsRel[wp.waypointID];
                                wp.UpdateEditDialog();
                                var dynText = wp.SingleComposer.GetDynamicText("text");
                                dynText.Font.Color = wp.dColor;
                                dynText.RecomposeText();

                                utils.waypointTextUpdateSystem.EnqueueIfNotAlreadyFast(wp);
                                wp.Dirty = false;
                            }
                            catch (System.IndexOutOfRangeException)
                            {

                            }

                        }
                    }
                    else if (wp.displayText || wp.IsOpened())
                    {
                        utils.waypointTextUpdateSystem.EnqueueIfNotAlready(wp);
                    }
                }
                mainThreadProcessing = false;
            }, "");
            while (mainThreadProcessing) ;
        }
        
        public void CreateOrUpdateHudElemOnMainThread(int i, HudElementWaypoint[] arr)
        {
            mainThreadProcessing = true;
            var wpRel = utils.WaypointsRel;

            if (i >= wpRel.Length || wpRel.Length == 0) return;

            WaypointRelative wp = wpRel[i];

            capi.Event.EnqueueMainThreadTask(() =>
            {
                var notif = capi.ModLoader.GetModSystem<ModSystemNotification>();
                notif.CreateNotification(string.Format("Rebuilding Waypoint Dialogs... {0}%", ((double)i / wpRel.Length * 100).ToString("F2")), null, 2.0f);

                if (arr[i] != null) arr[i].waypoint = wp;
                else arr[i] = new HudElementWaypoint(capi, wp);

                arr[i].UpdateEditDialog();

                if (i == arr.Length - 1)
                {
                    notif.CreateNotification("Rebuilding Waypoint Dialogs... 100.00%", null, 2.0f);
                }

                mainThreadProcessing = false;

            }, "");
            while (mainThreadProcessing) ;
        }

        public void Update()
        {
            if (capi.IsGamePaused) return;

            if (utils.Config.FloatyWaypoints)
            {
                Repopulate();
                
                foreach (var val in Closeable) CloseOnMainThread(val);
                foreach (var val in Openable) OpenOnMainThread(val);
                foreach (var val in Opened) RecomposeTextOnMainThread(val);
            }
            else
            {
                foreach (var val in Opened) CloseOnMainThread(val);
            }
        }

        public void Repopulate()
        {
            if (!forceRepop && (WaypointUtils.doingConfigAction || (WaypointElements.Count == utils.WaypointsRel.Length && !Dirty)))
            {
                forceRepop = false;
                return;
            }

            Stack<HudElementWaypoint> stack = new Stack<HudElementWaypoint>();

            //move waypoints to temp stack within range of updated waypoint count
            for (int i = 0; i < utils.WaypointsRel.Length; i++)
            {
                HudElementWaypoint elem;

                while (!WaypointElements.TryPop(out elem) && WaypointElements.Count > 0) ;
                stack.Push(elem);
            }

            //dispose any extras
            int c = WaypointElements.Count;
            for (int i = 0; i < c; i++)
            {
                HudElementWaypoint elem;

                while (!WaypointElements.TryPop(out elem) && WaypointElements.Count > 0) ;
                CloseOnMainThread(elem);
                DisposeOnMainThread(elem);
            }

            //create or update hud elements from temp stack then push back onto main stack
            var arr = stack.ToArray();

            for (int i = 0; i < arr.Length; i++)
            {
                CreateOrUpdateHudElemOnMainThread(i, arr);
            }

            if (arr.Length > 0) WaypointElements.PushRange(arr);
            forceRepop = false;
        }

        public void Dispose() => Dispose(capi.World as ClientMain);

        public override void Dispose(ClientMain game)
        {
            if (game != null) base.Dispose(game);
            foreach (var val in WaypointElements)
            {
                val?.TryClose();
                val?.Dispose();
            }
            WaypointElements?.Clear();
            capi = null;
        }
    }
}
