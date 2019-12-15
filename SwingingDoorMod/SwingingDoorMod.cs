using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class System_glTF : ModSystem
    {
        ICoreAPI api;
        public Dictionary<AssetLocation, JObject> gltfs = new Dictionary<AssetLocation, JObject>();
        public List<IAsset> objs = new List<IAsset>();
        public Dictionary<AssetLocation, MeshData> meshes = new Dictionary<AssetLocation, MeshData>();

        public MeshRenderer testrenderer;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.BlockTexturesLoaded += LoadRawMeshes;
            api.RegisterCommand("testrender", "", "", (p, a) =>
            {
                string l = a.PopWord();
                AssetLocation loc = l != null ? new AssetLocation(l) : null;
                BlockPos pos = api.World.Player?.CurrentBlockSelection?.Position?.UpCopy();
                if (testrenderer != null)
                {
                    api.Event.UnregisterRenderer(testrenderer, EnumRenderStage.Opaque);
                    testrenderer.Dispose();
                }
                if (pos != null && loc != null && meshes.TryGetValue(loc, out MeshData mesh))
                {
                    testrenderer = new MeshRenderer(api, api.World.Player.CurrentBlockSelection.Position.UpCopy(), mesh, new Vec3f());
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
            gltfs = api.World.AssetManager.GetMany<JObject>(api.World.Logger, "shapes/gltf");
            meshes = api.World.AssetManager.GetMany<MeshData>(api.World.Logger, "shapes/meshdata");
            objs = api.World.AssetManager.GetMany("shapes/obj");
            ConvertObj();
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
                        Vec3f vec = vertices.Count > 0 ? vertices.Dequeue() : new Vec3f();
                        mesh.AddVertexWithFlags(vec.X, vec.Y, vec.Z, 0, 0, ColorUtil.WhiteArgb, 0, 0);
                    }

                    for (int i = vertexIndices.Count; i > 0; i--)
                    {
                        mesh.AddIndex(vertexIndices.Dequeue() - 1);
                    }

                    mesh.Flags = packedNormals.ToArray();
                    mesh.Uv = packedUVs.ToArray();
                    meshes.Add(val.Location, mesh);
                }
            }
        }
    }
    public static class Extentions
    {
        public static void SetUv(this MeshData mesh, TextureAtlasPosition texPos) => mesh.SetUv(new float[] { texPos.x1, texPos.y1, texPos.x2, texPos.y1, texPos.x2, texPos.y2, texPos.x1, texPos.y2 });
    }

    public class AssetExtends
    {
        public static AssetCategory gltf = new AssetCategory("gltf", false, EnumAppSide.Universal);
        public static AssetCategory obj = new AssetCategory("obj", false, EnumAppSide.Universal);
        public static AssetCategory mtl = new AssetCategory("mtl", false, EnumAppSide.Universal);
        public static AssetCategory meshdata = new AssetCategory("meshdata", false, EnumAppSide.Universal);

    }

    public class MeshRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private BlockPos pos;
        private MeshRef meshRef;
        public Matrixf ModelMat = new Matrixf();
        public bool shouldRender;
        Vec3f rotation;
        float dRot;

        public MeshRenderer(ICoreClientAPI capi, BlockPos pos, MeshData meshData, Vec3f rotation)
        {
            this.capi = capi;
            this.pos = pos;
            this.rotation = rotation;
            this.meshRef = capi.Render.UploadMesh(meshData);
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            meshRef.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRef == null) return;
            IRenderAPI render = capi.Render;
            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
            //render.GlDisableCullFace();
            render.GlToggleBlend(true, EnumBlendMode.Standard);
            IStandardShaderProgram prog = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = capi.Render.GetOrLoadTexture(new AssetLocation("block/wood/debarked/oak.png"));

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