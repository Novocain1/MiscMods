using ProtoBuf;
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
using Vintagestory.Common;

namespace SortTest
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SortMessage
    {
        public string mode;
        public string inv;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SortResponse
    {
        public string mode;
        public string inv;
    }

    class ItemSortTest : ModSystem
    {
        ICoreAPI api;
        static ICoreClientAPI capi;
        static ICoreServerAPI sapi;
        IClientNetworkChannel cChannel;
        IServerNetworkChannel sChannel;
        IClientPlayer cPlayer;
        int[] sorting;
        uint index = 0;
        string laststring;

        private bool a = true;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            sorting = new int[Enum.GetNames(typeof(EnumSortMode)).Length];
            for (int i = 0; i < sorting.Length; i++) sorting[i] = i;
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
            capi.Input.RegisterHotKey("sortmodifier", "Sort Modifier", GlKeys.LShift, HotkeyType.GUIOrOtherControls);
            capi.Input.SetHotKeyHandler("sort", SortKey);
        }

        public void OnClientMessage(IPlayer fromPlayer, SortResponse networkMessage)
        {
            sChannel.BroadcastPacket(new SortMessage()
            {
                mode = networkMessage.mode,
                inv = networkMessage.inv
            });
            int mode = int.Parse(networkMessage.mode);
            string inv = networkMessage.inv;
            StartSort(fromPlayer, (EnumSortMode)mode, inv);
        }

        public void OnServerMessage(SortMessage networkMessage)
        {
            int mode = int.Parse(networkMessage.mode);

            string inv = networkMessage.inv;

            StartSort(cPlayer, (EnumSortMode)mode, inv);
            capi.Gui.PlaySound("tick");

            if (laststring != "Sort By " + ((EnumSortMode)mode).ToString())
            {
                laststring = "Sort By " + ((EnumSortMode)mode).ToString();
                capi.ShowChatMessage(laststring);
            }
        }

        private bool SortKey(KeyCombination key)
        {
            if (a)
            {
                a = false;
                int modid = capi.Input.GetHotKeyByCode("sortmodifier").CurrentMapping.KeyCode;
                capi.World.RegisterCallback(dt => a = true, 50);

                string invid = "";
                if (capi.World.Player.InventoryManager.CurrentHoveredSlot != null)
                {
                    invid = capi.World.Player.InventoryManager.CurrentHoveredSlot.Inventory.InventoryID;
                }

                if (capi.Input.KeyboardKeyStateRaw[modid])
                {
                    cChannel.SendPacket(new SortResponse()
                    {
                        mode = sorting.Next(ref index).ToString(),
                        inv = invid
                    });
                }
                else
                {
                    cChannel.SendPacket(new SortResponse()
                    {
                        mode = sorting[index].ToString(),
                        inv = invid
                    });
                }
            }

            return true;
        }

        public void StartSort(IPlayer player, EnumSortMode mode, string invstring)
        {
            if (invstring != "")
            {
                IInventory inv = player.InventoryManager.GetInventory(invstring);
                Sort(player, mode, inv);
            }
            else
            {
                for (int i = 0; i < player.InventoryManager.OpenedInventories.Count; i++)
                {
                    Sort(player, mode, player.InventoryManager.OpenedInventories[i]);
                }
            }
        }

        public void Sort(IPlayer player, EnumSortMode mode, IInventory activeinv)
        {
            string name = activeinv.ClassName;

            if (name == "basket" || name == "chest" || name == "hotbar" || name == "backpack")
            {
                List<ItemStack> objects = activeinv.Sort(mode);
                if (objects.Count == 0) return;

                for (int j = 0; j < activeinv.Count; j++)
                {
                    if (activeinv[j] is ItemSlotOffhand) continue;

                    if (activeinv[j].Itemstack != null)
                    {
                        if (activeinv[j].Itemstack.Attributes["backpack"] != null) continue;

                        activeinv[j].TakeOutWhole();
                    }
                    for (int o = objects.Count - 1; o >= 0; o--)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, 1);
                        DummySlot slot = new DummySlot(objects[o]);

                        slot.TryPutInto(activeinv[j], ref op);
                        if (op.MovedQuantity > 0)
                            objects.RemoveAt(o);
                        else break;
                    }
                }
            }
        }
    }

    public static class Sorting
    {
        public static ItemStack[] ToItemStacks(this IInventory slots)
        {
            List<ItemStack> objects = new List<ItemStack>();
            
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Itemstack != null && !(slots[i] is ItemSlotOffhand) && slots[i].Itemstack.Attributes["backpack"] == null)
                {
                    for (int j = 0; j < slots[i].Itemstack.StackSize; j++)
                    {
                        ItemStack tempstack = slots[i].Itemstack.Clone();
                        tempstack.StackSize = 1;
                        objects.Add(tempstack);
                    }
                }
            }
            return objects.ToArray();
        }
        
        public static List<ItemStack> Sort(this IInventory inv, EnumSortMode mode) => inv.ToItemStacks().Sort(mode).ToList();
        public static ItemStack[] Sort(this ItemStack[] arr, EnumSortMode mode)
        {
            switch (mode)
            {
                case EnumSortMode.ID:
                    Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return a.Collectible.Id.CompareTo(b.Collectible.Id); });
                    break;
                case EnumSortMode.Name:
                    Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return a.Collectible.Code.ToString().CompareTo(b.Collectible.Code.ToString()); });
                    break;
                case EnumSortMode.Type:
                    Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return a.Collectible.ItemClass.CompareTo(b.Collectible.ItemClass); });
                    break;
                case EnumSortMode.MatterState:
                    Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return a.Collectible.MatterState.CompareTo(b.Collectible.MatterState); });
                    break;
                case EnumSortMode.Durability:
                    Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return a.Collectible.Durability.CompareTo(b.Collectible.Durability); });
                    break;
                default:
                    Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return a.Id.CompareTo(b.Id); });
                    break;
            }
            return arr;
        }

        public static T Next<T>(this T[] array, ref uint index)
        {
            index = (uint)(++index % array.Length);
            return array[index];
        }
    }

    public enum EnumSortMode
    {
        ID,
        Name,
        Type,
        MatterState,
        Durability,
    }
}
