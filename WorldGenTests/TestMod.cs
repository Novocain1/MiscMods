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
            
            int veinUpLeft = veinMap.GetInt((int)(rdx * veinStep), (int)(rdz * veinStep));
            int veinUpRight = veinMap.GetInt((int)(rdx * veinStep + veinStep), (int)(rdz * veinStep));
            int veinBotLeft = veinMap.GetInt((int)(rdx * veinStep), (int)(rdz * veinStep + veinStep));
            int veinBotRight = veinMap.GetInt((int)(rdx * veinStep + veinStep), (int)(rdz * veinStep + veinStep));

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {
                    int posY = heightMap[z * chunksize + x];
                    
                    float veinRRel = GameMath.BiLerp((veinUpLeft & ~0xFFFFFF00) >> 00, (veinUpRight & ~0xFFFFFF00) >> 00, (veinBotLeft & ~0xFFFFFF00) >> 00, (veinBotRight & ~0xFFFFFF00) >> 00, (float)x / chunksize, (float)z / chunksize) / 255f;
                    float veinGRel = GameMath.BiLerp((veinUpLeft & ~0xFFFF00FF) >> 08, (veinUpRight & ~0xFFFF00FF) >> 08, (veinBotLeft & ~0xFFFF00FF) >> 08, (veinBotRight & ~0xFFFF00FF) >> 08, (float)x / chunksize, (float)z / chunksize) / 255f;
                    float veinBRel = GameMath.BiLerp((veinUpLeft & ~0xFF00FFFF) >> 16, (veinUpRight & ~0xFF00FFFF) >> 16, (veinBotLeft & ~0xFF00FFFF) >> 16, (veinBotRight & ~0xFF00FFFF) >> 16, (float)x / chunksize, (float)z / chunksize) / 255f;
                    float veinARel = GameMath.BiLerp((veinUpLeft & ~0x00FFFFFF) >> 24, (veinUpRight & ~0x00FFFFFF) >> 24, (veinBotLeft & ~0x00FFFFFF) >> 24, (veinBotRight & ~0x00FFFFFF) >> 24, (float)x / chunksize, (float)z / chunksize) / 255f;

                    int chunkY = 142 / chunksize;
                    int lY = 142 % chunksize;
                    int index3d = (chunksize * lY + z) * chunksize + x;
                    int blockId = chunks[chunkY].Blocks[index3d];

                    if (veinRRel > 0.5f)
                    {
                        chunks[chunkY].Blocks[index3d] = 1;
                    }
                    else
                    {

                    }
                }
            }
        }

        public void DebugRGBMap(ICoreServerAPI api, IServerPlayer player)
        {
            int[] colors = OreVeinLayer.GenLayer(0, 0, 512, 512);

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
            int pad = TerraGenConfig.geoProvMapPadding;
            int regionSize = api.WorldManager.RegionSize;
            IntDataMap2D data = new IntDataMap2D()
            {
                Data = OreVeinLayer.GenLayer(
                    regionX * regionSize - pad,
                    regionZ * regionSize - pad,
                    regionSize + 2 * pad,
                    regionSize + 2 * pad
                ),
                Size = regionSize + 2 * pad,
                BottomRightPadding = 1,
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
                        double nRX = xCoord + x + 0000000;

                        double nRZ = zCoord + z + 0000000;

                        double nGX = xCoord + x + 5498987;

                        double nGZ = zCoord + z + 5498987;

                        double nBX = xCoord + x + 2987992;

                        double nBZ = zCoord + z + 2987992;

                        double nR = noisegen.Noise(nRX, nRZ, thresholds);
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
                        double nRX = xCoord + x + 0000000;

                        double nRZ = zCoord + z + 0000000;

                        double nGX = xCoord + x + 5498987;

                        double nGZ = zCoord + z + 5498987;

                        double nBX = xCoord + x + 2987992;

                        double nBZ = zCoord + z + 2987992;

                        double nR = noisegen.Noise(nRX, nRZ);
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
                    double nRX = xCoord + x + 0000000;

                    double nRZ = zCoord + z + 0000000;

                    double nGX = xCoord + x + 5498987;

                    double nGZ = zCoord + z + 5498987;

                    double nBX = xCoord + x + 2987992;

                    double nBZ = zCoord + z + 2987992;

                    double nR = noisegen.Noise(nRX, nRZ, thresholds);
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
