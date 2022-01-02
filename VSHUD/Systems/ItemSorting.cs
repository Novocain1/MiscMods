using System;
using System.Collections;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VSHUD
{
    class ItemSorting : ClientModSystem
    {
        ICoreClientAPI capi;
        IEnumerator sortingIterator;


        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            api.Input.RegisterHotKey("sortinv", "Sort Inventory", GlKeys.Y, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("sortinv", (keycomb) =>
            {
                inv = api.World.Player.InventoryManager.CurrentHoveredSlot?.Inventory;
                if (inv == null || inv.ClassName == GlobalConstants.creativeInvClassName || inv.ClassName == GlobalConstants.hotBarInvClassName) return true;
                
                sortingIterator = IterateOnReturned();

                inv.SlotModified += SortFunc;
                SortFunc(0);

                return true;
            });
        }

        public void SortFunc(int id)
        {
            var slots = inv.ToArray();
            slotsWID = new SlotIDTransfer[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                slotsWID[i] = new SlotIDTransfer(slots[i], i);
            }

            Array.Sort(slotsWID, (one, two) => one.slot.Itemstack?.Id.CompareTo(two.slot.Itemstack?.Id) ?? 0);

            for (int i = 0; i < slotsWID.Length; i++)
            {
                slotsWID[i].toId = i;
            }

            sortingIterator.MoveNext();
        }

        SlotIDTransfer[] slotsWID;
        InventoryBase inv;

        public IEnumerator IterateOnReturned()
        {
            for (int i = 0; i < slotsWID.Length; i++)
            {
                Swap(inv, slotsWID[i].fromId, slotsWID[i].toId);
                yield return null;
            }
            inv.SlotModified -= SortFunc;
        }

        public void Swap(InventoryBase inv, int a, int b)
        {
            var pack = inv.InvNetworkUtil.GetFlipSlotsPacket(inv, a, b);
            capi.Network.SendPacketClient(pack);
        }
    }
}
