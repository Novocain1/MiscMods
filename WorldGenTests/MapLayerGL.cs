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

        public int[] GenLayer(float xCoord, float zCoord, int iteration)
        {
            ServerGL.xCoord = xCoord;
            ServerGL.yCoord = zCoord;
            ServerGL.zCoord = (seed + iteration / 9.0f) % 256.0f;
            ServerGL.snap = true;

            while (ServerGL.snap) ; ;

            return ServerGL.pixels;
        }
    }
}