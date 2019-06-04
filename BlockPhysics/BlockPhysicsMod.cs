using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace StandAloneBlockPhysics
{
    public class BlockPhysicsMod : ModSystem
    {
        ICoreServerAPI sapi;
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockBehaviorClass("UnstableFalling", typeof(AlteredBlockPhysics));
            api.RegisterBlockBehaviorClass("SupportBeam", typeof(BehaviorSupportBeam));
            api.RegisterBlockBehaviorClass("BreakIfFloating", typeof(BreakIfFloatingAndCollapse));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            api.World.RegisterGameTickListener(SuffocationAndStepWatch, 500);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            AirBar airBar = new AirBar(api);
            airBar.TryOpen();
        }

        public void SuffocationAndStepWatch(float dt)
        {
            foreach (var val in sapi.World.LoadedEntities)
            {
                Entity entity = val.Value;

                if (entity is EntityItem || entity == null || entity.ServerPos == null || !entity.Alive) continue;

                if (entity is EntityPlayer)
                {
                    EntityPlayer entityPlayer = entity as EntityPlayer;
                    if (entityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Survival) continue;
                }

                float height = entity.CollisionBox.Height;
                BlockPos pos = entity.ServerPos.AsBlockPos;
                double entityArea = entity.CollisionBox.Area();

                if (sapi.World.Rand.NextDouble() > 0.9 && entityArea > 0.6)
                {
                    sapi.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                }

                ITreeAttribute attribs = entity.WatchedAttributes.GetOrAddTreeAttribute("health");
                float? maxair = attribs.TryGetFloat("maxair");
                float? currentair = attribs.TryGetFloat("currentair");

                if (maxair == null || currentair == null)
                {
                    attribs.SetFloat("maxair", 1.0f);
                    attribs.SetFloat("currentair", 1.0f);
                    entity.WatchedAttributes.MarkPathDirty("health");
                    continue;
                }

                if (InBlockBounds(entity.ServerPos.XYZ, height, out float suff))
                {
                    if (currentair > 0)
                    {
                        attribs.SetFloat("currentair", (float)currentair - suff);
                        entity.WatchedAttributes.MarkPathDirty("health");
                    }
                    else
                    {
                        currentair = 0.0f;
                        attribs.SetFloat("currentair", (float)currentair);

                        DamageSource source = new DamageSource();
                        source.Source = EnumDamageSource.Drown;
                        entity.ReceiveDamage(source, (float)(sapi.World.Rand.NextDouble() * 2));
                    }
                }
                else if (currentair < 1.0)
                {
                    attribs.SetFloat("currentair", (float)currentair + 0.25f);
                    entity.WatchedAttributes.MarkPathDirty("health");
                }
                if (currentair > 1.0)
                {
                    currentair = 1.0f;
                    attribs.SetFloat("currentair", (float)currentair);
                }
            }
        }

        public bool InBlockBounds(Vec3d vec, float height, out float suffocation)
        {
            suffocation = 0.0f;

            vec.Sub(0.5, 0, 0.5).Add(0,height/2.0,0);

            BlockPos pos = new BlockPos((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z));
            Vec3d blockCenter = pos.ToVec3d().AddCopy(0.5, 0.5, 0.5);
            Block block = sapi.World.BlockAccessor.GetBlock(pos);
            double distance = Math.Sqrt(vec.SquareDistanceTo(blockCenter));
            if (block.IsLiquid() && distance < 1.5)
            {
                suffocation = 0.01f;
                return true;
            }
            if (block.Id == 0 || block.CollisionBoxes == null) return false;

            for (int i = 0; i < block.CollisionBoxes.Length; i++)
            {
                suffocation = 0.5f;
                if ((block.CollisionBoxes[i].Area() > 0.512) && distance < 1.11) return true;
            }
            return false;
        }
    }

    public class AirBar : HudElement
    {
        public AirBar(ICoreClientAPI capi) : base(capi)
        {
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds statbarbounds = ElementStdBounds.Statbar(EnumDialogArea.CenterBottom, 345).WithFixedAlignmentOffset(-250.0, -88.0);
            statbarbounds.WithFixedHeight(8);

            SingleComposer = capi.Gui.CreateCompo("airbar", statbarbounds)
                .AddStatbar(statbarbounds, new double[] { 255.0 / 66.0, 255.0 / 134.0, 255.0 / 244.0, 0.5 }, "airbar")
                .Compose();
            SingleComposer
                .GetStatbar("airbar").SetMinMax(0, 1.0f);

            base.OnOwnPlayerDataReceived();
        }

        public override void OnRenderGUI(float deltaTime)
        {
            ITreeAttribute tree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");

            float? currentair = tree.TryGetFloat("currentair");
            if (currentair != null)
            {
                GuiElementStatbar statbar = SingleComposer.GetStatbar("airbar");

                if (currentair != statbar.GetValue())
                {
                    SingleComposer.GetStatbar("airbar").SetValue((float)currentair);
                }
            }

            base.OnRenderGUI(deltaTime);
        }
    }

    public class AlteredBlockPhysics : BlockBehavior
    {
        Utilities util;
        BlockPos[] offset;
        BlockPos[] cardinal;
        ModSystemBlockReinforcement blockReinforcement;
        double resistance = 0.0;

        public AlteredBlockPhysics(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            util = new Utilities(api);
            base.OnLoaded(api);
            blockReinforcement = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            resistance = block is BlockSoil ? 0.5 : block.FirstCodePart() == "gravel" ? 0.15 : 0.0;

            offset = AreaMethods.AreaBelowOffsetList().ToArray();
            cardinal = AreaMethods.SphericalOffsetList(1).ToArray();
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
        {
            TryCollapse(world, pos);

            base.OnBlockPlaced(world, pos, ref handled);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            TryCollapse(world, pos);

            base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            world.RegisterCallbackUnique((vworld, vpos, dt) => 
            {
                world.BlockAccessor.GetBlock(pos.UpCopy()).OnNeighourBlockChange(world, pos.UpCopy(), pos);
                if ((world.Rand.NextDouble() > resistance && world.Rand.NextDouble() > 0.5) || util.Isolated(pos) || util.OverHangAtLimit(pos, 8))
                {
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                }
            }, pos, 30);
            base.OnBlockRemoved(world, pos, ref handling);
        }

        public void TryCollapse(IWorldAccessor world, BlockPos pos)
        {
            if (world.Side.IsClient()) return;
            BlockReinforcement r = new BlockReinforcement();
            r = blockReinforcement.GetReinforcment(pos);

            bool isolated = util.Isolated(pos);
            bool overhang = util.OverHangAtLimit(pos, 8);

            double currentresistance = r != null && (r.Strength > 0 || r.Locked) ? 1.0 : isolated || overhang ? -0.1 : resistance;
            double chance = world.Rand.NextDouble();

            if (chance < currentresistance || util.IsSupported(pos, block)) return;

            world.RegisterCallbackUnique((vworld, vpos, dt) =>
            {
                BlockPos dPos = pos.AddCopy(0, -1, 0);
                Block dBlock = world.BlockAccessor.GetBlock(dPos);

                if (dBlock.IsReplacableBy(block))
                {
                    util.MoveBlock(pos, pos.AddCopy(0, -1, 0));
                }
                else
                {
                    if (dBlock.CollisionBoxes != null && block.CollisionBoxes != null)
                    {
                        for (int i = 0; i < dBlock.CollisionBoxes.Length; i++)
                        {
                            if (dBlock.CollisionBoxes[i].Area() < 1)
                            {
                                world.BlockAccessor.BreakBlock(pos, null);
                                break;
                            }
                            else if (block.CollisionBoxes[i].Area() >= dBlock.CollisionBoxes[i].Area())
                            {
                                BeginMove(world, pos);
                                break;
                            }
                        }
                    }
                    else
                    {
                        BeginMove(world, pos);
                    }
                }
            }, pos, 30);
        }

        public void BeginMove(IWorldAccessor world, BlockPos pos)
        {
            List<BlockPos> possiblePos = new List<BlockPos>();
            for (int i = 0; i < offset.Length; i++)
            {
                BlockPos offs = pos.AddCopy(offset[i].X, offset[i].Y, offset[i].Z);
                Block oBlock = world.BulkBlockAccessor.GetBlock(offs);

                if (offs.Y < 0 || offs.Y > world.BlockAccessor.MapSizeY) continue;

                if (oBlock.IsReplacableBy(block))
                {
                    possiblePos.Add(offs);
                }
            }
            if (possiblePos.Count() > 0)
            {
                BlockPos toPos = possiblePos[(int)Math.Round((world.Rand.NextDouble() * (possiblePos.Count - 1)))];
                Block toBlock = world.BulkBlockAccessor.GetBlock(toPos);
                util.MoveBlock(pos, toPos);
            }
        }
    }

    public class BehaviorSupportBeam : BlockBehavior
    {
        public BehaviorSupportBeam(Block block) : base(block)
        {
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            if (world.BlockAccessor.GetBlock(pos).FirstCodePart() != block.FirstCodePart())
            {
                world.BlockAccessor.WalkBlocks(pos.AddCopy(-8, -8, -8), pos.AddCopy(8, 8, 8), (vBlock, vPos) =>
                {
                    if (vBlock.HasBehavior<BehaviorSupportBeam>() && vPos != pos) world.BlockAccessor.BreakBlock(vPos, null);
                });
            }
            base.OnBlockRemoved(world, pos, ref handling);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (blockSel == null) return;

            handHandling = EnumHandHandling.PreventDefault;

            var bA = byEntity.World.BlockAccessor;
            BlockPos pos = blockSel.Position;
            if (slot.Itemstack.StackSize > 2 && bA.GetBlock(pos.X, pos.Y + 3, pos.Z).IsReplacableBy(block) && blockSel.Face.Code == "up")
            {
                for (int i = 1; i <= 3 && bA.GetBlock(pos.X, pos.Y + i, pos.Z).IsReplacableBy(slot.Itemstack.Block); i++)
                {
                    slot.Itemstack.StackSize -= 1;
                    bA.SetBlock(block.BlockId, new BlockPos(pos.X, pos.Y + i, pos.Z));
                }
                slot.MarkDirty();
                if (byEntity.Api.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                }
            }
            else
            {
                handHandling = EnumHandHandling.NotHandled;
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
            }
            
        }
    }

    public class BreakIfFloatingAndCollapse : BlockBehaviorBreakIfFloating
    {
        Block placedblock;
        Utilities util;
        bool IsRock = false;

        public BreakIfFloatingAndCollapse(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            util = new Utilities(api);
            IsRock = block.FirstCodePart() == "rock";
            if (IsRock) placedblock = api.World.GetBlock(new AssetLocation("gravel-" + block.Variant["rock"]));

            base.OnLoaded(api);
        }
        public void WalkUpdate(IWorldAccessor world, BlockPos pos)
        {
            world.RegisterCallbackUnique((iworld, ipos, dt) =>
            {
                int range = world.Rand.Next(2, 8);
                world.BlockAccessor.WalkBlocks(ipos.AddCopy(-range), ipos.AddCopy(range), (b, bp) =>
                {
                    if (world.Rand.NextDouble() > 0.5)
                    {
                        world.BulkBlockAccessor.TriggerNeighbourBlockUpdate(bp);
                    }
                });
                world.BulkBlockAccessor.Commit();
            }, pos, 1000);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            WalkUpdate(world, pos);
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
        {
            if (world.Side.IsServer())
            {
                Block nBlock = world.BlockAccessor.GetBlock(neibpos);
                if (IsRock && !world.BlockAccessor.GetBlock(pos.UpCopy()).IsReplacableBy(block) && nBlock.Id == 0)
                {
                    if (placedblock != null && !util.IsSupported(pos, block) && world.Rand.NextDouble() > 0.95)
                    {
                        if (placedblock.Sounds.Break != null) world.PlaySoundAt(placedblock.Sounds.Break, pos.X, pos.Y, pos.Z);
                        world.BulkBlockAccessor.SetBlock(placedblock.BlockId, pos);
                        WalkUpdate(world, pos);
                    }
                }
            }
            base.OnNeighbourBlockChange(world, pos, neibpos, ref handled);
        }
    }

    public class Utilities
    {
        IBlockAccessor bA;
        IWorldAccessor world;
        ICoreAPI api;
        BlockPos[] cardinal;
        BlockPos[] supportarea;

        public Utilities(ICoreAPI api)
        {
            this.api = api;
            world = api.World;
            bA = api.World.BlockAccessor;
            cardinal = AreaMethods.SphericalOffsetList(1).ToArray();
            supportarea = AreaMethods.LargeAreaBelowOffsetList().ToArray();
        }

        public bool IsSupported(BlockPos pos, Block block)
        {
            for (int i = 0; i < supportarea.Length; i++)
            {
                BlockPos iPos = pos.AddCopy(supportarea[i].X, supportarea[i].Y, supportarea[i].Z);
                Block iBlock = bA.GetBlock(iPos);

                if (iBlock.HasBehavior<BehaviorSupportBeam>()) return !Isolated(pos);
            }
            return false;
        }

        public bool Isolated(BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(pos);
            for (int i = 0; i < cardinal.Length; i++)
            {
                BlockPos iPos = pos.AddCopy(cardinal[i].X, cardinal[i].Y, cardinal[i].Z);
                if (iPos == pos) continue;
                Block iBlock = world.BlockAccessor.GetBlock(iPos);
                if (!iBlock.IsReplacableBy(block))
                {
                    return false;
                }
            }
            return true;
        }

        public bool OverHangAtLimit(BlockPos pos, int limit = 8)
        {
            int j = 0;
            int i = 0;
            Block block = bA.GetBlock(pos);

            bA.WalkBlocks(pos.AddCopy(-8,-1,-8), pos.AddCopy(8,-1,8), (vBlock, vPos) =>
            {
                BlockPos uPos = vPos.UpCopy();
                Block uBlock = bA.GetBlock(uPos);

                if (!uBlock.IsReplacableBy(block) && uBlock.HasBehavior<AlteredBlockPhysics>())
                {
                    j++;
                    if (vBlock.IsReplacableBy(block)) i++;
                }
                
            });

            return i > limit || i >= j;
        }

        public void MoveBlock(BlockPos fromPos, BlockPos toPos)
        {
            Block block = bA.GetBlock(fromPos);
            if (block.EntityClass != null)
            {
                TreeAttribute attribs = new TreeAttribute();
                BlockEntity be = bA.GetBlockEntity(fromPos);
                if (be != null)
                {
                    be.ToTreeAttributes(attribs);
                    attribs.SetInt("posx", toPos.X);
                    attribs.SetInt("posy", toPos.Y);
                    attribs.SetInt("posz", toPos.Z);

                    bA.SetBlock(0, fromPos);
                    bA.SetBlock(block.BlockId, toPos);

                    BlockEntity be2 = bA.GetBlockEntity(toPos);

                    if (be2 != null) be2.FromTreeAtributes(attribs, api.World);
                }
            }
            else
            {
                bA.SetBlock(0, fromPos);
                bA.SetBlock(block.BlockId, toPos);
                api.World.SpawnCubeParticles(toPos, toPos.ToVec3d().Add(0.5), 4, 32);
            }
        }
    }

    public static class MiscUtilities
    {
        public static double Area(this Cuboidf cuboid)
        {
            return (cuboid.Length * cuboid.Width * cuboid.Height);
        }
    }

    public class HAX
    {
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
    }
}
