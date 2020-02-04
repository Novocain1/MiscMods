using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace SwingingDoor
{
    public class LoadCustomModels : ModSystem
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        public Dictionary<AssetLocation, GltfType> gltfs = new Dictionary<AssetLocation, GltfType>();
        public List<IAsset> objs = new List<IAsset>();
        public Dictionary<AssetLocation, MeshData> meshes = new Dictionary<AssetLocation, MeshData>();
        public Dictionary<AssetLocation, TextureAtlasPosition> gltfTextures = new Dictionary<AssetLocation, TextureAtlasPosition>();

        public MeshRenderer testrenderer;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.Event.BlockTexturesLoaded += LoadRawMeshes;

            api.RegisterCommand("testrender", "", "", (p, a) =>
            {
                string l = a.PopWord();
                Vec3d rot = a.PopVec3d() ?? new Vec3d();
                Vec3d scale = a.PopVec3d() ?? new Vec3d(1, 1, 1);

                AssetLocation loc = l != null ? new AssetLocation(l) : new AssetLocation("game:shapes/obj/mesh.obj");
                BlockPos pos = api.World.Player?.CurrentBlockSelection?.Position?.UpCopy();
                if (testrenderer != null)
                {
                    api.Event.UnregisterRenderer(testrenderer, EnumRenderStage.Opaque);
                    testrenderer.Dispose();
                }
                if (pos != null && loc != null)
                {
                    testrenderer = new MeshRenderer(api, api.World.Player.CurrentBlockSelection.Position.UpCopy(), loc,
                        new Vec3f((float)rot.X, (float)rot.Y, (float)rot.Z), 
                        new Vec3f((float)scale.X, (float)scale.Y, (float)scale.Z), out bool failed);
                    if (failed) return;
                    api.Event.RegisterRenderer(testrenderer, EnumRenderStage.Opaque);
                }
            });
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.SaveGameLoaded += LoadRawMeshes;
        }

        private void LoadRawMeshes()
        {
            gltfs = api.World.AssetManager.GetMany<GltfType>(api.World.Logger, "shapes/gltf");
            meshes = api.World.AssetManager.GetMany<MeshData>(api.World.Logger, "shapes/meshdata");
            objs = api.World.AssetManager.GetMany("shapes/obj");
            ConvertGltf();
            ConvertObj();
        }

        private void ConvertGltf()
        {
            foreach (var gltf in gltfs)
            {
                GltfType gltfType = gltf.Value;
                var buffers = gltfType.Buffers;
                var accessors = gltfType.Accessors;
                var bufferViews = gltfType.BufferViews;
                List<Dictionary<string, long>> accvalues = new List<Dictionary<string, long>>();
                Dictionary<string, Queue<byte>> buffdat = new Dictionary<string, Queue<byte>>();

                foreach (var gltfMesh in gltfType.Meshes) foreach (var primitive in gltfMesh.Primitives)
                {
                        Dictionary<string, long> dict = new Dictionary<string, long>();
                        dict.Add("vertex", primitive.Attributes.Position);
                        dict.Add("uv", primitive.Attributes.Texcoord0);
                        dict.Add("normal", primitive.Attributes.Normal);
                        dict.Add("indices", primitive.Indices);
                        dict.Add("material", primitive.Material);
                        accvalues.Add(dict);
                }

                if (gltfType.Materials != null)
                {
                    foreach (var mat in gltfType.Materials)
                    {
                        if (mat?.PbrMetallicRoughness?.BaseColorTexture?.Index == null) continue;

                        Dictionary<string, long> dict = new Dictionary<string, long>();
                        dict.Add("basecolor", gltfType.Images[mat.PbrMetallicRoughness.BaseColorTexture.Index].BufferView);
                        accvalues.Add(dict);
                    }
                }

                foreach (var dict in accvalues) foreach (var acc in dict)
                {
                        Queue<byte> bytes;
                        if (!buffdat.TryGetValue(acc.Key, out bytes)) buffdat.Add(acc.Key, new Queue<byte>()); bytes = buffdat[acc.Key];
                        var bufferview = bufferViews[acc.Value];
                        var buffer = buffers[bufferview.Buffer];
                        byte[] bufferdat = Convert.FromBase64String(buffer.Uri.Replace("data:application/octet-stream;base64,", "")).SubArray(bufferview.ByteOffset, bufferview.ByteLength);
                        for (int i = 0; i < bufferdat.Length; i++) bytes.Enqueue(bufferdat[i]);
                }

                MeshData mesh = new MeshData(1, 1);

                //positions
                byte[] posbytes = buffdat["vertex"].ToArray();
                Queue<Vec3f> positions = ToVec3fs(posbytes);

                //uvs
                byte[] uvbytes = buffdat["uv"].ToArray();
                Queue<Vec2f> uv = ToVec2fs(uvbytes);

                //normals
                byte[] nrmbytes = buffdat["normal"].ToArray();
                Queue<Vec3f> normals = ToVec3fs(nrmbytes);

                //indices
                byte[] indbytes = buffdat["indices"].ToArray();
                Queue<int> indices = ToInts(indbytes);

                //material
                byte[] matbytes = buffdat["material"].ToArray();
                Queue<Vec3f> material = ToVec3fs(matbytes);

                ApplyQueues(normals, positions, uv, indices, ref mesh);

                //texture
                if (capi != null && buffdat.ContainsKey("basecolor"))
                {
                    byte[] texbytes = buffdat["basecolor"]?.ToArray();
                    BitmapExternal bitmap = capi.Render.BitmapCreateFromPng(texbytes);
                    capi.BlockTextureAtlas.InsertTexture(bitmap, out int id, out TextureAtlasPosition position);
                    Size2i size = capi.BlockTextureAtlas.Size;
                    gltfTextures.Add(gltf.Key, position);
                }

                meshes.Add(gltf.Key, mesh);
            }

        }

        public MeshData GetWithTexPos(AssetLocation assetLocation, TextureAtlasPosition pos)
        {
            if (meshes.TryGetValue(assetLocation, out MeshData mesh))
            {
                return mesh.WithTexPos(pos);
            }
            return null;
        }

        public Queue<Vec3f> ToVec3fs(byte[] bytes) => ToVec3fs(ToFloats(bytes));
        public Queue<Vec2f> ToVec2fs(byte[] bytes) => ToVec2fs(ToFloats(bytes));
        public Queue<int> ToInts(byte[] bytes) => ToInts(ToShorts(bytes));

        public Queue<Vec3f> ToVec3fs(Queue<float> floats)
        {
            Queue<Vec3f> vecs = new Queue<Vec3f>();
            for (int i = floats.Count; i > 0 && floats.Count > 1; i--)
            {
                vecs.Enqueue(new Vec3f(floats.Dequeue(), floats.Dequeue(), floats.Dequeue()));
            }
            return vecs;
        }

        public Queue<Vec2f> ToVec2fs(Queue<float> floats)
        {
            Queue<Vec2f> vecs = new Queue<Vec2f>();
            for (int i = floats.Count; i > 0 && floats.Count > 0; i--)
            {
                vecs.Enqueue(new Vec2f(floats.Dequeue(), floats.Dequeue()));
            }
            return vecs;
        }

        public Queue<int> ToInts(Queue<ushort> shorts)
        {
            Queue<int> ints = new Queue<int>();
            for (int i = shorts.Count; i > 0 && shorts.Count > 0; i--)
            {
                ints.Enqueue(shorts.Dequeue());
            }
            return ints;
        }

        public Queue<float> ToFloats(byte[] bytes)
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

        public Queue<ushort> ToShorts(byte[] bytes)
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

        public void ApplyQueues(Queue<Vec3f> normals, Queue<Vec3f> vertices, Queue<Vec2f> vertexUvs, Queue<int> vertexIndices, ref MeshData mesh)
        {
            Queue<int> packedNormals = new Queue<int>();
            Queue<float> packedUVs = new Queue<float>();
            for (int i = normals.Count; i > 0; i--)
            {
                Vec3f nrm = normals.Dequeue();
                packedNormals.Enqueue(VertexFlags.NormalToPackedInt(nrm.X, nrm.Y, nrm.Z) << 15);
            }

            for (int i = vertexUvs.Count; i > 0; i--)
            {
                Vec2f uv = vertexUvs.Dequeue();
                packedUVs.Enqueue(uv.X);
                packedUVs.Enqueue(uv.Y);
            }

            for (int i = vertices.Count; i > 0; i--)
            {
                Vec3f vec = vertices.Dequeue();
                mesh.AddVertexWithFlags(vec.X, vec.Y, vec.Z, 0, 0, ColorUtil.WhiteArgb, 0, 0);
            }

            for (int i = vertexIndices.Count; i > 0; i--)
            {
                mesh.AddIndex(vertexIndices.Dequeue());
            }

            mesh.Flags = packedNormals.ToArray();
            mesh.Uv = packedUVs.ToArray();
        }

        private void ConvertObj()
        {
            foreach (var val in objs)
            {
                if (val.Location.ToString().Contains(".obj"))
                {
                    MeshData mesh = new MeshData(1, 1);
                    Queue<Vec3f> normals = new Queue<Vec3f>();
                    Queue<Vec3f> vertices = new Queue<Vec3f>();
                    Queue<Vec2f> vertexUvs = new Queue<Vec2f>();
                    Queue<int> vertexIndices = new Queue<int>();
                    var lines = val.ToText().Split('\n');
                    foreach (var str in lines)
                    {
                        if (str.StartsWith("v "))
                        {
                            var n = str.Split(' ');
                            vertices.Enqueue(new Vec3f(float.Parse(n[1]), float.Parse(n[2]), float.Parse(n[3])));
                        }
                        else if (str.StartsWith("vn "))
                        {
                            var n = str.Split(' ');
                            normals.Enqueue(new Vec3f(float.Parse(n[1]), float.Parse(n[2]), float.Parse(n[3])));
                        }
                        else if (str.StartsWith("vt "))
                        {
                            var n = str.Split(' ');
                            vertexUvs.Enqueue(new Vec2f(float.Parse(n[1]) - 1, float.Parse(n[2]) - 1));
                        }
                        else if (str.StartsWith("f "))
                        {
                            var n = str.Split(' ');

                            for (int i = 1; i < n.Length; i++)
                            {
                                var ind = n[i].Split('/');
                                if (int.TryParse(ind[0], out int vI))
                                {
                                    vertexIndices.Enqueue(int.Parse(ind[0]));
                                }
                                else
                                {
                                    ind = n[i].Replace("//", " ").Split(' ');
                                    if (!ind[0].Contains('\r'))
                                    {
                                        vertexIndices.Enqueue(int.Parse(ind[0]));
                                    }
                                }
                            }
                        }
                    }
                    ApplyQueues(normals, vertices, vertexUvs, vertexIndices, ref mesh);
                    meshes.Add(val.Location, mesh);
                }
            }
        }
    }
}