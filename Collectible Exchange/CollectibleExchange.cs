using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Collectible_Exchange
{
    public class CollectibleExchange : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockEntityClass("Shop", typeof(BlockEntityShop));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.RegisterCommand("cec", "Allows you to create a collectible exchange from the chest you are looking at.", "", (byPlayer, id, args) =>
            {
                string arg = args.PopWord();
                switch (arg)
                {
                    case "create":
                        BlockPos pos = byPlayer?.CurrentBlockSelection?.Position;
                        if (pos != null)
                        {
                            BlockEntityGenericTypedContainer be = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer);
                            if (be != null)
                            {
                                List<Exchange> exchanges = new List<Exchange>();
                                Exchange exchange = new Exchange();

                                foreach (var val in be.Inventory)
                                {
                                    if (val.Itemstack != null)
                                    {
                                        if (exchange.Input == null)
                                        {
                                            exchange.Input = val.Itemstack;
                                        }
                                        else if (exchange.Output == null)
                                        {
                                            exchange.Output = val.Itemstack;
                                            if (exchange.Output != null)
                                            {
                                                exchanges.Add(exchange);
                                                exchange = new Exchange();
                                            }
                                        }
                                    }
                                }
                                api.World.BlockAccessor.RemoveBlockEntity(pos);
                                string a = api.World.BlockAccessor.GetBlock(pos).EntityClass;
                                api.World.BlockAccessor.SpawnBlockEntity("Shop", pos);
                                BlockEntityShop beShop = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShop);
                                beShop.inventory = (InventoryGeneric)be.Inventory;
                                beShop.Exchanges = exchanges;
                            }
                        }
                        break;
                    case "reset":
                        break;
                    case "append":
                        break;
                    default:
                        break;
                }

            });
        }
    }

    public class BlockEntityShop : BlockEntityGenericTypedContainerModified
    {
        public override InventoryGeneric inventory { get; set; }
        public override InventoryBase Inventory => inventory;
        public List<Exchange> Exchanges { get; set; } = new List<Exchange>();

        public override void Initialize(ICoreAPI api)
        {
            api.World.RegisterCallback(dt => 
            {
                base.Initialize(api);
                api.World.PlaySoundAt(AssetLocation.Create("sounds/effect/latch"), pos.X, pos.Y, pos.Z);
            }, 30);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (api.Side.IsServer())
            {
                foreach (var val in Exchanges)
                {
                    ItemSlot active = byPlayer.InventoryManager.ActiveHotbarSlot;
                    if (ExchangePossible(val, active))
                    {
                        foreach (var slot in inventory)
                        {
                            if (active.TryPutInto(api.World, slot, val.Input.StackSize) == val.Input.StackSize)
                            {
                                foreach (var invslot in inventory)
                                {
                                    if (invslot.Itemstack?.Collectible.Code.ToString() == val.Output.Collectible.Code.ToString())
                                    {
                                        DummySlot dummySlot = new DummySlot();
                                        invslot.TryPutInto(api.World, dummySlot, val.Output.StackSize);
                                        if (!byPlayer.InventoryManager.TryGiveItemstack(dummySlot.Itemstack))
                                        {
                                            byPlayer.Entity.World.SpawnItemEntity(dummySlot.Itemstack, pos.ToVec3d());
                                        }
                                        api.World.PlaySoundAt(AssetLocation.Create("sounds/effect/cashregister"), pos.X, pos.Y, pos.Z);
                                        break;
                                    }
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return base.OnPlayerRightClick(byPlayer, blockSel);
        }

        public bool ExchangePossible(Exchange exchange, ItemSlot slot)
        {
            if (slot.Itemstack == null || exchange.Input == null || exchange.Output == null) return false;
            return inventory.Any(a => a.CanTakeFrom(slot)) && inventory.Any(a => (a.Itemstack?.Collectible?.Code?.ToString() == exchange.Output?.Collectible?.Code?.ToString() && a.Itemstack?.StackSize >= exchange.Output?.StackSize));
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            List <Exchange> e = new List<Exchange>();
            int count = tree.GetInt("ExchangesCount", 0);

            for (int i = 0; i < count; i++)
            {
                string strI = "Exchanges" + i + "Input", strO = "Exchanges" + i + "Output";
                if (tree.GetItemstack(strI) != null && tree.GetItemstack(strO) != null)
                {
                    tree.GetItemstack(strI).ResolveBlockOrItem(worldForResolving); tree.GetItemstack(strO).ResolveBlockOrItem(worldForResolving);
                    e.Add(new Exchange(tree.GetItemstack(strI), tree.GetItemstack(strO)));
                }
                else break;
            }
            Exchanges = e;

            base.FromTreeAtributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("ExchangesCount", Exchanges.Count);
            for (int i = 0; i < Exchanges.Count; i++)
            {
                string strI = "Exchanges" + i + "Input", strO = "Exchanges" + i + "Output";
                tree.SetItemstack(strI, Exchanges[i].Input);
                tree.SetItemstack(strO, Exchanges[i].Output);
            }
            base.ToTreeAttributes(tree);
        }

        public override string GetBlockInfo(IPlayer forPlayer)
        {
            string v = "";
            try { v = base.GetBlockInfo(forPlayer); } catch(Exception) { }
            StringBuilder builder = new StringBuilder(v).AppendLine().AppendLine("Exchanges:");
            foreach (var val in Exchanges)
            {
                string ib = val.Input.Class == EnumItemClass.Block ? "block-" : "item-", ob = val.Output.Class == EnumItemClass.Block ? "block-" : "item-";
                builder.AppendLine(val.Input.StackSize + "x " + Lang.Get(ib + val.Input.Collectible.Code.ToShortString()) + " For " + val.Output.StackSize + "x " + Lang.Get(ob + val.Output.Collectible.Code.ToShortString()));
            }
            return builder.ToString();
        }
    }

    public class Exchange
    {
        public Exchange(ItemStack input = null, ItemStack output = null)
        {
            Input = input;
            Output = output;
        }

        public ItemStack Input { get; set; }
        public ItemStack Output { get; set; }
    }
}
