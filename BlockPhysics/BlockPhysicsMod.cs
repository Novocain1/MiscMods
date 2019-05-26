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
                if (val.Value.Class == "EntityItem") continue;
                Entity entity = val.Value;

                BlockPos pos = entity.ServerPos.AsBlockPos;

                if (sapi.World.Rand.NextDouble() > 0.9)
                {
                    sapi.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                }

                if (entity is EntityPlayer)
                {
                    EntityPlayer entityPlayer = entity as EntityPlayer;
                    if (entityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Survival) continue;
                }

                if (entity == null) continue;
                if (entity.ServerPos == null || !entity.Alive) continue;

                ITreeAttribute attribs = entity.WatchedAttributes.GetOrAddTreeAttribute("health");
                float? maxair = attribs.TryGetFloat("maxair");
                float? currentair = attribs.TryGetFloat("currentair");

                if (maxair == null)
                {
                    attribs.SetFloat("maxair", 1.0f);
                    entity.WatchedAttributes.MarkPathDirty("health");
                    continue;
                }
                if (currentair == null)
                {
                    attribs.SetFloat("currentair", 1.0f);
                    entity.WatchedAttributes.MarkPathDirty("health");
                    continue;
                }

                if (InBlockBounds(entity.ServerPos.XYZ))
                {
                    if (currentair > 0)
                    {
                        attribs.SetFloat("currentair", (float)currentair - 0.01f);
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

        public bool InBlockBounds(Vec3d vec)
        {
            vec.Sub(0.5, 0, 0.5).Add(0, 1, 0);
            BlockPos pos = new BlockPos((int)Math.Round(vec.X), (int)Math.Round(vec.Y), (int)Math.Round(vec.Z));
            Vec3d blockCenter = pos.ToVec3d().AddCopy(0.5, 0.5, 0.5);
            Block block = sapi.World.BlockAccessor.GetBlock(pos);
            double distance = Math.Sqrt(vec.SquareDistanceTo(blockCenter));

            if (block.IsLiquid() && distance < 1.5) return true;
            if (block.Id == 0 || block.CollisionBoxes == null) return false;

            for (int i = 0; i < block.CollisionBoxes.Length; i++)
            {
                if ((block.CollisionBoxes[i].Length > 0.8 && block.CollisionBoxes[i].Height > 0.8 && block.CollisionBoxes[i].Width > 0.8) && distance < 1.11) return true;
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
                float? barvalue = HAX.GetInstanceField(typeof(GuiElementStatbar), statbar, "value") as float?;

                if (currentair != barvalue)
                {
                    SingleComposer.GetStatbar("airbar").SetValue((float)currentair);
                }
            }

            base.OnRenderGUI(deltaTime);
        }
    }

    public class AlteredBlockPhysics : BlockBehavior
    {
        MiscUtilities misc = new MiscUtilities();
        BlockPos[] offset;
        BlockPos[] cardinal;
        ModSystemBlockReinforcement blockReinforcement;
        double resistance = 0.0;

        public AlteredBlockPhysics(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            blockReinforcement = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            resistance = block is BlockSoil ? 0.5 : block.FirstCodePart() == "gravel" ? 0.25 : 0.0;

            offset = AreaMethods.AreaBelowOffsetList().ToArray();
            cardinal = AreaMethods.CardinalOffsetList().ToArray();
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
            world.BlockAccessor.GetBlock(pos.UpCopy()).OnNeighourBlockChange(world, pos.UpCopy(), pos);

            base.OnBlockRemoved(world, pos, ref handling);
        }

        public void TryCollapse(IWorldAccessor world, BlockPos pos)
        {
            if (world.Side.IsClient()) return;
            BlockReinforcement r = blockReinforcement.GetReinforcment(pos);
            double currentresistance = r != null && (r.Strength > 0 || r.Locked) ? 1.0 : Isolated(world, pos) ? -0.1 : resistance;
            if (world.Rand.NextDouble() < currentresistance || misc.IsSupported(world, pos, block)) return;

            world.RegisterCallbackUnique((vworld, vpos, dt) =>
            {
                BlockPos dPos = pos.AddCopy(0, -1, 0);
                Block dBlock = world.BlockAccessor.GetBlock(dPos);

                if (dBlock.IsReplacableBy(block))
                {
                    MoveBlock(world, pos, pos.AddCopy(0, -1, 0));
                }
                else if (dBlock.CollisionBoxes == null || block.CollisionBoxes == null)
                {
                    BeginMove(world, pos);
                }
                else if (dBlock.CollisionBoxes[0].Height < 1)
                {
                    world.BlockAccessor.BreakBlock(pos, null);
                }
                else if (block.CollisionBoxes[0].Length >= dBlock.CollisionBoxes[0].Length)
                {
                    BeginMove(world, pos);
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

                MoveBlock(world, pos, toPos);
            }
        }

        public void MoveBlock(IWorldAccessor world, BlockPos fromPos, BlockPos toPos)
        {
            if (block.EntityClass != null)
            {
                TreeAttribute attribs = new TreeAttribute();
                BlockEntity be = world.BlockAccessor.GetBlockEntity(fromPos);
                if (be != null)
                {
                    be.ToTreeAttributes(attribs);
                    attribs.SetInt("posx", toPos.X);
                    attribs.SetInt("posy", toPos.Y);
                    attribs.SetInt("posz", toPos.Z);

                    world.BulkBlockAccessor.SetBlock(0, fromPos);
                    world.BulkBlockAccessor.SetBlock(block.BlockId, toPos);
                    world.BulkBlockAccessor.Commit();
                    BlockEntity be2 = world.BlockAccessor.GetBlockEntity(toPos);

                    if (be2 != null) be2.FromTreeAtributes(attribs, world);
                }
            }
            else
            {
                world.BulkBlockAccessor.SetBlock(0, fromPos);
                world.BulkBlockAccessor.SetBlock(block.BlockId, toPos);
                world.BulkBlockAccessor.Commit();
            }
        }

        public bool Isolated(IWorldAccessor world, BlockPos pos)
        {
            for (int i = 0; i < cardinal.Length; i++)
            {
                BlockPos iPos = pos.AddCopy(cardinal[i].X, cardinal[i].Y, cardinal[i].Z);
                Block iBlock = world.BlockAccessor.GetBlock(iPos);
                if (!iBlock.IsReplacableBy(block))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class BehaviorSupportBeam : BlockBehavior
    {
        public BehaviorSupportBeam(Block block) : base(block)
        {
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            if (world.BlockAccessor.GetBlock(pos).HasBehavior<BehaviorSupportBeam>()) return;
            world.BlockAccessor.WalkBlocks(pos.AddCopy(-8, -8, -8), pos.AddCopy(8, 8, 8), (vBlock, vPos) =>
            {
                if (vBlock.HasBehavior<BehaviorSupportBeam>() && vPos != pos) world.BlockAccessor.BreakBlock(vPos, null);
                else if (vBlock.HasBehavior<AlteredBlockPhysics>()) world.BlockAccessor.TriggerNeighbourBlockUpdate(vPos);
            });
            base.OnBlockRemoved(world, pos, ref handling);
        }
    }

    public class BreakIfFloatingAndCollapse : BlockBehaviorBreakIfFloating
    {
        MiscUtilities misc = new MiscUtilities();
        Block placedblock;

        public BreakIfFloatingAndCollapse(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            if (IsRock()) placedblock = api.World.GetBlock(new AssetLocation("gravel-" + block.Variant["rock"]));

            base.OnLoaded(api);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
        {
            if (world.Side.IsServer())
            {
                if (IsRock() && !world.BlockAccessor.GetBlock(pos.UpCopy()).IsReplacableBy(block) && world.BlockAccessor.GetBlock(neibpos).LiquidCode != "water")
                {
                    if ((!misc.IsSupported(world, pos, block) && world.Rand.NextDouble() > 0.9))
                    {
                        world.BlockAccessor.SetBlock(placedblock.BlockId, pos);
                        int ns = world.Rand.Next(2, 8);
                        world.BlockAccessor.WalkBlocks(pos.AddCopy(-ns, -ns, -ns), pos.AddCopy(ns, ns, ns), (b, bp) =>
                        {
                            world.BlockAccessor.TriggerNeighbourBlockUpdate(bp);
                        });
                    }
                }
            }
            base.OnNeighbourBlockChange(world, pos, neibpos, ref handled);
        }

        public bool IsRock() => block.FirstCodePart() == "rock";
    }

    public class MiscUtilities
    {
        BlockPos[] cardinal = AreaMethods.CardinalOffsetList().ToArray();
        BlockPos[] supportarea = AreaMethods.LargeAreaBelowOffsetList().ToArray();

        public bool IsSupported(IWorldAccessor world, BlockPos pos, Block block)
        {
            for (int i = 0; i < supportarea.Length; i++)
            {
                BlockPos iPos = pos.AddCopy(supportarea[i].X, supportarea[i].Y, supportarea[i].Z);
                Block iBlock = world.BlockAccessor.GetBlock(iPos);
                if (iBlock.HasBehavior<BehaviorSupportBeam>())
                {
                    BlockPos dPos = pos.DownCopy();
                    Block dBlock = world.BlockAccessor.GetBlock(dPos);
                    if (!dBlock.IsReplacableBy(block))
                    {
                        return true;
                    }
                    else
                    {
                        for (int j = 0; j < cardinal.Length; j++)
                        {
                            BlockPos jPos = pos.AddCopy(cardinal[j].X, cardinal[j].Y, cardinal[j].Z);
                            Block jBlock = world.BlockAccessor.GetBlock(jPos);
                            if (!jBlock.IsReplacableBy(block))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
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
