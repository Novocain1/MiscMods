using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace WorldGenTests
{
    public class NukeTest : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.RegisterCommand("nuke", "", "", (a, b, c) =>
            {
                var bA = api.World.BulkBlockAccessor;
                int sploded = 1;

                int radius = 32;
                long id = 0;
                int splode = 0;

                BlockPos startPos = a.Entity.ServerPos.AsBlockPos;
                id = api.Event.RegisterGameTickListener((dt) =>
                {
                    if (splode < radius)
                    {
                        int rad = splode;

                        int diameter = rad * 2;

                        int probecount = (diameter * diameter * diameter) - ((diameter - 1) * (diameter - 1) * (diameter - 1));

                        int[] probes = new int[probecount];
                        int i = 0;

                        for (int x = -rad; x < rad; x++)
                        {
                            for (int y = -rad; y < rad; y++)
                            {
                                for (int z = -rad; z < rad; z++)
                                {
                                    if (i * 3 + 2 > probecount) break;

                                    if (api.World.Rand.NextDouble() > 0.9999 && InsideRadius(rad, x, y, z) && !InsideRadius(rad - 1, x, y, z))
                                    {
                                        //bA.SetBlock(710, startPos.AddCopy(x, y, z));
                                        probes[i * 3 + 0] = startPos.X + x;
                                        probes[i * 3 + 1] = startPos.Y + y;
                                        probes[i * 3 + 2] = startPos.Z + z;
                                        i++;
                                    }
                                }
                            }
                        }

                        Vec3d fromPos = new Vec3d(startPos.X, startPos.Y, startPos.Z);

                        Vec3d toPos = new Vec3d();

                        List<BlockSelection> blockIntercepts = new List<BlockSelection>();
                        List<EntitySelection> entityIntercepts = new List<EntitySelection>();

                        for (int j = 0; j < i / 3; j++)
                        {
                            toPos.X = probes[j * 3 + 0];
                            toPos.Y = probes[j * 3 + 1];
                            toPos.Z = probes[j * 3 + 2];

                            var dist = GameMath.Sqrt(fromPos.SquareDistanceTo(toPos.X, toPos.Y, toPos.Z));
                            if (dist > rad) continue;

                            var blockIntercept = new BlockSelection();
                            var entityIntercept = new EntitySelection();

                            var dir1 = fromPos.SubCopy(toPos);

                            var ray1 = new Ray()
                            {
                                origin = fromPos,
                                dir = dir1
                            };

                            api.World.RayTraceForSelection(ray1, ref blockIntercept, ref entityIntercept);

                            if (blockIntercept != null && !blockIntercepts.Any(p => p.Position.Equals(blockIntercept.Position))) blockIntercepts.Add(blockIntercept);
                            if (entityIntercept != null && !entityIntercepts.Any(p => p.Position.Equals(entityIntercept.Position))) entityIntercepts.Add(entityIntercept);
                        }

                        var ember = bA.GetBlock(new AssetLocation("game:ember"));

                        foreach (var bs in blockIntercepts)
                        {
                            var block = api.World.BlockAccessor.GetBlock(bs.Position);

                            if (block.Id != 0)
                            {
                                bA.SetBlock(0, bs.Position);
                                bA.TriggerNeighbourBlockUpdate(bs.Position);
                                if (api.World.Rand.NextDouble() > 0.99) block.OnBlockExploded(api.World, bs.Position, startPos, EnumBlastType.RockBlast);
                                sploded++;
                            }
                        }

                        bA.Commit();

                        foreach (var entitySel in entityIntercepts)
                        {
                            entitySel.Entity?.ReceiveDamage(new DamageSource(), 50.0f / sploded);
                            sploded++;
                        }
                        splode++;
                    }
                    else
                    {
                        api.Event.UnregisterGameTickListener(id);
                    }
                },
                1);
            });
        }

        public bool InsideRadius(int rad, int x, int y, int z)
        {
            return (x * x + y * y + z * z) <= (rad * rad);
        }
    }

    public class TestMod : ModSystem
    {
        private Harmony harmony;
        private ICoreServerAPI api;

        private const string patchCode = "Novocain.ModSystem.TestMod";

        public override double ExecuteOrder()
        {
            return 0.25;
        }

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony(patchCode);
            harmony.PatchAll();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.InitWorldGenerator(Init, "standard");
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.TerrainFeatures, "standard");
        }

        private Dictionary<string, DepositVariant> DepositByCode = new Dictionary<string, DepositVariant>();

        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            var veinMaps = chunks[0].MapChunk.MapRegion.GetModdata<Dictionary<string, IntDataMap2D>>("veinmaps");
            var oreMaps = chunks[0].MapChunk.MapRegion.OreMaps;

            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

            int chunksize = api.World.BlockAccessor.ChunkSize;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            foreach (var vein in veinMaps)
            {
                if (!DepositByCode.ContainsKey(vein.Key)) continue;

                var deposit = DepositByCode[vein.Key];

                Dictionary<int, ResolvedDepositBlock> placeBlockByInBlockId = deposit.GeneratorInst.GetField<Dictionary<int, ResolvedDepositBlock>>("placeBlockByInBlockId");
                Dictionary<int, ResolvedDepositBlock> surfaceBlockByInBlockId = deposit.GeneratorInst.GetField<Dictionary<int, ResolvedDepositBlock>>("surfaceBlockByInBlockId");

                var oreMap = oreMaps[vein.Key];

                var veinMap = vein.Value;

                float veinStep = (float)veinMap.InnerSize / regionChunkSize;
                float oreStep = (float)oreMap.InnerSize / regionChunkSize;

                for (int x = 0; x < chunksize; x++)
                {
                    for (int z = 0; z < chunksize; z++)
                    {
                        int heightY = heightMap[z * chunksize + x];

                        int veinAtPos = veinMap.GetInt((int)(rdx * veinStep + x), (int)(rdz * veinStep + z));

                        float veinARel = (((uint)veinAtPos & ~0x00FFFFFF) >> 24) / 255f;
                        float veinRRel = (((uint)veinAtPos & ~0xFF00FFFF) >> 16) / 255f;
                        float veinGRel = (((uint)veinAtPos & ~0xFFFF00FF) >> 08) / 255f;
                        float veinBRel = (((uint)veinAtPos & ~0xFFFFFF00) >> 00) / 255f;

                        int oreUpLeft = oreMap.GetUnpaddedInt((int)(rdx * oreStep), (int)(rdz * oreStep));
                        int oreUpRight = oreMap.GetUnpaddedInt((int)(rdx * oreStep + oreStep), (int)(rdz * oreStep));
                        int oreBotLeft = oreMap.GetUnpaddedInt((int)(rdx * oreStep), (int)(rdz * oreStep + oreStep));
                        int oreBotRight = oreMap.GetUnpaddedInt((int)(rdx * oreStep + oreStep), (int)(rdz * oreStep + oreStep));
                        uint oreMapInt = (uint)GameMath.BiLerp(oreUpLeft, oreUpRight, oreBotLeft, oreBotRight, (float)x / chunksize, (float)z / chunksize);

                        float oreMapRel = ((oreMapInt & ~0xFF00FF) >> 8) / 15f;

                        if (veinRRel > 0.85f)
                        {
                            int y = (int)(veinGRel * heightY);

                            int depth = (int)(veinARel * 4) + 1;

                            for (int dy = -depth; dy < depth; dy++)
                            {
                                if (y + dy > 0 && y + dy < api.WorldManager.MapSizeY)
                                {
                                    int chunkY = (y + dy) / chunksize;
                                    int lY = (y + dy) % chunksize;

                                    int index3d = (chunksize * lY + z) * chunksize + x;
                                    int blockId = chunks[chunkY].Blocks[index3d];
                                    if (placeBlockByInBlockId?.ContainsKey(blockId) ?? false)
                                    {
                                        var blocks = placeBlockByInBlockId[blockId].Blocks;

                                        chunks[chunkY].Blocks[index3d] = blocks[(int)(oreMapRel * (blocks.Length - 1))].Id;

                                        if (deposit.ChildDeposits != null)
                                        {
                                            foreach (var child in deposit.ChildDeposits)
                                            {
                                                if (veinBRel > 0.5)
                                                {
                                                    Dictionary<int, ResolvedDepositBlock> childPlaceBlockByInBlockId = child.GeneratorInst.GetField<Dictionary<int, ResolvedDepositBlock>>("placeBlockByInBlockId");
                                                    Dictionary<int, ResolvedDepositBlock> childSurfaceBlockByInBlockId = child.GeneratorInst.GetField<Dictionary<int, ResolvedDepositBlock>>("surfaceBlockByInBlockId");

                                                    if (childPlaceBlockByInBlockId.ContainsKey(blockId))
                                                    {
                                                        blocks = childPlaceBlockByInBlockId[blockId].Blocks;
                                                        chunks[chunkY].Blocks[index3d] = blocks[(int)(oreMapRel * (blocks.Length - 1))].Id;
                                                    }
                                                }
                                            }
                                        }

                                        if (dy == 0)
                                        {
                                            //gen surface deposits

                                            if (surfaceBlockByInBlockId?.ContainsKey(blockId) ?? false)
                                            {
                                                if (heightY < api.WorldManager.MapSizeY && veinBRel > 0.5f)
                                                {
                                                    Block belowBlock = api.World.Blocks[chunks[heightY / chunksize].Blocks[((heightY % chunksize) * chunksize + z) * chunksize + x]];

                                                    index3d = (((heightY + 1) % chunksize) * chunksize + z) * chunksize + x;
                                                    if (belowBlock.SideSolid[BlockFacing.UP.Index] && chunks[(heightY + 1) / chunksize].Blocks[index3d] == 0)
                                                    {
                                                        chunks[(heightY + 1) / chunksize].Blocks[index3d] = surfaceBlockByInBlockId[blockId].Blocks[0].BlockId;
#if DEBUG
                                                        //so I can see it better when debugging
                                                        index3d = (heightY % chunksize * chunksize + z) * chunksize + x;
                                                        chunks[heightY / chunksize].Blocks[index3d] = 1;
#endif
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DebugVeinMaps(ICoreServerAPI api, IMapRegion mapRegion, int regionX, int regionZ)
        {
            TyronThreadPool.QueueTask(() =>
            {
                var path = Directory.CreateDirectory(Path.Combine(GamePaths.DataPath, "VeinMaps", api.World.Seed.ToString()));
                var veinMaps = mapRegion.GetModdata<Dictionary<string, IntDataMap2D>>("veinmaps");

                foreach (var vein in veinMaps)
                {
                    IntDataMap2D veinMap = vein.Value;

                    Bitmap bmp = new Bitmap(veinMap.Size, veinMap.Size);
                    for (int x = 0; x < veinMap.Size; x++)
                    {
                        for (int y = 0; y < veinMap.Size; y++)
                        {
                            bmp.SetPixel(x, y, Color.FromArgb(veinMap.GetInt(x, y)));
                        }
                    }
                    Directory.CreateDirectory(Path.Combine(path.FullName, vein.Key.UcFirst()));

                    string pt = Path.Combine(path.FullName, vein.Key.UcFirst(), string.Format("{0}, {1}.png", regionX, regionZ));
                    
                    bmp.Save(pt, ImageFormat.Png);
#if DEBUG
                    break;
#endif
                }
            });
        }

        private void Init()
        {
            var Deposits = api.ModLoader.GetModSystem<GenDeposits>().Deposits;
            foreach (var deposit in Deposits)
            {
                DepositByCode[deposit.Code] = deposit;
            }
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            int i = 0;

            Dictionary<string, IntDataMap2D> maps = new Dictionary<string, IntDataMap2D>();
            foreach (var val in mapRegion.OreMaps)
            {
                var OreVeinLayer = new MapLayerOreVeins(api.World.Seed + i, 8, 0.0f, 255, 64, 512, 256, 64, 32.0);
                int regionSize = api.WorldManager.RegionSize;

                IntDataMap2D data = new IntDataMap2D()
                {
                    Data = OreVeinLayer.GenLayer(
                        regionX * regionSize,
                        regionZ * regionSize,
                        regionSize,
                        regionSize
                    ),
                    Size = regionSize,
                    BottomRightPadding = 0,
                    TopLeftPadding = 0
                };

                maps[val.Key] = data;
                i++;
            }

            mapRegion.SetModdata("veinmaps", maps);

#if DEBUG
            DebugVeinMaps(api, mapRegion, regionX, regionZ);
#endif
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(patchCode);
        }
    }

    public static class RegionExtension
    {
        public static T GetModdata<T>(this IMapRegion mapRegion, string key)
        {
            return SerializerUtil.Deserialize<T>(mapRegion.GetModdata(key));
        }

        public static void SetModdata<T>(this IMapRegion mapRegion, string key, T data)
        {
            mapRegion.SetModdata(key, SerializerUtil.Serialize(data));
        }
    }

    public class MapLayerOreVeins : MapLayerBase
    {
        private Type mlBlurT = Assembly.GetAssembly(typeof(MapLayerBase)).GetTypes().Where(t => t.Name == "MapLayerBlur").Single();
        private object mlBlurInst;

        private NormalizedSimplexNoise noisegenA, noisegenR, noisegenG, noisegenB;
        private double ridgedMul;

        private float multiplier;
        private double[] thresholds;

        public MapLayerOreVeins(long seed, int octaves, float persistence, int multiplier, int scaleA, int scaleR, int scaleG, int scaleB, double ridgedMul = 2.0) : base(seed)
        {
            mlBlurInst = AccessTools.CreateInstance(mlBlurT);
            this.ridgedMul = ridgedMul;

            noisegenA = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleA, persistence, seed + 7312654);
            noisegenR = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleR, persistence, seed + 5498987);
            noisegenG = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleG, persistence, seed + 2987992);
            noisegenB = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleB, persistence, seed + 4987462);

            this.multiplier = multiplier;
        }

        public MapLayerOreVeins(long seed, int octaves, float persistence, int scale, int multiplier, int scaleA, int scaleR, int scaleG, int scaleB, double[] thresholds, double ridgedMul = 2.0) : base(seed)
        {
            mlBlurInst = AccessTools.CreateInstance(mlBlurT);

            this.ridgedMul = ridgedMul;

            noisegenA = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleA, persistence, seed + 7312654);
            noisegenR = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleR, persistence, seed + 5498987);
            noisegenG = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleG, persistence, seed + 2987992);
            noisegenB = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleB, persistence, seed + 4987462);

            this.multiplier = multiplier;
            this.thresholds = thresholds;
        }

        public int GetRGBANoise(int xCoord, int x, int zCoord, int z, int flags = 0, double[] thresholds = null)
        {
            bool inverse = (flags & 0b10000) > 0;

            double nR, nG, nB, nA;
            nR = nG = nB = nA = inverse ? 1 : 0;

            double nRX = xCoord + x;
            double nRZ = zCoord + z;
            double nGX = xCoord + x;
            double nGZ = zCoord + z;
            double nBX = xCoord + x;
            double nBZ = zCoord + z;
            double nAX = xCoord + x;
            double nAZ = zCoord + z;
            
            int onCol = flags >> 5;

            if (thresholds != null)
            {
                switch (onCol)
                {
                    case 1:
                        nA = noisegenA.Noise(nAX, nAZ, thresholds);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                        if ((inverse && nA < 1) || (!inverse && nA > 0))
                        {
                            nR = noisegenR.Noise(nRX, nRZ, thresholds);
                            nG = noisegenG.Noise(nGX, nGZ, thresholds);
                            nB = noisegenB.Noise(nBX, nBZ, thresholds);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        }
                        break;
                    case 2:
                        nR = noisegenR.Noise(nRX, nRZ, thresholds);
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                        if ((inverse && nR < 1) || (!inverse && nR > 0))
                        {
                            nA = noisegenA.Noise(nAX, nAZ, thresholds);
                            nG = noisegenG.Noise(nGX, nGZ, thresholds);
                            nB = noisegenB.Noise(nBX, nBZ, thresholds);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        }
                        break;
                    case 3:
                        nG = noisegenG.Noise(nGX, nGZ, thresholds);
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                        if ((inverse && nG < 1) || (!inverse && nG > 0))
                        {
                            nA = noisegenA.Noise(nAX, nAZ, thresholds);
                            nR = noisegenR.Noise(nRX, nRZ, thresholds);
                            nB = noisegenB.Noise(nBX, nBZ, thresholds);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        }
                        break;
                    case 4:
                        nB = noisegenB.Noise(nBX, nBZ, thresholds);
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        if ((inverse && nB < 1) || (!inverse && nB > 0))
                        {
                            nA = noisegenA.Noise(nAX, nAZ, thresholds);
                            nR = noisegenR.Noise(nRX, nRZ, thresholds);
                            nG = noisegenG.Noise(nGX, nGZ, thresholds);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                        }
                        break;
                    default:
                        nA = noisegenA.Noise(nAX, nAZ, thresholds);
                        nR = noisegenR.Noise(nRX, nRZ, thresholds);
                        nG = noisegenG.Noise(nGX, nGZ, thresholds);
                        nB = noisegenB.Noise(nBX, nBZ, thresholds);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        break;
                }
            }
            else
            {
                switch (onCol)
                {
                    case 1:
                        nA = noisegenA.Noise(nAX, nAZ);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                        if ((inverse && nA < 1) || (!inverse && nA > 0))
                        {
                            nR = noisegenR.Noise(nRX, nRZ);
                            nG = noisegenG.Noise(nGX, nGZ);
                            nB = noisegenB.Noise(nBX, nBZ);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        }
                        break;
                    case 2:
                        nR = noisegenR.Noise(nRX, nRZ);
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                        if ((inverse && nR < 1) || (!inverse && nR > 0))
                        {
                            nA = noisegenA.Noise(nAX, nAZ);
                            nG = noisegenG.Noise(nGX, nGZ);
                            nB = noisegenB.Noise(nBX, nBZ);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        }
                        break;
                    case 3:
                        nG = noisegenG.Noise(nGX, nGZ);
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                        if ((inverse && nG < 1) || (!inverse && nG > 0))
                        {
                            nA = noisegenA.Noise(nAX, nAZ);
                            nR = noisegenR.Noise(nRX, nRZ);
                            nB = noisegenB.Noise(nBX, nBZ);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        }
                        break;
                    case 4:
                        nB = noisegenB.Noise(nBX, nBZ);
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        if ((inverse && nB < 1) || (!inverse && nB > 0))
                        {
                            nA = noisegenA.Noise(nAX, nAZ);
                            nR = noisegenR.Noise(nRX, nRZ);
                            nG = noisegenG.Noise(nGX, nGZ);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                        }
                        break;
                    default:
                        nA = noisegenA.Noise(nAX, nAZ);
                        nR = noisegenR.Noise(nRX, nRZ);
                        nG = noisegenG.Noise(nGX, nGZ);
                        nB = noisegenB.Noise(nBX, nBZ);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
                        break;
                }
            }

            byte r = (byte)GameMath.Clamp(multiplier * nR, 0, 255);
            byte g = (byte)GameMath.Clamp(multiplier * nG, 0, 255);
            byte b = (byte)GameMath.Clamp(multiplier * nB, 0, 255);
            byte a = (byte)GameMath.Clamp(multiplier * nA, 0, 255);

            int rgba = b | g << 8 | r << 16 | a << 24;

            return inverse ? ~rgba : rgba;
        }

        public int GetRGBAScribbleNoise(int xCoord, int x, int zCoord, int z, int flags = 0, double[] thresholds = null, int depth = 2)
        {
            double nR, nG, nB, nA;
            nR = nG = nB = nA = 0.0;

            double nRX = xCoord + x + 0000000;
            double nRZ = zCoord + z + 0000000;
            double nGX = xCoord + x + 5498987;
            double nGZ = zCoord + z + 5498987;
            double nBX = xCoord + x + 2987992;
            double nBZ = zCoord + z + 2987992;
            double nAX = xCoord + x + 4987462;
            double nAZ = zCoord + z + 4987462;

            for (int i = 1; i <= depth; i++)
            {
                double nRt, nGt, nBt, nAt;

                if (thresholds != null)
                {
                    nRt = noisegenR.Noise((nRX * i) + (i * 512), (nRZ * i) + (i * 512), thresholds) / depth;
                    nGt = noisegenG.Noise((nGX * i) + (i * 512), (nGZ * i) + (i * 512), thresholds) / depth;
                    nBt = noisegenB.Noise((nBX * i) + (i * 512), (nBZ * i) + (i * 512), thresholds) / depth;
                    nAt = noisegenA.Noise((nAX * i) + (i * 512), (nAZ * i) + (i * 512), thresholds) / depth;
                }
                else
                {
                    nRt = noisegenR.Noise((nRX * i) + (i * 512), (nRZ * i) + (i * 512)) / depth;
                    nGt = noisegenG.Noise((nGX * i) + (i * 512), (nGZ * i) + (i * 512)) / depth;
                    nBt = noisegenB.Noise((nBX * i) + (i * 512), (nBZ * i) + (i * 512)) / depth;
                    nAt = noisegenA.Noise((nAX * i) + (i * 512), (nAZ * i) + (i * 512)) / depth;
                }

                nR += (flags & 0b00001) > 0 ? Math.Abs((nRt * depth - 0.5) * 2.0) / depth : nRt;
                nG += (flags & 0b00010) > 0 ? Math.Abs((nGt * depth - 0.5) * 2.0) / depth : nGt;
                nB += (flags & 0b00100) > 0 ? Math.Abs((nBt * depth - 0.5) * 2.0) / depth : nBt;
                nA += (flags & 0b01000) > 0 ? Math.Abs((nAt * depth - 0.5) * 2.0) / depth : nAt;
            }

            bool inverse = (flags & 0b10000) > 0;

            byte r = (byte)GameMath.Clamp(multiplier * nR, 0, 255);
            byte g = (byte)GameMath.Clamp(multiplier * nG, 0, 255);
            byte b = (byte)GameMath.Clamp(multiplier * nB, 0, 255);
            byte a = (byte)GameMath.Clamp(multiplier * nA, 0, 255);

            int rgba = b | g << 8 | r << 16 | a << 24;

            return inverse ? ~rgba : rgba;
        }

        public virtual int[] GenLayer(int xCoord, int zCoord, int sizeXSmall, int sizeZSmall, int sizeXLarge, int sizeZLarge)
        {
            int smallSize = (sizeXSmall + sizeZSmall) / 2;
            int largeSize = (sizeXLarge + sizeZLarge) / 2;

            int step = largeSize / smallSize / 2;

            int[] smallData = GenLayer(xCoord, zCoord, largeSize, largeSize, step * 2);
            int[] largeData = new int[largeSize * largeSize];

            for (int z = 0; z < largeSize; ++z)
            {
                for (int x = 0; x < largeSize; ++x)
                {
                    int pX = (int)(((float)x / largeSize) * smallSize);
                    int pZ = (int)(((float)z / largeSize) * smallSize);

                    largeData[z * largeSize + x] = smallData[pZ * smallSize + pX];
                }
            }

            return largeData;
        }

        public virtual int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ, int step)
        {
            int[] outData = new int[sizeX * sizeZ / step];

            int? li = null;
            for (int z = 0; z < sizeZ; ++z)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    int flags = 0b1010001;
                    int ssX = sizeX / step;
                    int ssZ = sizeZ / step;

                    int lx = (int)(((float)x / sizeX) * ssX);
                    int lz = (int)(((float)z / sizeZ) * ssZ);

                    int li2 = lz * ssX + lx;

                    if (li2 == li) continue;

                    li = li2;

                    outData[li ?? 0] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                }
            }

            return outData;
        }

        public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            int[] outData = new int[sizeX * sizeZ];

            if (thresholds != null)
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        int flags = 0b1010001;
                        outData[z * sizeX + x] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                    }
                }
            }
            else
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        int flags = 0b1010001;
                        outData[z * sizeX + x] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                    }
                }
            }

            return outData;
        }

        public int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ, double[] thresholds)
        {
            int[] outData = new int[sizeX * sizeZ];

            for (int z = 0; z < sizeZ; ++z)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    int flags = 0b10001;
                    outData[z * sizeX + x] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                }
            }

            return outData;
        }
    }

    [HarmonyPatch(typeof(NoiseOre), "GetLerpedOreValueAt")]
    public class TestPatch
    {
        public static void Postfix(ref int __result)
        {
            if (__result != 0)
            {
            }
        }
    }

    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname) => (T)AccessTools.Field(instance.GetType(), fieldname)?.GetValue(instance);

        public static T GetProperty<T>(this object instance, string fieldname) => (T)AccessTools.Property(instance.GetType(), fieldname)?.GetValue(instance);

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

        public static void CallMethod(this object instance, string method) => instance?.CallMethod(method, null);

        public static void CallMethod(this object instance, string method, params object[] args) => instance?.CallMethod<object>(method, args);

        public static T CallMethod<T>(this object instance, string method) => (T)instance.CallMethod<object>(method, null);

        public static T CallMethod<T>(this object instance, string method, params object[] args)
        {
            Type[] parameters = null;
            if (args != null)
            {
                parameters = args.Length > 0 ? new Type[args.Length] : null;
                for (int i = 0; i < args.Length; i++)
                {
                    parameters[i] = args[i].GetType();
                }
            }
            return (T)instance?.GetMethod(method, parameters).Invoke(instance, args);
        }

        public static MethodInfo GetMethod(this object instance, string method, params Type[] parameters) => instance.GetMethod(method, parameters, null);

        public static MethodInfo GetMethod(this object instance, string method, Type[] parameters = null, Type[] generics = null) => AccessTools.Method(instance.GetType(), method, parameters, generics);
    }
}