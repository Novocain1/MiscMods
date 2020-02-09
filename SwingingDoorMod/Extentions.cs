using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

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

        public static Queue<Vec3f> ToVec3fs(this byte[] bytes) => ToVec3fs(ToFloats(bytes));
        public static Queue<Vec2f> ToVec2fs(this byte[] bytes) => ToVec2fs(ToFloats(bytes));
        public static Queue<int> ToInts(this byte[] bytes) => ToInts(ToShorts(bytes));

        public static Queue<Vec3f> ToVec3fs(this Queue<float> floats)
        {
            Queue<Vec3f> vecs = new Queue<Vec3f>();
            for (int i = floats.Count; i > 0 && floats.Count > 1; i--)
            {
                vecs.Enqueue(new Vec3f(floats.Dequeue(), floats.Dequeue(), floats.Dequeue()));
            }
            return vecs;
        }

        public static Queue<Vec2f> ToVec2fs(this Queue<float> floats)
        {
            Queue<Vec2f> vecs = new Queue<Vec2f>();
            for (int i = floats.Count; i > 0 && floats.Count > 0; i--)
            {
                vecs.Enqueue(new Vec2f(floats.Dequeue(), floats.Dequeue()));
            }
            return vecs;
        }

        public static Queue<int> ToInts(this Queue<ushort> shorts)
        {
            Queue<int> ints = new Queue<int>();
            for (int i = shorts.Count; i > 0 && shorts.Count > 0; i--)
            {
                ints.Enqueue(shorts.Dequeue());
            }
            return ints;
        }

        public static Queue<float> ToFloats(this byte[] bytes)
        {
            Queue<float> queue = new Queue<float>();

            for (int i = 0; i < bytes.Length; i += sizeof(float))
            {
                Queue<byte> postrim = new Queue<byte>();
                for (int j = i; j < i + sizeof(float); j++)
                {
                    postrim.Enqueue(bytes[j]);
                }
                float pos = BitConverter.ToSingle(postrim.ToArray(), 0);
                queue.Enqueue(pos);
            }
            return queue;
        }

        public static Queue<ushort> ToShorts(this byte[] bytes)
        {
            Queue<ushort> queue = new Queue<ushort>();

            for (int i = 0; i < bytes.Length; i += sizeof(ushort))
            {
                Queue<byte> postrim = new Queue<byte>();
                for (int j = i; j < i + sizeof(ushort); j++)
                {
                    postrim.Enqueue(bytes[j]);
                }
                ushort pos = BitConverter.ToUInt16(postrim.ToArray(), 0);
                queue.Enqueue(pos);
            }
            return queue;
        }
    }
}