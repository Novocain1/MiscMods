using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CivMods
{
    public static class ArrayStuff
    {
        public static T NextR<T>(this T[] array, ref uint index)
        {
            index = (uint)(++index % array.Length);
            return array[index];
        }

        public static T NextR<T>(this List<T> array, ref uint index) => array.ToArray().NextR(ref index);
        public static T NextR<T>(this HashSet<T> array, ref uint index) => array.ToArray().NextR(ref index);

        public static T Prev<T>(this T[] array, ref uint index)
        {
            index = index > 0 ? index - 1 : (uint)(array.Length-1);
            return array[index];
        }

        public static Block GetBlock(this BlockPos pos, IWorldAccessor world) { return world.BlockAccessor.GetBlock(pos); }
        public static Block GetBlock(this BlockPos pos, ICoreAPI api) { return pos.GetBlock(api.World); }

        public static Block GetBlock(this AssetLocation asset, ICoreAPI api)
        {
            if (api.World.BlockAccessor.GetBlock(asset) != null)
            {
                return api.World.BlockAccessor.GetBlock(asset);
            }
            return null;
        }

        public static Item GetItem(this AssetLocation asset, ICoreAPI api)
        {
            if (api.World.GetItem(asset) != null)
            {
                return api.World.GetItem(asset);
            }
            return null;
        }

        public static AssetLocation ToAsset(this string asset) { return new AssetLocation(asset); }
        public static Block ToBlock(this string block, ICoreAPI api) => block.WithDomain().ToAsset().GetBlock(api);
        public static Block Block(this BlockSelection sel, ICoreAPI api) => api.World.BlockAccessor.GetBlock(sel.Position);
        public static BlockEntity BlockEntity(this BlockSelection sel, ICoreAPI api) => api.World.BlockAccessor.GetBlockEntity(sel.Position);

        public static AssetLocation[] ToAssets(this string[] strings)
        {
            List<AssetLocation> assets = new List<AssetLocation>();
            foreach (var val in strings)
            {
                assets.Add(val.ToAsset());
            }
            return assets.ToArray();
        }
        public static AssetLocation[] ToAssets(this List<string> strings) => strings.ToArray().ToAssets();

        public static void PlaySoundAtWithDelay(this IWorldAccessor world, AssetLocation location, BlockPos pos, int delay)
        {
            world.RegisterCallback(dt => world.PlaySoundAt(location, pos.X, pos.Y, pos.Z), delay);
        }

        public static T[] Stretch<T>(this T[] array, int amount)
        {
            if (amount < 1) return array;
            T[] parray = array;

            Array.Resize(ref array, array.Length + amount);
            for (int i = 0; i < array.Length; i++)
            {
                double scalar = (double)i / array.Length;
                array[i] = parray[(int)(scalar * parray.Length)];
            }
            return array;
        }

        public static int IndexOfMin(this IList<int> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            int min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public static int IndexOfMin(this int[] self) => IndexOfMin(self.ToList());

        public static bool IsSurvival(this EnumGameMode gamemode) => gamemode == EnumGameMode.Survival;
        public static bool IsCreative(this EnumGameMode gamemode) => gamemode == EnumGameMode.Creative;
        public static bool IsSpectator(this EnumGameMode gamemode) => gamemode == EnumGameMode.Spectator;
        public static bool IsGuest(this EnumGameMode gamemode) => gamemode == EnumGameMode.Guest;
        public static void PlaySoundAt(this IWorldAccessor world, AssetLocation loc, BlockPos pos) => world.PlaySoundAt(loc, pos.X, pos.Y, pos.Z);
        public static int GetID(this AssetLocation loc, ICoreAPI api) => loc.GetBlock(api).BlockId;

        public static string WithDomain(this string a) => a.IndexOf(":") == -1 ? "game:" + a : a;

        public static string[] WithDomain(this string[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = a[i].WithDomain();
            }
            return a;
        }

        public static Vec3d MidPoint(this BlockPos pos) => pos.ToVec3d().AddCopy(0.5, 0.5, 0.5);

        public static string Apd(this string a, string appended)
        {
            return a + "-" + appended;
        }

        public static string Apd(this string a, int appended)
        {
            return a + "-" + appended;
        }

        public static void SpawnItemEntity(this IWorldAccessor world, ItemStack[] stacks, Vec3d pos, Vec3d velocity = null)
        {
            foreach (ItemStack stack in stacks)
            {
                world.SpawnItemEntity(stack, pos, velocity);
            }
        }

        public static void SpawnItemEntity(this IWorldAccessor world, JsonItemStack[] stacks, Vec3d pos, Vec3d velocity = null)
        {
            foreach (JsonItemStack stack in stacks)
            {
                string err = "";
                stack.Resolve(world, err);
                if (stack.ResolvedItemstack != null) world.SpawnItemEntity(stack.ResolvedItemstack, pos, velocity);
            }
        }

        public static BlockEntity BlockEntity(this BlockPos pos, IWorldAccessor world)
        {
            return world.BlockAccessor.GetBlockEntity(pos);
        }

        public static BlockEntity BlockEntity(this BlockPos pos, ICoreAPI api) => pos.BlockEntity(api.World);

        public static BlockEntity BlockEntity(this BlockSelection sel, IWorldAccessor world)
        {
            return sel.Position.BlockEntity(world);
        }

        public static void InitializeAnimators(this BlockEntityAnimationUtil util, Vec3f rot, params string[] CacheDictKeys )
        {
            foreach (var val in CacheDictKeys)
            {
                util.InitializeAnimator(val, rot);
            }
        }

        public static void InitializeAnimators(this BlockEntityAnimationUtil util, Vec3f rot, List<string> CacheDictKeys)
        {
            InitializeAnimators(util, rot, CacheDictKeys.ToArray());
        }

        public static void SetUv(this MeshData mesh, TextureAtlasPosition texPos) => mesh.SetUv(new float[] {texPos.x1, texPos.y1, texPos.x2, texPos.y1, texPos.x2, texPos.y2, texPos.x1, texPos.y2 });

        public static bool TryGiveItemstack(this IPlayerInventoryManager manager, ItemStack[] stacks)
        {
            foreach (var val in stacks)
            {
                if (manager.TryGiveItemstack(val)) continue;
                return false;
            }
            return true;
        }

        public static bool TryGiveItemstack(this IPlayerInventoryManager manager, JsonItemStack[] stacks)
        {
            return manager.TryGiveItemstack(stacks.ResolvedStacks(manager.ActiveHotbarSlot.Inventory.Api.World));
        }

        public static double DistanceTo(this SyncedEntityPos pos, Vec3d vec)
        {
            return Math.Sqrt(pos.SquareDistanceTo(vec));
        }

        public static bool WildCardMatch(this RegistryObject obj, string a) => obj.WildCardMatch(new AssetLocation(a));

        public static ItemStack[] ResolvedStacks(this JsonItemStack[] stacks, IWorldAccessor world)
        {
            List<ItemStack> stacks1 = new List<ItemStack>();
            foreach (JsonItemStack stack in stacks)
            {
                stack.Resolve(world, null);
                stacks1.Add(stack.ResolvedItemstack);
            }
            return stacks1.ToArray();
        }

        public static object GetInstanceField<T>(this T instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = instance.GetType().GetField(fieldName, bindFlags);
            return field?.GetValue(instance);
        }

        public static List<BlockEntity> GetBlockEntities(this IWorldAccessor world, BlockPos pos, Vec3i range)
        {
            List<BlockEntity> blockEntities = new List<BlockEntity>();
            for (int x = pos.X - range.X; x < pos.X + range.X; x++)
            {
                for (int y = pos.Y - range.Y; y < pos.Y + range.Y; y++)
                {
                    for (int z = pos.Z - range.Z; z < pos.Z + range.Z; z++)
                    {
                        BlockPos p = new BlockPos(x, y, z);
                        BlockEntity be = p.BlockEntity(world);
                        if (be != null)
                            blockEntities.Add(be);
                    }
                }
            }
            return blockEntities;
        }

        public static List<BlockEntity> GetBlockEntitiesAround(this IWorldAccessor world, BlockPos pos, Vec2i range) => GetBlockEntities(world, pos, new Vec3i(range.X, range.Y, range.X));

        public static BlockPos RelativeToSpawn(this BlockPos pos, IWorldAccessor world)
        {
            return pos.SubCopy(world.DefaultSpawnPosition.AsBlockPos);
        }

        public static SimpleParticleProperties TemporalEffect {
            get
            {
                var p = new SimpleParticleProperties(
                    0.5f, 1,
                    ColorUtil.ToRgba(150, 34, 47, 44),
                    new Vec3d(),
                    new Vec3d(),
                    new Vec3f(-0.1f, -0.1f, -0.1f),
                    new Vec3f(0.1f, 0.1f, 0.1f),
                    1.5f,
                    0,
                    0.5f,
                    0.75f,
                    EnumParticleModel.Quad
                );

                p.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
                p.addPos.Set(1, 2, 1);
                p.addLifeLength = 0.5f;
                p.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 80);
                return p;
            }
        }

        static readonly NatFloat rndPos = NatFloat.create(EnumDistribution.INVERSEGAUSSIAN, 0, 0.5f);

        public static SimpleParticleProperties TemporalEffectAtPos(this BlockPos pos, ICoreAPI api)
        {
            SimpleParticleProperties p = TemporalEffect;
            Vec3d posvec = pos.DownCopy().MidPoint();
            int r = 53;
            int g = 221;
            int b = 172;
            p.color = (r << 16) | (g << 8) | (b << 0) | (50 << 24);

            p.addPos.Set(0, 0, 0);
            p.BlueEvolve = null;
            p.RedEvolve = null;
            p.GreenEvolve = null;
            p.minSize = 0.1f;
            p.maxSize = 0.2f;
            p.SizeEvolve = null;
            p.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 100f);

            double xpos = rndPos.nextFloat();
            double ypos = 1.9 + api.World.Rand.NextDouble() * 0.2;
            double zpos = rndPos.nextFloat();

            p.lifeLength = GameMath.Sqrt(xpos * xpos + zpos * zpos) / 10;
            p.minPos.Set(posvec.X + xpos, posvec.Y + ypos, posvec.Z + zpos);
            p.minVelocity.Set(-(float)xpos, -1 - (float)api.World.Rand.NextDouble() / 2, -(float)zpos);
            p.minQuantity = 0.25f;
            p.addQuantity = 0;

            return p;
        }
    }
}
