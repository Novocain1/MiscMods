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

            OreVeinLayer = new MapLayerOreVeins(api.World.Seed + 49055687, 8, 0.0f, 32, 255);
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
        }
        
        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            IntDataMap2D veinData = SerializerUtil.Deserialize<IntDataMap2D>(chunks[0].MapChunk.MapRegion.GetModdata("OreVeinData"));
            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;
            int chunksize = api.World.BlockAccessor.ChunkSize;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {

                }
            }
        }

        public void DebugRGBMap(ICoreServerAPI api)
        {
            int[] colors = OreVeinLayer.GenLayer(0, 0, 512, 512);
            Bitmap bmp = new Bitmap(512, 512);
            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 512; y++)
                {
                    bmp.SetPixel(x, y, Color.FromArgb(colors[y * 512 + x]));
                }
            }
            bmp.Save("noise.png", ImageFormat.Png);
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            int pad = TerraGenConfig.geoProvMapPadding;

            IntDataMap2D data = new IntDataMap2D()
            {
                Data = OreVeinLayer.GenLayer(
                    regionX * 512 - pad,
                    regionZ * 512 - pad,
                    512 + 2 * pad,
                    512 + 2 * pad
                ),
                Size = 513,
                BottomRightPadding = 1
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


        public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            int[] outData = new int[sizeX * sizeZ];

            if (thresholds != null)
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        double nR = noisegen.Noise(xCoord + x, zCoord + z, thresholds);

                        double nGX = xCoord + x + 5498987;
                        nGX *= 0.25;

                        double nGZ = zCoord + z + 5498987;
                        nGZ *= 0.25;

                        double nBX = xCoord + x + 2987992;
                        nBX *= 0.25;

                        double nBZ = zCoord + z + 2987992;
                        nBZ *= 0.25;

                        double nG = noisegen.Noise(nGX, nGZ, thresholds);
                        double nB = noisegen.Noise(nBX, nBZ, thresholds);

                        int r = (int)GameMath.Clamp(multiplier * (1.0 - Math.Abs((nR - 0.5) * 2.0)), 0, 255);
                        r = r > 200 ? r : 0;

                        int g = (int)GameMath.Clamp(multiplier * (1.0 - nG), 0, 255);
                        int b = (int)GameMath.Clamp(multiplier * (1.0 - nB), 0, 255);

                        outData[z * sizeX + x] = b | g << 8 | r << 16 | 0xFF << 24;
                    }
                }
            }
            else
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        double nR = noisegen.Noise(xCoord + x, zCoord + z);
                        
                        double nGX = xCoord + x + 5498987;
                        nGX *= 0.25;

                        double nGZ = zCoord + z + 5498987;
                        nGZ *= 0.25;

                        double nBX = xCoord + x + 2987992;
                        nBX *= 0.25;

                        double nBZ = zCoord + z + 2987992;
                        nBZ *= 0.25;

                        double nG = noisegen.Noise(nGX, nGZ);
                        double nB = noisegen.Noise(nBX, nBZ);

                        int r = (int)GameMath.Clamp(multiplier * (1.0 - Math.Abs((nR - 0.5) * 2.0)), 0, 255);
                        r = r > 200 ? r : 0;
                        
                        int g = (int)GameMath.Clamp(multiplier * (1.0 - nG), 0, 255);
                        int b = (int)GameMath.Clamp(multiplier * (1.0 - nB), 0, 255);

                        outData[z * sizeX + x] = b | g << 8 | r << 16 | 0xFF << 24;
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
                    double nR = noisegen.Noise(xCoord + x, zCoord + z, thresholds);

                    double nGX = xCoord + x + 5498987;
                    nGX *= 0.25;

                    double nGZ = zCoord + z + 5498987;
                    nGZ *= 0.25;

                    double nBX = xCoord + x + 2987992;
                    nBX *= 0.25;

                    double nBZ = zCoord + z + 2987992;
                    nBZ *= 0.25;

                    double nG = noisegen.Noise(nGX, nGZ, thresholds);
                    double nB = noisegen.Noise(nBX, nBZ, thresholds);

                    int r = (int)GameMath.Clamp(multiplier * (1.0 - Math.Abs((nR - 0.5) * 2.0)), 0, 255);
                    r = r > 200 ? r : 0;

                    int g = (int)GameMath.Clamp(multiplier * (1.0 - nG), 0, 255);
                    int b = (int)GameMath.Clamp(multiplier * (1.0 - nB), 0, 255);

                    outData[z * sizeX + x] = b | g << 8 | r << 16 | 0xFF << 24;
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
