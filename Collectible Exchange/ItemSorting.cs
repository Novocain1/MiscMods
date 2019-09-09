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

namespace Collectible_Exchange
{
    public static class Sorting
	{
        public static void Sort(this IInventory activeInv, ICoreAPI api, EnumSortMode mode)
        {
            if (api == null || activeInv == null) return;
            string name = activeInv.ClassName;

            if (name == "basket" || name == "chest" || name == "hotbar" || name == "backpack")
            {
                List<ItemStack> objects = activeInv.SortArr(mode);

                for (int j = 0; j < activeInv.Count; j++)
                {
                    if (activeInv[j] is ItemSlotOffhand) continue;

                    if (activeInv[j].Itemstack != null)
                    {
                        if (activeInv[j].Itemstack.Attributes["backpack"] != null) continue;

                        activeInv[j].TakeOutWhole();
                    }
                    for (int o = objects.Count; o-- > 0;)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(api.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, 1);
                        DummySlot slot = new DummySlot(objects[o]);

                        slot.TryPutInto(activeInv[j], ref op);
                        if (op.MovedQuantity > 0)
                            objects.RemoveAt(o);
                        else break;
                    }
                }
            }
        }

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

		public static List<ItemStack> SortArr(this IInventory inv, EnumSortMode mode) => inv.ToItemStacks().SortArr(mode).ToList();
		public static ItemStack[] SortArr(this ItemStack[] arr, EnumSortMode mode)
		{
			switch (mode)
			{
				case EnumSortMode.Dictionary:
					Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return Lang.Get(b.Collectible.Code.ToString()).CompareTo(Lang.Get(a.Collectible.Code.ToString())); });
					break;
				case EnumSortMode.ID:
					Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return b.Collectible.Id.CompareTo(a.Collectible.Id); });
					break;
				case EnumSortMode.Code:
					Array.Sort(arr, delegate (ItemStack a, ItemStack b) { return b.Collectible.Code.ToString().CompareTo(a.Collectible.Code.ToString()); });
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
		Dictionary,
		ID,
		Code,
	}
}
