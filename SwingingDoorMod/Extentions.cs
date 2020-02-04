using System;
using Vintagestory.API.Client;

namespace SwingingDoor
{
    public static class Extentions
    {
        public static void SetUv(this MeshData mesh, TextureAtlasPosition texPos) => mesh.SetUv(new float[] { texPos.x1, texPos.y1, texPos.x2, texPos.y1, texPos.x2, texPos.y2, texPos.x1, texPos.y2 });

        public static T[] SubArray<T>(this T[] data, long index, long length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static MeshData WithTexPos(this MeshData mesh, TextureAtlasPosition texPos)
        {
            MeshData meshClone = mesh.Clone();
            for (int i = 0; i < meshClone.Uv.Length; i++)
            {
                float x = texPos.x2 - texPos.x1;
                float y = texPos.y2 - texPos.y1;

                meshClone.Uv[i] = i % 2 == 0 ? (meshClone.Uv[i] * x) + texPos.x1 : (meshClone.Uv[i] * y) + texPos.y1;
            }
            return meshClone;
        }
    }
}