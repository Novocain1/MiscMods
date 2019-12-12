/*
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace VSHUD
{
    class MapUpdateData
    {
        public MapUpdateData(Vec2i coord, IMapChunk mapChunk)
        {
            Coord = coord;
            MapChunk = mapChunk;
        }

        public Vec2i Coord { get; set; }
        public IMapChunk MapChunk { get; set; }
    }



    class Persistency : ModSystem
    {
        public ICoreClientAPI capi;
        public string MapDataPath { get => capi.GetOrCreateDataPath(@"VSHUD\MapCache\" + capi.World.Seed); }
        public UniqueQueue<MapUpdateData> mapUpdateDatas = new UniqueQueue<MapUpdateData>();
        public UniqueQueue<string> mapFiles = new UniqueQueue<string>();
        public List<MapComponent> mapComponents { get => capi.OpenedGuis.OfType<GuiDialogWorldMap>()?.SingleOrDefault()?.mapComponents; }
        public ChunkMapLayer Map { get => capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<ChunkMapLayer>().Single(); }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.Event.LevelFinalize += () =>
            {
                UpdateFileList();
                api.Event.RegisterGameTickListener(SaveNextImage, 30);
                api.Event.RegisterGameTickListener(dt => UpdateFileList(), 1000);
            };
            api.Event.ChunkDirty += AddItem;
        }

        private void AddItem(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
        {
            if (chunkCoord.Y != chunk.MapChunk.YMax / capi.World.BlockAccessor.ChunkSize) return;
            Vec2i pos = new Vec2i(chunkCoord.X, chunkCoord.Z);
            mapUpdateDatas.Enqueue(new MapUpdateData(pos, chunk.MapChunk));
        }

        private void SaveNextImage(float dt)
        {
            if (mapUpdateDatas.Count == 0) return;
            var data = mapUpdateDatas.Dequeue();
            GenerateChunkImage(data.Coord, data.MapChunk).Save(Path.Combine(MapDataPath, data.Coord.X + "_" + data.Coord.Y + ".png"), ImageFormat.Png);
        }

        private void UpdateFileList()
        {
            string[] files = Directory.GetFiles(MapDataPath, "*.png");
            foreach (var val in files)
            {
                mapFiles.Enqueue(val);
            }
        }

        //credit Melchior
        public Bitmap GenerateChunkImage(Vec2i chunkPos, IMapChunk mc)
        {
            BlockPos tmpPos = new BlockPos();
            Vec2i localpos = new Vec2i();
            int chunkSize = capi.World.BlockAccessor.ChunkSize;
            var chunksColumn = new IWorldChunk[capi.World.BlockAccessor.MapSizeY / chunkSize];
            Bitmap chunkImage = new Bitmap(chunkSize, chunkSize, PixelFormat.Format24bppRgb);
            int topChunkY = mc.YMax / chunkSize;//Heywaitaminute -- this isn't a highest FEATURE, if Rainmap isn't accurate!
                                                //Metadata of DateTime chunk was edited, chunk coords.,world-seed? Y-Max feature height
                                                //Grab a chunk COLUMN... Topmost Y down...
            for (int chunkY = 0; chunkY <= topChunkY; chunkY++)
            {
                chunksColumn[chunkY] = capi.World.BlockAccessor.GetChunk(chunkPos.X, chunkY, chunkPos.Y);
                //What to do if chunk is a void? invalid?
            }

            // Prefetch map chunks, in pattern
            IMapChunk[] mapChunks = new IMapChunk[]
            {
            capi.World.BlockAccessor.GetMapChunk(chunkPos.X - 1, chunkPos.Y - 1),
            capi.World.BlockAccessor.GetMapChunk(chunkPos.X - 1, chunkPos.Y),
            capi.World.BlockAccessor.GetMapChunk(chunkPos.X, chunkPos.Y - 1)
            };


            for (int posIndex = 0; posIndex < (chunkSize * chunkSize); posIndex++)
            {
                int mapY = mc.RainHeightMap[posIndex];
                int localChunkY = mapY / chunkSize;
                if (localChunkY >= (chunksColumn.Length)) continue;//Out of range!

                MapUtil.PosInt2d(posIndex, chunkSize, localpos);
                int localX = localpos.X;
                int localZ = localpos.Y;

                float b = 1;
                int leftTop, rightTop, leftBot;

                IMapChunk leftTopMapChunk = mc;
                IMapChunk rightTopMapChunk = mc;
                IMapChunk leftBotMapChunk = mc;

                int topX = localX - 1;
                int botX = localX;
                int leftZ = localZ - 1;
                int rightZ = localZ;

                if (topX < 0 && leftZ < 0)
                {
                    leftTopMapChunk = mapChunks[0];
                    rightTopMapChunk = mapChunks[1];
                    leftBotMapChunk = mapChunks[2];
                }
                else
                {
                    if (topX < 0)
                    {
                        leftTopMapChunk = mapChunks[1];
                        rightTopMapChunk = mapChunks[1];
                    }
                    if (leftZ < 0)
                    {
                        leftTopMapChunk = mapChunks[2];
                        leftBotMapChunk = mapChunks[2];
                    }
                }

                topX = GameMath.Mod(topX, chunkSize);
                leftZ = GameMath.Mod(leftZ, chunkSize);

                leftTop = leftTopMapChunk == null ? 0 : Math.Sign(mapY - leftTopMapChunk.RainHeightMap[leftZ * chunkSize + topX]);
                rightTop = rightTopMapChunk == null ? 0 : Math.Sign(mapY - rightTopMapChunk.RainHeightMap[rightZ * chunkSize + topX]);
                leftBot = leftBotMapChunk == null ? 0 : Math.Sign(mapY - leftBotMapChunk.RainHeightMap[leftZ * chunkSize + botX]);

                float slopeness = (leftTop + rightTop + leftBot);

                if (slopeness > 0) b = 1.2f;
                if (slopeness < 0) b = 0.8f;

                b -= 0.15f; //Slope boost value 

                if (chunksColumn[localChunkY] == null)
                {

                    continue;
                }

                chunksColumn[localChunkY].Unpack();
                int blockId = chunksColumn[localChunkY].Blocks[MapUtil.Index3d(localpos.X, mapY % chunkSize, localpos.Y, chunkSize, chunkSize)];

                Block block = capi.World.Blocks[blockId];

                tmpPos.Set(chunkSize * chunkPos.X + localpos.X, mapY, chunkSize * chunkPos.Y + localpos.Y);

                int avgCol = block.GetColor(capi, tmpPos);
                int rndCol = block.GetRandomColor(capi, tmpPos, BlockFacing.UP);
                //This is still, an abnormal color - the tint is too blue
                int col = ColorUtil.ColorOverlay(avgCol, rndCol, 0.125f);
                var packedFormat = ColorUtil.ColorMultiply3Clamped(col, b);

                Color pixelColor = Color.FromArgb(ColorUtil.ColorB(packedFormat), ColorUtil.ColorG(packedFormat), ColorUtil.ColorR(packedFormat));

                chunkImage.SetPixel(localX, localZ, pixelColor);
            }


            return chunkImage;
        }
    }

    public class UniqueQueue<T> : IEnumerable<T>
    {
        private HashSet<T> hashSet;
        private Queue<T> queue;


        public UniqueQueue()
        {
            hashSet = new HashSet<T>();
            queue = new Queue<T>();
        }


        public int Count
        {
            get
            {
                return hashSet.Count;
            }
        }

        public void Clear()
        {
            hashSet.Clear();
            queue.Clear();
        }


        public bool Contains(T item)
        {
            return hashSet.Contains(item);
        }


        public void Enqueue(T item)
        {
            if (hashSet.Add(item))
            {
                queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            T item = queue.Dequeue();
            hashSet.Remove(item);
            return item;
        }


        public T Peek()
        {
            return queue.Peek();
        }


        public IEnumerator<T> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return queue.GetEnumerator();
        }
    }
}
*/