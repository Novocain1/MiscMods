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
                    int heightY = heightMap[z * chunksize + x];

                    int veinAtPos = veinMap.GetInt((int)(rdx * veinStep + x), (int)(rdz * veinStep + z));

                    float veinARel = (((uint)veinAtPos & ~0x00FFFFFF) >> 24) / 255f;
                    float veinRRel = (((uint)veinAtPos & ~0xFF00FFFF) >> 16) / 255f;
                    float veinGRel = (((uint)veinAtPos & ~0xFFFF00FF) >> 08) / 255f;
                    float veinBRel = (((uint)veinAtPos & ~0xFFFFFF00) >> 00) / 255f;

                    if (veinRRel > 0.85f)
                    {
                        int y = (int)(veinGRel * heightY) / 2;

                        int depth = (int)(veinARel * 4);

                        for (int dy = -depth; dy < depth; dy++)
                        {
                            if (y + dy > 0 && y + dy < api.WorldManager.MapSizeY)
                            {
                                int chunkY = (y + dy) / chunksize;
                                int lY = (y + dy) % chunksize;

                                int index3d = (chunksize * lY + z) * chunksize + x;
                                int blockId = chunks[chunkY].Blocks[index3d];
                                chunks[chunkY].Blocks[index3d] = 0;
                            }
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
            OreVeinLayer = new MapLayerOreVeins(api.World.Seed, 8, 0.0f, 255, 64, 512, 64, 64, 32.0);
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
        NormalizedSimplexNoise noisegenA, noisegenR, noisegenG, noisegenB;
        double ridgedMul;

        float multiplier;
        double[] thresholds;

        public MapLayerOreVeins(long seed, int octaves, float persistence, int multiplier, int scaleA, int scaleR, int scaleG, int scaleB, double ridgedMul = 2.0) : base(seed)
        {
            this.ridgedMul = ridgedMul;

            noisegenA = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleA, persistence, seed + 7312654);
            noisegenR = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleR, persistence, seed + 5498987);
            noisegenG = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleG, persistence, seed + 2987992);
            noisegenB = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 1f / scaleB, persistence, seed + 4987462);

            this.multiplier = multiplier;
        }

        public MapLayerOreVeins(long seed, int octaves, float persistence, int scale, int multiplier, int scaleA, int scaleR, int scaleG, int scaleB, double[] thresholds, double ridgedMul = 2.0) : base(seed)
        {
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
            double nR, nG, nB, nA;

            double nRX = xCoord + x;
            double nRZ = zCoord + z;
            double nGX = xCoord + x;
            double nGZ = zCoord + z;
            double nBX = xCoord + x;
            double nBZ = zCoord + z;
            double nAX = xCoord + x;
            double nAZ = zCoord + z;

            if (thresholds != null)
            {
                nA = noisegenA.Noise(nAX, nAZ, thresholds);
                nR = noisegenR.Noise(nRX, nRZ, thresholds);
                nG = noisegenG.Noise(nGX, nGZ, thresholds);
                nB = noisegenB.Noise(nBX, nBZ, thresholds);
            }
            else
            {
                nA = noisegenA.Noise(nAX, nAZ);
                nR = noisegenR.Noise(nRX, nRZ);
                nG = noisegenG.Noise(nGX, nGZ);
                nB = noisegenB.Noise(nBX, nBZ);
            }

            bool inverse = (flags & 0b10000) > 0;

            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * ridgedMul);
            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * ridgedMul);
            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * ridgedMul);
            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * ridgedMul);

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
                        int flags = 0b10001;
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
}
