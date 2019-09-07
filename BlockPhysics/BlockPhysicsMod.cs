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
    public class PhysicsBlock
    {
        private double friction;
        private bool hasPhysics;
        public PhysicsBlock()
        {
            friction = 0.0;
            hasPhysics = false;
        }

        public PhysicsBlock(double frictionIn, bool hasPhysicsIn)
        {
            friction = frictionIn;
            hasPhysics = hasPhysicsIn;
        }

        public PhysicsBlock(bool hasPhysicsIn)
        {
            friction = 1.0;
            hasPhysics = hasPhysicsIn;
        }

        public PhysicsBlock(double frictionIn)
        {
            friction = frictionIn;
            hasPhysics = true;
        }

        public double getFriction()
        {
            return friction;
        }

        public bool getHasPhysics()
        {
            return hasPhysics;
        }
    }

    public class PhysicsModConfig
    {
        public Dictionary<EnumBlockMaterial, double> FrictionTable { get; set; } = new Dictionary<EnumBlockMaterial, double>
        {
            [EnumBlockMaterial.Air] = 0.0,
            [EnumBlockMaterial.Brick] = 1.0,
            [EnumBlockMaterial.Ceramic] = 0.0,
            [EnumBlockMaterial.Cloth] = 0.0,
            [EnumBlockMaterial.Fire] = 0.0,
            [EnumBlockMaterial.Glass] = 0.45,
            [EnumBlockMaterial.Gravel] = 0.05,
            [EnumBlockMaterial.Ice] = 0.5,
            [EnumBlockMaterial.Lava] = 0.0,
            [EnumBlockMaterial.Leaves] = 0.7,
            [EnumBlockMaterial.Liquid] = 0.0,
            [EnumBlockMaterial.Mantle] = 0.0,
            [EnumBlockMaterial.Meta] = 0.0,
            [EnumBlockMaterial.Metal] = 0.0,
            [EnumBlockMaterial.Ore] = 1.0,
            [EnumBlockMaterial.Other] = 0.0,
            [EnumBlockMaterial.Plant] = 0.7,
            [EnumBlockMaterial.Sand] = 0.0,
            [EnumBlockMaterial.Snow] = 0.0,
            [EnumBlockMaterial.Soil] = 0.2,
            [EnumBlockMaterial.Stone] = 0.95,
            [EnumBlockMaterial.Wood] = 0.7
        };
    }


    public class BlockPhysicsMod : ModSystem
    {
        ICoreServerAPI sapi;
        ICoreAPI api;

        public PhysicsModConfig Config { get; private set; } = new PhysicsModConfig();

        public void LoadConfig()
        {
            if (sapi.LoadModConfig<PhysicsModConfig>("blockphysicsmod.json") == null) { SaveConfig(); return; }

            Config = sapi.LoadModConfig<PhysicsModConfig>("blockphysicsmod.json");
            SaveConfig();
        }

        public void SaveConfig() => sapi.StoreModConfig(Config, "blockphysicsmod.json");

        public override void Start(ICoreAPI api)
        {
            this.api = api;

            api.RegisterBlockBehaviorClass("UnstableFalling", typeof(AlteredBlockPhysics));
            api.RegisterBlockBehaviorClass("LegacyUnstableFalling", typeof(BlockBehaviorUnstableFalling));
            api.RegisterBlockBehaviorClass("SupportBeam", typeof(BehaviorSupportBeam));
            api.RegisterBlockBehaviorClass("BreakIfFloating", typeof(BreakIfFloatingAndCollapse));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            LoadConfig();

            api.World.RegisterGameTickListener(SuffocationAndStepWatch, 500);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            AirBar airBar = new AirBar(api);
            airBar.TryOpen();
        }

        public void AddBehaviorToAll()
        {
            if (api.World.Blocks != null)
            {
                for (int i = 0; i < api.World.Blocks.Count; i++)
                {
                    try
                    {
                        Block block = api.World.Blocks[i];
                        if (block == null || block.Code == null) continue;

                        if (block.BlockMaterial != EnumBlockMaterial.Leaves || block.CollisionBoxes != null && block.Id != 0 && block.Id != 1 && block.FirstCodePart() != "rock")
                        {
                            List<BlockBehavior> behaviors = api.World.Blocks[i].BlockBehaviors.ToList();
                            AlteredBlockPhysics phys = new AlteredBlockPhysics(api.World.Blocks[i]);
                            if (!api.World.Blocks[i].BlockBehaviors.Contains(phys))
                            {
                                behaviors.Add(phys);
                                api.World.Blocks[i].BlockBehaviors = behaviors.ToArray();
                            }
                            for (int j = 0; j < api.World.Blocks[i].BlockBehaviors.Length; j++)
                            {
                                api.World.Blocks[i].BlockBehaviors[j].OnLoaded(api);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                }
            }
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

            vec.Sub(0.5, 0, 0.5).Add(0, height / 2.0, 0);

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

    public class AlteredBlockPhysics : BlockBehavior
    {
        Utilities util;
        BlockPos[] offset;
        BlockPos[] cardinal;
        ModSystemBlockReinforcement blockReinforcement;

        public AlteredBlockPhysics(Block block) : base(block)
        {
        }


        public override void OnLoaded(ICoreAPI api)
        {
            util = new Utilities(api);
            base.OnLoaded(api);
            blockReinforcement = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            offset = AreaMethods.AreaBelowOffsetList().ToArray();
            cardinal = AreaMethods.SphericalOffsetList(1).ToArray();
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
        {
            TryCollapse(world, pos);

            base.OnBlockPlaced(world, pos, ref handled);
        }

        public PhysicsBlock getFrictionTableElement(ICoreAPI api, EnumBlockMaterial material)
        {
            PhysicsModConfig config = api.ModLoader.GetModSystem<BlockPhysicsMod>().Config;
            if (config.FrictionTable.TryGetValue(material, out double friction))
            {
                return new PhysicsBlock(friction);
            }

            return new PhysicsBlock(0.0);
        }

        public double FindTotalFriction(IWorldAccessor world, BlockPos pos)
        {
            try
            {
                util = new Utilities(world.Api);
                double totalfriction = getFrictionTableElement(world.Api, block.BlockMaterial).getFriction();
                BlockReinforcement r = null;
                if (blockReinforcement != null)
                {
                    r = blockReinforcement.GetReinforcment(pos);
                }
                bool isolated = util.Isolated(pos);
                bool overhang = util.OverHangAtLimit(pos, 8);
                if ((r != null && (r.Strength > 0 || r.Locked)) || util.IsSupported(pos, block))
                {
                    totalfriction = 1.0;
                }
                else if (isolated || overhang)
                {
                    totalfriction = -0.1;
                }
                else
                {
                    //stickyness
                    for (int i = 0; i < util.cardinal.Length; i++)
                    {
                        Block iBlock = world.BlockAccessor.GetBlock(new BlockPos(pos.X + util.cardinal[i].X, pos.Y + util.cardinal[i].Y, pos.Z + util.cardinal[i].Z));
                        totalfriction += getFrictionTableElement(world.Api, iBlock.BlockMaterial).getFriction();
                    }
                    //subtract weight on block
                    for (int y = 0; y < world.BlockAccessor.MapSizeY - pos.Y; y++)
                    {
                        Block yBlock = world.BlockAccessor.GetBlock(new BlockPos(pos.X, pos.Y + y, pos.Z));
                        if (yBlock.IsReplacableBy(block)) break;
                        double friction = getFrictionTableElement(world.Api, yBlock.BlockMaterial).getFriction();
                        totalfriction -= 0.001;
                    }
                }
                return totalfriction;
            }
            catch (NullReferenceException)
            {
            }
            return 0;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            cardinal = cardinal ?? AreaMethods.SphericalOffsetList(1).ToArray(); //if for whatever reason it's null
            offset = offset ?? AreaMethods.AreaBelowOffsetList().ToArray();

            if (world.Side.IsServer())
            {
                for (int i = 0; i < cardinal.Length; i++)
                {
                    if (world.BlockAccessor.GetBlock(pos.AddCopy(cardinal[i])).Id != 0)
                    {
                        world.BlockAccessor.GetBlock(pos.AddCopy(cardinal[i])).GetBehavior<AlteredBlockPhysics>()?.TryCollapse(world, pos.AddCopy(cardinal[i]));
                        world.BlockAccessor.GetBlock(pos.AddCopy(cardinal[i])).OnNeighourBlockChange(world, pos.AddCopy(cardinal[i]), pos);
                    }
                }
                TryCollapse(world, pos);
            }
            base.OnBlockRemoved(world, pos, ref handling);
        }

        public void TryCollapse(IWorldAccessor world, BlockPos pos)
        {
            if (world.Side.IsClient()) return;

            if (FindTotalFriction(world, pos) >= 1.0) return;

            world.RegisterCallbackUnique((vworld, vpos, dt) =>
            {
                BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);
                Block dBlock = world.BlockAccessor.GetBlock(dPos);

                if (dBlock.IsReplacableBy(block))
                {
                    util.MoveBlock(pos, dPos);
                }
                else
                {
                    if (dBlock.CollisionBoxes != null && block.CollisionBoxes != null)
                    {
                        double area = 0;
                        double dArea = 0;
                        for (int i = 0; i < dBlock.CollisionBoxes.Length; i++) dArea += dBlock.CollisionBoxes[i].Area();
                        for (int i = 0; i < block.CollisionBoxes.Length; i++) area += block.CollisionBoxes[i].Area();

                        if (dArea < 0.8)
                        {
                            world.BlockAccessor.BreakBlock(pos, null);
                        }
                        else if (area >= dArea)
                        {
                            BeginMove(world, pos);
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
            try
            {
                List<BlockPos> possiblePos = new List<BlockPos>();
                for (int i = 0; i < offset.Length; i++)
                {
                    BlockPos offs = new BlockPos(pos.X + offset[i].X, pos.Y + offset[i].Y, pos.Z + offset[i].Z);
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
            catch (Exception)
            {
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
        IBulkBlockAccessor bbA;
        IWorldAccessor world;
        ICoreAPI api;
        public BlockPos[] cardinal;
        public BlockPos[] supportarea;

        public Utilities(ICoreAPI api)
        {
            this.api = api;
            world = api.World;
            bA = api.World.BlockAccessor;
            bbA = api.World.BulkBlockAccessor;
            cardinal = AreaMethods.SphericalOffsetList(1).ToArray();
            supportarea = AreaMethods.LargeAreaBelowOffsetList().ToArray();
        }

        public bool IsSupported(BlockPos pos, Block block)
        {
            for (int i = 0; i < supportarea.Length; i++)
            {
                BlockPos iPos = new BlockPos(pos.X + supportarea[i].X, pos.Y + supportarea[i].Y, pos.Z + supportarea[i].Z);
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
                BlockPos iPos = new BlockPos(pos.X + cardinal[i].X, pos.Y + cardinal[i].Y, pos.Z + cardinal[i].Z);
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

            bA.WalkBlocks(pos.AddCopy(-8, -1, -8), pos.AddCopy(8, -1, 8), (vBlock, vPos) =>
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
                if (bA.GetBlock(toPos).Id != 0)
                {
                    api.World.SpawnCubeParticles(toPos, toPos.ToVec3d().Add(0.5), 4, 32);
                }
            }
        }
    }

    public static class MiscUtilities
    {
        public static double Area(this Cuboidf cuboid)
        {
            return (cuboid.Length * cuboid.Width * cuboid.Height);
        }

        public static BlockPos AddCopy(this BlockPos pos, BlockPos copy)
        {
            return pos.AddCopy(copy.X, copy.Y, copy.Z);
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
