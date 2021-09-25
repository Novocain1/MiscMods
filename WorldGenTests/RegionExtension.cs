using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace WorldGenTests
{
    public static class RegionExtension
    {
        public static T GetModdata<T>(this IMapRegion mapRegion, string key)
        {
            return SerializerUtil.Deserialize<T>(mapRegion.GetModdata(key));
        }

        public static void SetModdata<T>(this IMapRegion mapRegion, string key, T data)
        {
            mapRegion.SetModdata(key, SerializerUtil.Serialize(data));
        }
    }
}