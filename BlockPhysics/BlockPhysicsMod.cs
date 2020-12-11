using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
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

        public double GetFriction()
        {
            return friction;
        }

        public bool GetHasPhysics()
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

        public PhysicsModConfig Config { get; private set; } = new PhysicsModConfig();

        public const string patchCode = "Novocain.ModSystem.BlockPhysicsMod";
        public Harmony harmonyInstance = new Harmony(patchCode);

        public void LoadConfig()
        {
            if (sapi.LoadModConfig<PhysicsModConfig>("blockphysicsmod.json") == null) { SaveConfig(); return; }

            Config = sapi.LoadModConfig<PhysicsModConfig>("blockphysicsmod.json");
            SaveConfig();
        }

        public void SaveConfig() => sapi.StoreModConfig(Config, "blockphysicsmod.json");

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockBehaviorClass("SupportBeam", typeof(BehaviorSupportBeam));
            api.RegisterBlockBehaviorClass("BreakIfFloating", typeof(BreakIfFloatingAndCollapse));
            
            harmonyInstance.PatchAll();
        }

        public override void Dispose()
        {
            harmonyInstance.UnpatchAll(patchCode);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            LoadConfig();
        }
    }

    public class BlockPhysics
    {
        Utilities util;
        BlockPos[] offset;
        ModSystemBlockReinforcement blockReinforcement;
        IWorldAccessor world;

        public BlockPhysics(ICoreAPI api)
        {
            util = new Utilities(api);
            blockReinforcement = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            offset = AreaMethods.AreaBelowOffsetList().ToArray();
            world = api.World;
        }

        public PhysicsBlock GetFrictionTableElement(ICoreAPI api, EnumBlockMaterial material)
        {
            PhysicsModConfig config = api.ModLoader.GetModSystem<BlockPhysicsMod>().Config;
            if (config.FrictionTable.TryGetValue(material, out double friction))
            {
                return new PhysicsBlock(friction);
            }

            return new PhysicsBlock(0.0);
        }

        public double FindTotalFriction(IWorldAccessor world, BlockPos pos, Block block)
        {
            try
            {
                util = new Utilities(world.Api);
                double totalfriction = GetFrictionTableElement(world.Api, block.BlockMaterial).GetFriction();
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
                        totalfriction += GetFrictionTableElement(world.Api, iBlock.BlockMaterial).GetFriction();
                    }
                    //subtract weight on block
                    for (int y = 0; y < world.BlockAccessor.MapSizeY - pos.Y; y++)
                    {
                        Block yBlock = world.BlockAccessor.GetBlock(new BlockPos(pos.X, pos.Y + y, pos.Z));
                        if (yBlock.IsReplacableBy(block)) break;
                        double friction = GetFrictionTableElement(world.Api, yBlock.BlockMaterial).GetFriction();
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

        public bool CanCollapse(BlockPos pos, Block block)
        {
            if (FindTotalFriction(world, pos, block) >= 1.0) return false;

            BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);
            Block dBlock = world.BlockAccessor.GetBlock(dPos);

            if (dBlock.IsReplacableBy(block))
            {
                return true;
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
                        return CanMove(world, pos, block);
                    }
                }
                else
                {
                    return CanMove(world, pos, block);
                }
            }

            return false;
        }

        public bool CanMove(IWorldAccessor world, BlockPos pos, Block block)
        {
            bool canMove = false;
            for (int i = 0; i < offset.Length; i++)
            {
                BlockPos offs = new BlockPos(pos.X + offset[i].X, pos.Y + offset[i].Y, pos.Z + offset[i].Z);
                Block oBlock = world.BulkBlockAccessor.GetBlock(offs);

                if (offs.Y < 0 || offs.Y > world.BlockAccessor.MapSizeY) continue;

                if (oBlock.IsReplacableBy(block))
                {
                    canMove = true;
                    break;
                }
            }

            return canMove;
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
        public BlockPos[] cardinal;
        public BlockPos[] supportarea;

        public Utilities(ICoreAPI api)
        {
            world = api.World;
            bA = api.World.BlockAccessor;
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

                    if (!uBlock.IsReplacableBy(block) && uBlock.HasBehavior<BlockBehaviorUnstableFalling>())
                    {
                        j++;
                        if (vBlock.IsReplacableBy(block)) i++;
                    }

                });

            return i > limit || i >= j;
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

    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname) => (T)AccessTools.Field(instance.GetType(), fieldname).GetValue(instance);
        public static T GetProperty<T>(this object instance, string fieldname) => (T)AccessTools.Property(instance.GetType(), fieldname).GetValue(instance);
        public static T CallMethod<T>(this object instance, string method, params object[] args) => (T)AccessTools.Method(instance.GetType(), method).Invoke(instance, args);
        public static void CallMethod(this object instance, string method, params object[] args) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, args);
        public static void CallMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, null);
        public static object CreateInstance(this Type type) => AccessTools.CreateInstance(type);
        public static T[] GetFields<T>(this object instance)
        {
            List<T> fields = new List<T>();
            var declaredFields = AccessTools.GetDeclaredFields(instance.GetType())?.Where((t) => t.FieldType == typeof(T));
            foreach (var val in declaredFields)
            {
                fields.Add(instance.GetField<T>(val.Name));
            }
            return fields.ToArray();
        }

        public static void SetField(this object instance, string fieldname, object setVal) => AccessTools.Field(instance.GetType(), fieldname).SetValue(instance, setVal);
        public static MethodInfo GetMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method);
    }

    static class ThreadStuff
    {
        static Type threadType;

        static ThreadStuff()
        {
            var ts = AccessTools.GetTypesFromAssembly(Assembly.GetAssembly(typeof(ClientMain)));
            threadType = ts.Where((t, b) => t.Name == "ClientThread").Single();
        }

        public static void InjectClientThread(this ICoreClientAPI capi, string name, int ms, params ClientSystem[] systems) => capi.World.InjectClientThread(name, ms, systems);

        public static void InjectClientThread(this IClientWorldAccessor world, string name, int ms, params ClientSystem[] systems)
        {
            object instance;
            Thread thread;

            instance = threadType.CreateInstance();
            instance.SetField("game", world as ClientMain);
            instance.SetField("threadName", name);
            instance.SetField("clientsystems", systems);
            instance.SetField("lastFramePassedTime", new Stopwatch());
            instance.SetField("totalPassedTime", new Stopwatch());
            instance.SetField("paused", false);
            instance.SetField("sleepMs", ms);

            List<Thread> clientThreads = (world as ClientMain).GetField<List<Thread>>("clientThreads");
            Stack<ClientSystem> vanillaSystems = new Stack<ClientSystem>((world as ClientMain).GetField<ClientSystem[]>("clientSystems"));
            foreach (var system in systems)
            {
                vanillaSystems.Push(system);
            }

            (world as ClientMain).SetField("clientSystems", vanillaSystems.ToArray());

            thread = new Thread(() => instance.CallMethod("Process"));
            thread.IsBackground = true;
            thread.Start();
            thread.Name = name;
            clientThreads.Add(thread);
        }
    }

    [HarmonyPatch(typeof(BlockBehaviorUnstableFalling), "TryFalling")]
    class UnstableFallingPatch
    {
        public static bool Prefix()
        {
            //skip original method
            return true;
        }

        public static void Postfix(BlockBehaviorUnstableFalling __instance, IWorldAccessor world, BlockPos pos, ref EnumHandling handling, ref string failureCode)
        {
            if (world.Side == EnumAppSide.Client) return;

            Block block = __instance.block;

            BlockPhysics physics = new BlockPhysics(world.Api);

            if (physics.CanCollapse(pos, block))
            {
                AssetLocation fallSound = __instance.GetField<AssetLocation>("fallSound");
                float impactDamageMul = __instance.GetField<float>("impactDamageMul");
                float dustIntensity = __instance.GetField<float>("dustIntensity");

                // Prevents duplication
                Entity entity = world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1, 1.5f, (e) =>
                {
                    return e is EntityBlockFalling && ((EntityBlockFalling)e).initialPos.Equals(pos);
                });

                if (entity == null)
                {
                    EntityBlockFalling entityblock = new EntityBlockFalling(block, world.BlockAccessor.GetBlockEntity(pos), pos, fallSound, impactDamageMul, true, dustIntensity);
                    world.SpawnEntity(entityblock);
                }
                else
                {
                    handling = EnumHandling.PreventDefault;
                    failureCode = "entityintersecting";
                    return;
                }

                handling = EnumHandling.PreventSubsequent;
                return;
            }

            handling = EnumHandling.PassThrough;
        }
    }
}
