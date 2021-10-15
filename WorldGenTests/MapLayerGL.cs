using System;
using Vintagestory.ServerMods;

namespace WorldGenTests
{
    public class MapLayerGL
    {
        long seed;

        public MapLayerGL(long seed)
        {
            this.seed = seed;
        }

        public int[] GenLayer(float xCoord, float zCoord, int i)
        {
            ServerGL.xCoord = xCoord;
            ServerGL.yCoord = zCoord;
            ServerGL.seed = seed + i * 512;

            ServerGL.ReadPtr();

            return ServerGL.pixels;
        }
    }
}