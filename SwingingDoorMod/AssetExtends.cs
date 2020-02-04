using Vintagestory.API.Common;

namespace SwingingDoor
{
    public class AssetExtends
    {
        public static AssetCategory gltf = new AssetCategory("gltf", false, EnumAppSide.Universal);
        public static AssetCategory obj = new AssetCategory("obj", false, EnumAppSide.Universal);
        public static AssetCategory mtl = new AssetCategory("mtl", false, EnumAppSide.Universal);
        public static AssetCategory ply = new AssetCategory("ply", false, EnumAppSide.Universal);
        public static AssetCategory meshdata = new AssetCategory("meshdata", false, EnumAppSide.Universal);
    }
}