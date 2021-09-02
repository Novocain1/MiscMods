using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace WorldGenTests
{
    public class TestMod : ModSystem
    {
        Harmony harmony;
        ICoreServerAPI api;

        const string patchCode = "Novocain.ModSystem.TestMod";

        MapLayerBase OreVeinLayer;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony(patchCode);
            harmony.PatchAll();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;

            OreVeinLayer = new MapLayerOreVeins(api.World.Seed + 49055687, 8, 0.0f, 64, 255);
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");

            api.RegisterCommand("veinmap", "", "", (a, b, c) => DebugRGBMap(api, a));
        }
        
        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            IntDataMap2D veinMap = SerializerUtil.Deserialize<IntDataMap2D>(chunks[0].MapChunk.MapRegion.GetModdata("OreVeinData"));
            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;
            int chunksize = api.World.BlockAccessor.ChunkSize;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;
            float veinStep = (float)veinMap.InnerSize / regionChunkSize;

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {
                    int posY = heightMap[z * chunksize + x];

                    int veinAtPos = veinMap.GetInt((int)(rdx * veinStep + x), (int)(rdz * veinStep + z));

                    float veinRRel = ((veinAtPos & ~0xFF00FFFF) >> 16) / 255f;

                    if (veinRRel > 0.6f)
                    {
                        int chunkY = 142 / chunksize;
                        int lY = 142 % chunksize;
                        float depth = veinRRel - 0.7843137254901961f;

                        for (int dY = -2; dY < 2; dY++)
                        {
                            int y = lY + dY;
                            int absDY = Math.Abs(dY);

                            int index3d = (chunksize * y + z) * chunksize + x;
                            int blockId = chunks[chunkY].Blocks[index3d];
                            if (depth * absDY < 0.5) chunks[chunkY].Blocks[index3d] = 1;
                        }
                    }
                    else
                    {

                    }
                }
            }
        }

        public void DebugRGBMap(ICoreServerAPI api, IServerPlayer player)
        {
            var chunk = api.WorldManager.GetChunk(player.Entity.ServerPos.AsBlockPos);

            IntDataMap2D veinMap = SerializerUtil.Deserialize<IntDataMap2D>(chunk.MapChunk.MapRegion.GetModdata("OreVeinData"));

            Bitmap bmp = new Bitmap(512, 512);
            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 512; y++)
                {
                    bmp.SetPixel(x, y, Color.FromArgb(veinMap.GetInt(x, y)));
                }
            }
            bmp.Save("noise.png", ImageFormat.Png);
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            int regionSize = api.WorldManager.RegionSize;
            IntDataMap2D data = new IntDataMap2D()
            {
                Data = OreVeinLayer.GenLayer(
                    regionX * regionSize,
                    regionZ * regionSize,
                    regionSize + 2,
                    regionSize + 2
                ),
                Size = regionSize + 2,
                BottomRightPadding = 0,
                TopLeftPadding = 0
            };
            mapRegion.SetModdata("OreVeinData", SerializerUtil.Serialize(data));
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(patchCode);
        }
    }

    public class MapLayerOreVeins : MapLayerBase
    {
        NormalizedSimplexNoise noisegen;

        float multiplier;
        double[] thresholds;

        public MapLayerOreVeins(long seed, int octaves, float persistence, int scale, int multiplier) : base(seed)
        {
            noisegen = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scale, persistence, seed + 12321);
            this.multiplier = multiplier;
        }

        public MapLayerOreVeins(long seed, int octaves, float persistence, int scale, int multiplier, double[] thresholds) : base(seed)
        {
            noisegen = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scale, persistence, seed + 12321);
            this.multiplier = multiplier;
            this.thresholds = thresholds;
        }


        public int GetRGBANoise(int xCoord, int x, int zCoord, int z, int flags = 0, double[] thresholds = null)
        {
            double nR, nG, nB, nA;

            double nRX = xCoord + x + 0000000;
            double nRZ = zCoord + z + 0000000;
            double nGX = xCoord + x + 5498987;
            double nGZ = zCoord + z + 5498987;
            double nBX = xCoord + x + 2987992;
            double nBZ = zCoord + z + 2987992;
            double nAX = xCoord + x + 4987462;
            double nAZ = zCoord + z + 4987462;

            if (thresholds != null)
            {
                nR = noisegen.Noise(nRX, nRZ, thresholds);
                nG = noisegen.Noise(nGX, nGZ, thresholds);
                nB = noisegen.Noise(nBX, nBZ, thresholds);
                nA = noisegen.Noise(nAX, nAZ, thresholds);
            }
            else
            {
                nR = noisegen.Noise(nRX, nRZ);
                nG = noisegen.Noise(nGX, nGZ);
                nB = noisegen.Noise(nBX, nBZ);
                nA = noisegen.Noise(nAX, nAZ);
            }

            bool inverse = (flags & 0b10000) > 0;

            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2.0);
            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2.0);
            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2.0);
            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2.0);

            byte r = (byte)GameMath.Clamp(multiplier * nR, 0, 255);
            byte g = (byte)GameMath.Clamp(multiplier * nG, 0, 255);
            byte b = (byte)GameMath.Clamp(multiplier * nB, 0, 255);
            byte a = (byte)GameMath.Clamp(multiplier * nA, 0, 255);

            int rgba = b | g << 8 | r << 16 | a << 24;

            return inverse ? ~rgba : rgba;
        }

        public int GetRGBAScribbleNoise(int xCoord, int x, int zCoord, int z, int flags = 0, double[] thresholds = null, int depth = 8)
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

            for (int i = 1; i < depth; i++)
            {
                double nRt, nGt, nBt, nAt;
                nRt = nGt = nBt = nAt = 0.0;

                if (thresholds != null)
                {
                    nRt = noisegen.Noise(nRX * i, nRZ * i, thresholds) / depth;
                    nGt = noisegen.Noise(nGX * i, nGZ * i, thresholds) / depth;
                    nBt = noisegen.Noise(nBX * i, nBZ * i, thresholds) / depth;
                    nAt = noisegen.Noise(nAX * i, nAZ * i, thresholds) / depth;
                }
                else
                {
                    nRt = noisegen.Noise(nRX * i, nRZ * i) / depth;
                    nGt = noisegen.Noise(nGX * i, nGZ * i) / depth;
                    nBt = noisegen.Noise(nBX * i, nBZ * i) / depth;
                    nAt = noisegen.Noise(nAX * i, nAZ * i) / depth;
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

        public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            int[] outData = new int[sizeX * sizeZ];

            if (thresholds != null)
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        int flags = 0b10001;
                        outData[z * sizeX + x] = GetRGBAScribbleNoise(xCoord, x, zCoord, z, flags, thresholds);
                    }
                }
            }
            else
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        int flags = 0b10001;
                        outData[z * sizeX + x] = GetRGBAScribbleNoise(xCoord, x, zCoord, z, flags);
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
                    outData[z * sizeX + x] = GetRGBAScribbleNoise(xCoord, x, zCoord, z, flags, thresholds);
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
}
