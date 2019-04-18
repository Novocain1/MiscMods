﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace SortTest
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SortMessage
    {
        public string message;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SortResponse
    {
        public string response;
    }

    class ItemSortTest : ModSystem
    {
        ICoreAPI api;
        static ICoreClientAPI capi;
        static ICoreServerAPI sapi;
        IClientNetworkChannel cChannel;
        IServerNetworkChannel sChannel;
        IClientPlayer cPlayer;

        private bool a = true;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            sChannel =
                sapi.Network.RegisterChannel("sortchannel")
                .RegisterMessageType(typeof(SortMessage))
                .RegisterMessageType(typeof(SortResponse))
                .SetMessageHandler<SortResponse>(OnClientMessage);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            cChannel =
                capi.Network.RegisterChannel("sortchannel")
                .RegisterMessageType(typeof(SortMessage))
                .RegisterMessageType(typeof(SortResponse))
                .SetMessageHandler<SortMessage>(OnServerMessage);
            api.Event.PlayerJoin += RegisterSort;
        }

        private void RegisterSort(IClientPlayer byPlayer)
        {
            cPlayer = byPlayer;
            capi.Input.RegisterHotKey("sort", "Sort", GlKeys.I, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("sort", SortKey);
        }

        public void OnClientMessage(IPlayer fromPlayer, SortResponse networkMessage)
        {
            sChannel.BroadcastPacket(new SortMessage());
            Sort(fromPlayer);
        }

        public void OnServerMessage(SortMessage networkMessage)
        {
            Sort(cPlayer);
            capi.Gui.PlaySound("tick");
        }

        private bool SortKey(KeyCombination key)
        {
            if (a)
            {
                a = false;
                capi.World.RegisterCallback(dt => a = true, 500);

                cChannel.SendPacket(new SortResponse());
            }

            return true;
        }

        public void Sort(IPlayer player)
        {
            for (int i = 0; i < player.InventoryManager.OpenedInventories.Count; i++)
            {
                string name = player.InventoryManager.OpenedInventories[i].ClassName;
                if (name == "chest" || name == "hotbar" || name == "backpack")
                {
                    List<CollectibleObject> objects = player.InventoryManager.OpenedInventories[i].Sort(EnumSortMode.Id);
                    if (objects.Count == 0) continue;

                    for (int j = 0; j < player.InventoryManager.OpenedInventories[i].Count; j++)
                    {
                        if (player.InventoryManager.OpenedInventories[i][j].Itemstack != null)
                        {
                            player.InventoryManager.OpenedInventories[i][j].TakeOutWhole();
                        }

                        for (int o = objects.Count - 1; o >= 0; o--)
                        {
                            ItemStackMoveOperation op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, 1);
                            ItemStack stack = new ItemStack(objects[o], 1);
                            DummySlot slot = new DummySlot(stack);

                            slot.TryPutInto(player.InventoryManager.OpenedInventories[i][j], ref op);
                            if (op.MovedQuantity > 0)
                                objects.RemoveAt(o);
                        }
                    }
                }
            }
        }
    }

    public static class Sorting
    {
        public static List<CollectibleObject> ToCollectableList(this ItemSlot[] slots)
        {
            List<CollectibleObject> objects = new List<CollectibleObject>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Itemstack != null)
                {
                    for (int j = 0; j < slots[i].Itemstack.StackSize; j++)
                    {
                        objects.Add(slots[i].Itemstack.Collectible);
                    }
                }
            }
            return objects;
        }

        public static List<CollectibleObject> ToCollectablesList(this IInventory slots) => slots.ToArray().ToCollectableList();

        public static CollectibleObject[] ToCollectables(this IInventory slots) => slots.ToArray().ToCollectableList().ToArray();

        public static List<CollectibleObject> Sort(this IInventory slots, EnumSortMode mode) => slots.ToCollectables().Sort(mode).ToList();

        public static CollectibleObject[] Sort(this CollectibleObject[] arr, EnumSortMode mode)
        {
            switch (mode)
            {
                case EnumSortMode.Id:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.Id.CompareTo(b.Id); });
                    break;
                case EnumSortMode.Name:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.Code.ToString().CompareTo(b.Code.ToString()); });
                    break;
                case EnumSortMode.Type:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.Class.CompareTo(b.Class); });
                    break;
                case EnumSortMode.MatterState:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.MatterState.CompareTo(b.MatterState); });
                    break;
                default:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.Id.CompareTo(b.Id); });
                    break;
            }
            return arr;
        }

        public static List<T> ToList<T>(this T[] arr)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < arr.Length; i++) list.Add(arr[i]);
            return list;
        }
    }

    public enum EnumSortMode
    {
        Id,
        Name,
        Type,
        MatterState,
    }
}
