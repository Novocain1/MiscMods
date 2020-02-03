using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using System.IO;
using Vintagestory.API.Util;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace SwingingDoor
{
    class SwingingDoorMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockSwingingDoor", typeof(BlockSwingingDoor));
            api.RegisterBlockEntityClass("SwingingDoor", typeof(BlockEntitySwingingDoor));
        }
    }

    class BlockSwingingDoor : Block
    {
        Block TopBlock { get => api.World.BlockAccessor.GetBlock(CodeWithVariant("ud", "top")); }
        Block BottomBlock { get => api.World.BlockAccessor.GetBlock(CodeWithVariant("ud", "bottom")); }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos pos = blockSel?.Position;
            if (pos != null && !world.BlockAccessor.GetBlock(pos.UpCopy()).IsReplacableBy(this)) return false;

            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            if (Code.ToString() == BottomBlock.Code.ToString())
            {
                (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntitySwingingDoor)?.OnBlockInteract(world, byPlayer);
                return true;
            }
            else if (Code.ToString() == TopBlock.Code.ToString())
            {
                (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) as BlockEntitySwingingDoor)?.OnBlockInteract(world, byPlayer);
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (world.Side.IsServer())
            {
                if (Code.ToString() == BottomBlock.Code.ToString())
                {
                    world.BlockAccessor.SetBlock(TopBlock.Id, blockPos.UpCopy());
                }
            }
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            base.OnBlockRemoved(world, pos);
            if (Code.ToString() == BottomBlock.Code.ToString())
            {
                world.BlockAccessor.SetBlock(0, pos.UpCopy());
            }
            else if (Code.ToString() == TopBlock.Code.ToString())
            {
                world.BlockAccessor.SetBlock(0, pos.DownCopy());
            }
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
        {
            if (Variant["ud"] == "top")
            {
                var textures = capi.World.BlockAccessor.GetBlock(pos.DownCopy()).Textures;
                if (textures == null || textures.Count == 0) return 0;
                if (!textures.TryGetValue(facing.Code, out CompositeTexture tex))
                {
                    tex = textures.First().Value;
                }
                if (tex?.Baked == null) return 0;

                int color = capi.BlockTextureAtlas.GetRandomColor(tex.Baked.TextureSubId);

                if (TintIndex > 0)
                {
                    color = capi.ApplyColorTintOnRgba(TintIndex, color, pos.X, pos.Y, pos.Z);
                }

                return color;
            }

            return base.GetRandomColor(capi, pos, facing);
        }

        public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
        {
            return base.GetRandomColor(capi, stack);
        }
    }

    class BlockEntitySwingingDoor : BlockEntity
    {
        Block OwnBlock { get; set; }
        BlockEntityAnimationUtil Util { get; set; }
        string AnimKey { get; set; }
        int AnimCount { get => Util.activeAnimationsByAnimCode.Count; }
        bool IsClosed { get => OwnBlock.Variant["state"] == "closed"; }
        ICoreAPI api { get => Api; }
        BlockPos pos { get => Pos; }
        Block OpenState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "open")); }
        Block ClosedState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "closed")); }
        Block BetweenState { get => api.World.BlockAccessor.GetBlock(OwnBlock.CodeWithVariant("state", "between")); }

        Block OpenTop { get => api.World.BlockAccessor.GetBlock(OpenState.CodeWithVariant("ud", "top")); }
        Block ClosedTop { get => api.World.BlockAccessor.GetBlock(ClosedState.CodeWithVariant("ud", "top")); }
        Block BetweenTop { get => api.World.BlockAccessor.GetBlock(BetweenState.CodeWithVariant("ud", "top")); }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            OwnBlock = api.World.BlockAccessor.GetBlock(pos);

            if (api.Side.IsClient() && OwnBlock.Id != 0)
            {
                Util = new BlockEntityAnimationUtil(api, this);
                AnimKey = OwnBlock.Attributes["animKey"].AsString("anim");
                Util.InitializeAnimator(OwnBlock.Code.ToString(), new Vec3f(OwnBlock.Shape.rotateX, OwnBlock.Shape.rotateY, OwnBlock.Shape.rotateZ));
            }
        }

        public void OnBlockInteract(IWorldAccessor world, IPlayer byPlayer)
        {
            float speed = OwnBlock.Attributes["animSpeed"].AsFloat(1);
            RegisterDelayedCallback(dt =>
            {
                if (IsClosed)
                {
                    world.BlockAccessor.ExchangeBlock(OpenState.BlockId, pos);
                    world.BlockAccessor.ExchangeBlock(OpenTop.BlockId, pos.UpCopy());
                }
                else
                {
                    world.BlockAccessor.ExchangeBlock(ClosedState.BlockId, pos);
                    world.BlockAccessor.ExchangeBlock(ClosedTop.BlockId, pos.UpCopy());
                }

                if (world.Side.IsServer()) world.PlaySoundAt(new AssetLocation("sounds/" + OwnBlock.Attributes["closeSound"].AsString()), byPlayer);
                Util?.StopAnimation(AnimKey);
                Initialize(api);
            }, (int)Math.Round((OwnBlock.Attributes["animLength"].AsInt(30) * 31) / speed));

            if (world.Side.IsClient())
            {
                AnimationMetaData data = new AnimationMetaData { Animation = AnimKey, Code = AnimKey, AnimationSpeed = speed };
                Util.StartAnimation(data);
            }
            else
            {
                world.PlaySoundAt(new AssetLocation("sounds/" + OwnBlock.Attributes["openSound"].AsString()), pos.X, pos.Y, pos.Z);
            }

            world.BlockAccessor.ExchangeBlock(BetweenState.BlockId, pos);
            world.BlockAccessor.ExchangeBlock(BetweenTop.BlockId, pos.UpCopy());
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            Util?.StopAnimation(AnimKey);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) => AnimCount > 0;
    }

    static class ComponentMap
    {
        public const int SCALAR = 1, VEC2 = 2, VEC3 = 3, VEC4 = 4, MAT2 = 4, MAT3 = 9, MAT4 = 16;
    }

    public class LoadCustomModels : ModSystem
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        public Dictionary<AssetLocation, GltfType> gltfs = new Dictionary<AssetLocation, GltfType>();
        public List<IAsset> objs = new List<IAsset>();
        public Dictionary<AssetLocation, MeshData> meshes = new Dictionary<AssetLocation, MeshData>();
        public Dictionary<AssetLocation, MeshRef> meshrefs = new Dictionary<AssetLocation, MeshRef>();

        public MeshRenderer testrenderer;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.Event.BlockTexturesLoaded += LoadRawMeshes;
            api.Event.LeaveWorld += () =>
            {
                foreach (var val in meshrefs)
                {
                    if (!val.Value.Disposed) val.Value.Dispose();
                }
                meshrefs.Clear();
            };

            api.RegisterCommand("testrender", "", "", (p, a) =>
            {
                string l = a.PopWord();
                AssetLocation loc = l != null ? new AssetLocation(l) : new AssetLocation("game:shapes/obj/mesh.obj");
                BlockPos pos = api.World.Player?.CurrentBlockSelection?.Position?.UpCopy();
                if (testrenderer != null)
                {
                    api.Event.UnregisterRenderer(testrenderer, EnumRenderStage.Opaque);
                    testrenderer.Dispose();
                }
                if (pos != null && loc != null)
                {
                    testrenderer = new MeshRenderer(api, api.World.Player.CurrentBlockSelection.Position.UpCopy(), loc, new Vec3f(), out bool failed);
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
            if (capi != null) BuildMeshRefs();
        }

        private void BuildMeshRefs()
        {
            foreach (var val in meshes)
            {
                meshrefs.Add(val.Key, capi.Render.UploadMesh(val.Value));
            }
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

                foreach (var mat in gltfType.Materials)
                {
                    if (mat?.PbrMetallicRoughness?.BaseColorTexture?.Index == null) continue;

                    Dictionary<string, long> dict = new Dictionary<string, long>();
                    dict.Add("basecolor", mat.PbrMetallicRoughness.BaseColorTexture.Index);
                    accvalues.Add(dict);
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
                byte[] posbytes = buffdat["vertex "].ToArray();
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
                byte[] texbytes = buffdat["basecolor"].ToArray();
                //convert it from png and store it somewhere

                meshes.Add(gltf.Key, mesh);
            }

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

        public void ApplyQueues(Queue<Vec3f> normals, Queue<Vec3f> vertices, Queue<Vec2f> vertexUvs, Queue<int> vertexIndices, ref MeshData mesh, int offset = 0)
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
                mesh.AddIndex(vertexIndices.Dequeue() - offset);
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
                            vertexUvs.Enqueue(new Vec2f(float.Parse(n[1]), float.Parse(n[2])));
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
                    ApplyQueues(normals, vertices, vertexUvs, vertexIndices, ref mesh, 1);
                    meshes.Add(val.Location, mesh);
                }
            }
        }
    }
    public static class Extentions
    {
        public static void SetUv(this MeshData mesh, TextureAtlasPosition texPos) => mesh.SetUv(new float[] { texPos.x1, texPos.y1, texPos.x2, texPos.y1, texPos.x2, texPos.y2, texPos.x1, texPos.y2 });

        public static T[] SubArray<T>(this T[] data, long index, long length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }

    public class AssetExtends
    {
        public static AssetCategory gltf = new AssetCategory("gltf", false, EnumAppSide.Universal);
        public static AssetCategory obj = new AssetCategory("obj", false, EnumAppSide.Universal);
        public static AssetCategory mtl = new AssetCategory("mtl", false, EnumAppSide.Universal);
        public static AssetCategory ply = new AssetCategory("ply", false, EnumAppSide.Universal);
        public static AssetCategory meshdata = new AssetCategory("meshdata", false, EnumAppSide.Universal);
    }

    public class MeshRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        private MeshRef meshRef;
        public Matrixf ModelMat = new Matrixf();
        public LoadCustomModels models { get => capi.ModLoader.GetModSystem<LoadCustomModels>(); }
        public bool shouldRender;
        Vec3f rotation;

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, AssetLocation location, Vec3f rotation, out bool failed)
        {
            failed = false;
            try
            {
                this.capi = capi;
                this.pos = pos;
                this.rotation = rotation;
                this.meshRef = models.meshrefs[location];
            }
            catch (Exception)
            {
                failed = true;
                Dispose();
            }
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRef == null) return;
            IRenderAPI render = capi.Render;
            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
            //render.GlDisableCullFace();
            render.GlToggleBlend(true, EnumBlendMode.Standard);
            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = capi.Render.GetOrLoadTexture(new AssetLocation("gltf/boat.png"));

            prog.ModelMatrix = ModelMat.Identity()
                .Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z)
                .Translate(0.5, 0.5, 0.5)
                .RotateDeg(rotation)
                //.RotateZDeg(dRot += deltaTime * 512)
                .Values;
            prog.ViewMatrix = render.CameraMatrixOriginf;
            prog.ProjectionMatrix = render.CurrentProjectionMatrix;

            capi.Render.RenderMesh(meshRef);
            prog.Stop();
        }
    }
}