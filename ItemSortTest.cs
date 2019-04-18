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
        int[] sorting = new int[] { 0, 1, 2, 3 };
        uint index = 0;
        string laststring;

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
                capi.World.RegisterCallback(dt => a = true, 50);

                string invid = "";
                if (capi.World.Player.InventoryManager.CurrentHoveredSlot != null)
                {
                    invid = capi.World.Player.InventoryManager.CurrentHoveredSlot.Inventory.InventoryID;
                }

                if (capi.Input.KeyboardKeyStateRaw[1])
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
            if (name == "chest" || name == "hotbar" || name == "backpack")
            {
                List<CollectibleObject> objects = activeinv.Sort(mode);
                if (objects.Count == 0) return;

                for (int j = 0; j < activeinv.Count; j++)
                {
                    if (activeinv[j].Itemstack != null)
                    {
                        activeinv[j].TakeOutWhole();
                    }

                    for (int o = objects.Count - 1; o >= 0; o--)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, 1);
                        ItemStack stack = new ItemStack(objects[o], 1);
                        DummySlot slot = new DummySlot(stack);

                        slot.TryPutInto(activeinv[j], ref op);
                        if (op.MovedQuantity > 0)
                            objects.RemoveAt(o);
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
                case EnumSortMode.ID:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.Id.CompareTo(b.Id); });
                    break;
                case EnumSortMode.Name:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.Code.ToString().CompareTo(b.Code.ToString()); });
                    break;
                case EnumSortMode.Type:
                    Array.Sort(arr, delegate (CollectibleObject a, CollectibleObject b) { return a.ItemClass.CompareTo(b.ItemClass); });
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
    }
}
