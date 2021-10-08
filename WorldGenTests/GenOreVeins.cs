using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace WorldGenTests
{
    public class GenOreVeins : ModSystem
    {
        private Harmony harmony;
        private ICoreServerAPI api;
        LCGRandom rand = new LCGRandom();
        
        Argb8 veinAtPos = new Argb8();
        Argb8 surfaceAtPos = new Argb8();

        private const string patchCode = "Novocain.ModSystem.GenOreVeins";

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

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
            rand.SetWorldSeed(api.World.Seed + 56498846451);
            api.Event.InitWorldGenerator(Init, "standard");
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.TerrainFeatures, "standard");
        }

        private Dictionary<string, DepositVariant> DepositByCode = new Dictionary<string, DepositVariant>();

        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            int seaLevel = api.World.SeaLevel;
            int mapSizeY = api.WorldManager.MapSizeY;

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
                        rand.InitPositionSeed(chunkX + x, chunkZ + z);
                        int dCx = rand.NextInt(9) - 4;
                        int dCz = rand.NextInt(9) - 4;

                        int height = heightMap[z * chunksize + x];

                        veinAtPos.Value = veinMap.GetInt((int)(rdx * veinStep + x), (int)(rdz * veinStep + z));
                        surfaceAtPos.Value = veinMap.GetInt((int)GameMath.Clamp(rdx * veinStep + (x + dCx), 0, veinMap.Size - 1), (int)GameMath.Clamp(rdz * veinStep + (z + dCz), 0, veinMap.Size - 1));
                        
                        if (veinAtPos.Value == 0 && surfaceAtPos.Value == 0) continue;

                        int oreUpLeft = oreMap.GetUnpaddedInt((int)(rdx * oreStep), (int)(rdz * oreStep));
                        int oreUpRight = oreMap.GetUnpaddedInt((int)(rdx * oreStep + oreStep), (int)(rdz * oreStep));
                        int oreBotLeft = oreMap.GetUnpaddedInt((int)(rdx * oreStep), (int)(rdz * oreStep + oreStep));
                        int oreBotRight = oreMap.GetUnpaddedInt((int)(rdx * oreStep + oreStep), (int)(rdz * oreStep + oreStep));
                        uint oreMapInt = (uint)GameMath.BiLerp(oreUpLeft, oreUpRight, oreBotLeft, oreBotRight, (float)x / chunksize, (float)z / chunksize);

                        float oreMapRel = ((oreMapInt & ~0xFF00FF) >> 8) / 15f;

                        if (veinAtPos.Rrel > 0.0)
                        {
                            int y = (int)((veinAtPos.Grel * mapSizeY) % height);

                            int depth = (int)(veinAtPos.Rrel * 10) + 1;

                            for (int dy = -depth; dy < depth; dy++)
                            {
                                if (y + dy > 0 && y + dy < mapSizeY)
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
                                                if (veinAtPos.Brel > 0.5)
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
                                    }
                                }
                            }
                        }
                        if (surfaceAtPos.Rrel > 0.0)
                        {
                            //gen surface deposits
                            int y = (int)((surfaceAtPos.Grel * mapSizeY) % height);

                            int chunkY = y / chunksize;
                            int lY = y % chunksize;

                            int index3d = (chunksize * lY + z) * chunksize + x;
                            int blockId = chunks[chunkY].Blocks[index3d];

                            if (surfaceBlockByInBlockId?.ContainsKey(blockId) ?? false)
                            {
                                if (height < mapSizeY && surfaceAtPos.Brel > 0.5f)
                                {
                                    Block belowBlock = api.World.Blocks[chunks[height / chunksize].Blocks[(height % chunksize * chunksize + z) * chunksize + x]];

                                    index3d = ((height + 1) % chunksize * chunksize + z) * chunksize + x;
                                    if (belowBlock.SideSolid[BlockFacing.UP.Index] && chunks[(height + 1) / chunksize].Blocks[index3d] == 0)
                                    {
                                        chunks[(height + 1) / chunksize].Blocks[index3d] = surfaceBlockByInBlockId[blockId].Blocks[0].BlockId;
#if DEBUG
                                        //so I can see it better when debugging
                                        index3d = (height % chunksize * chunksize + z) * chunksize + x;
                                        chunks[height / chunksize].Blocks[index3d] = 1;
#endif
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
                            Argb8 test = new Argb8(veinMap.GetInt(x, y));

                            bmp.SetPixel(x, y, Color.FromArgb(test.Value));
                        }
                    }
                    Directory.CreateDirectory(Path.Combine(path.FullName, vein.Key.UcFirst()));

                    string pt = Path.Combine(path.FullName, vein.Key.UcFirst(), string.Format("{0}, {1}.png", regionX, regionZ));
                    
                    bmp.Save(pt, ImageFormat.Png);
#if DEBUG
                    //break;
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
                var OreVeinLayer = new MapLayerFractalARGB(api.World.Seed + i, 8, 0.0f, 64, 2048, 1024, 64, 128.0, 0.001);
                int regionSize = api.WorldManager.RegionSize;

                IntDataMap2D data = new IntDataMap2D()
                {
                    //Data = OreVeinLayer.GenLayerDiffuse(regionX * regionSize, regionZ * regionSize, 128, regionSize, 0b1010001, 2, 0, 4, 8),
                    Data = OreVeinLayer.GenLayerGL(regionX * regionSize, regionZ * regionSize, api.World.Seed + i * 100),
                    Size = regionSize,
                    BottomRightPadding = 0,
                    TopLeftPadding = 0
                };

                maps[val.Key] = data;
                i++;
#if DEBUG
                api.Logger.StoryEvent(Math.Round(((float)i / mapRegion.OreMaps.Count) * 100, 2).ToString());
#endif
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
}