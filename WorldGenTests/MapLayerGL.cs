using Vintagestory.ServerMods;

namespace WorldGenTests
{
    public class MapLayerGL : MapLayerBase
    {
        long seed;
        int iteration;

        public MapLayerGL(long seed, int iteration) : base(seed)
        {
            this.seed = seed;
            this.iteration = iteration;
        }

        public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            ServerGL.xCoord = xCoord;
            ServerGL.yCoord = zCoord;
            ServerGL.zCoord = (float)seed + iteration * 1000;
            ServerGL.snap = true;

            while (ServerGL.snap) ; ;

            return ServerGL.pixels;
        }
    }
}