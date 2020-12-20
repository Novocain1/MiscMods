using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace RandomTests
{
    [HarmonyPatch(typeof(ItemAxe), "OnBlockBrokenWith")]
    class FellTree
    {
        public static bool Prefix()
        {
            return false;
        }

        public static void Postfix(ItemAxe __instance, ref bool __result, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier)
        {
            float fallTime = 2.0f;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            double windspeed = world.Api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0;


            string treeType;
            var test = world.BlockAccessor.GetBlock(blockSel.Position);
            Stack<BlockPos> foundPositions = __instance.FindTree(world, blockSel.Position, out treeType);

            Block leavesBranchyBlock = world.GetBlock(new AssetLocation("leavesbranchy-grown-" + treeType));
            Block leavesBlock = world.GetBlock(new AssetLocation("leaves-grown-" + treeType));


            if (foundPositions.Count == 0)
            {
                __result = new Item().OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
                return;
            }

            bool damageable = __instance.DamagedBy != null && __instance.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);

            float leavesMul = 1;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;

            Stack<KeyValuePair<Block, float>> blocks = new Stack<KeyValuePair<Block, float>>();
            BlockPos originPos = blockSel.Position;
            Stack<BlockPos> breakable = new Stack<BlockPos>();

            while (foundPositions.Count > 0)
            {
                BlockPos pos = foundPositions.Pop();
                breakable.Push(pos);
                blocksbroken++;

                Block block = world.BlockAccessor.GetBlock(pos);

                bool isLog = block.Code.Path.StartsWith("beehive-inlog-" + treeType) || block.Code.Path.StartsWith("log-resinharvested-" + treeType) || block.Code.Path.StartsWith("log-resin-" + treeType) || block.Code.Path.StartsWith("log-grown-" + treeType) || block.Code.Path.StartsWith("bamboo-grown-brown-segment") || block.Code.Path.StartsWith("bamboo-grown-green-segment");
                bool isBranchy = block == leavesBranchyBlock;
                bool isLeaves = block == leavesBlock || block.Code.Path == "bambooleaves-grown";


                blocks.Push(new KeyValuePair<Block, float>(block, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1)));
                //world.BlockAccessor.SetBlock(0, pos);
                //world.BlockAccessor.BreakBlock(pos, byPlayer, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1));

                if (world.Side == EnumAppSide.Client)
                {
                    var dustParticles = __instance.GetField<SimpleParticleProperties>("dustParticles");
                    dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
                    dustParticles.Color |= 255 << 24;
                    dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);

                    if (block.BlockMaterial == EnumBlockMaterial.Leaves)
                    {
                        dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
                        dustParticles.ParticleModel = EnumParticleModel.Quad;
                        dustParticles.MinVelocity.Set(-0.4f + 4 * (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + 4 * (float)windspeed, 1.2f, 0.8f);

                    }
                    else
                    {
                        dustParticles.GravityEffect = 0.8f;
                        dustParticles.ParticleModel = EnumParticleModel.Cube;
                        dustParticles.MinVelocity.Set(-0.4f + (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + (float)windspeed, 1.2f, 0.8f);
                    }


                    world.SpawnParticles(dustParticles);
                }


                if (damageable && isLog)
                {
                    __instance.DamageItem(world, byEntity, itemslot);
                }

                if (itemslot.Itemstack == null) {
                    __result = true;
                    return;
                }

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }
            
            var indexable = breakable.ToArray();

            if (world.Side.IsClient())
            {
                MeshData logMesh = null;
                MeshData leavesMesh = null;

                var capi = world.Api as ICoreClientAPI;

                foreach (var pos in indexable)
                {
                    var tesselatormanager = capi.TesselatorManager as Vintagestory.Client.NoObf.ShapeTesselatorManager;

                    Block block = world.BlockAccessor.GetBlock(pos);

                    bool isLog = block.Code.Path.StartsWith("beehive-inlog-" + treeType) || block.Code.Path.StartsWith("log-resinharvested-" + treeType) || block.Code.Path.StartsWith("log-resin-" + treeType) || block.Code.Path.StartsWith("log-grown-" + treeType) || block.Code.Path.StartsWith("bamboo-grown-brown-segment") || block.Code.Path.StartsWith("bamboo-grown-green-segment");
                    bool isBranchy = block == leavesBranchyBlock;
                    bool isLeaves = block == leavesBlock || block.Code.Path == "bambooleaves-grown";

                    capi.Tesselator.TesselateBlock(block, out MeshData addMesh);

                    var lod0 = tesselatormanager.blockModelDatasLod0.ContainsKey(block.Id) ? tesselatormanager.blockModelDatasLod0[block.Id] : null;
                    var lod1 = tesselatormanager.blockModelDatas[block.Id].Clone();

                    var lod0alt = tesselatormanager.altblockModelDatasLod0[block.Id];
                    var lod1alt = tesselatormanager.altblockModelDatasLod1[block.Id];

                    if (block.HasAlternates && lod1alt != null)
                    {
                        long alternateIndex = block.RandomizeAxes == EnumRandomizeAxes.XYZ ? GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, lod1alt.Length) : GameMath.MurmurHash3Mod(pos.X, 0, pos.Z, lod1alt.Length);
                        addMesh = lod1alt[alternateIndex].Clone();
                        var lod = lod0alt?[alternateIndex].Clone();

                        if (lod != null && addMesh != lod)
                        {
                            addMesh.IndicesMax = addMesh.Indices.Count();
                            lod.IndicesMax = lod.Indices.Count();

                            addMesh.AddMeshData(lod);
                            addMesh.CompactBuffers();
                        }
                    }
                    else
                    {
                        addMesh = lod1;
                        addMesh.IndicesMax = addMesh.Indices.Count();
                        if (lod0 != null)
                        {
                            lod0.IndicesMax = lod0.Indices.Count();
                            if (addMesh != lod0) addMesh.AddMeshData(lod0);
                        }
                    }


                    if (isLog)
                    {
                        if (logMesh == null)
                        {
                            capi.Tesselator.TesselateBlock(block, out logMesh);
                        }
                        else
                        {
                            addMesh.Translate(pos.SubCopy(originPos).ToVec3f());
                            logMesh.AddMeshData(addMesh);
                        }
                    }

                    if (isLeaves || isBranchy)
                    {
                        var worldmap = (capi.World as ClientMain).GetField<ClientWorldMap>("WorldMap");
                        ColorMapData colormap = worldmap.getColorMapData(block, pos.X, pos.Y, pos.Z);

                        if (leavesMesh == null)
                        {
                            capi.Tesselator.TesselateBlock(block, out leavesMesh);
                            if (logMesh != null) leavesMesh.Translate(pos.SubCopy(originPos).ToVec3f());
                            leavesMesh.CustomInts = new CustomMeshDataPartInt()
                            {
                                InterleaveOffsets = new int[] { 1 },
                                InterleaveSizes = new int[] { 1 },
                                InterleaveStride = 4,
                                Conversion = DataConversion.Integer
                            };

                            for (int i = 0; i < leavesMesh.VerticesCount; i++)
                            {
                                leavesMesh.CustomInts.Add(colormap.Value);
                            }
                        }
                        else
                        {
                            addMesh.Translate(pos.SubCopy(originPos).ToVec3f());

                            addMesh.CustomInts = new CustomMeshDataPartInt()
                            {
                                InterleaveOffsets = new int[leavesMesh.VerticesCount].Fill(1),
                                InterleaveSizes = new int[leavesMesh.VerticesCount].Fill(1),
                                InterleaveStride = 4,
                                Conversion = DataConversion.Integer
                            };

                            for (int i = 0; i < addMesh.VerticesCount; i++)
                            {
                                addMesh.CustomInts.Add(colormap.Value);
                            }

                            leavesMesh.AddMeshData(addMesh);
                        }
                    }

                    world.BlockAccessor.SetBlock(0, pos);
                }

                logMesh?.CompactBuffers();
                leavesMesh?.WithColorMaps();
                leavesMesh?.CompactBuffers();

                var chopFace = blockSel.Face;

                var fallDirection = chopFace.IsHorizontal ? chopFace.Opposite : BlockFacing.HORIZONTALS_ANGLEORDER[capi.World.Rand.Next(0, 3)];

                if (logMesh != null)
                {
                    new FallingTree(world.Api as ICoreClientAPI, originPos, false, logMesh, fallTime, fallDirection, EnumRenderStage.Opaque);
                    new FallingTree(world.Api as ICoreClientAPI, originPos, false, logMesh, fallTime, fallDirection, EnumRenderStage.ShadowNear);
                    new FallingTree(world.Api as ICoreClientAPI, originPos, false, logMesh, fallTime, fallDirection, EnumRenderStage.ShadowFar);
                }

                if (leavesMesh != null)
                {
                    new FallingTree(world.Api as ICoreClientAPI, originPos, true, leavesMesh, fallTime, fallDirection, EnumRenderStage.Opaque);
                    new FallingTree(world.Api as ICoreClientAPI, originPos, true, leavesMesh, fallTime, fallDirection, EnumRenderStage.ShadowNear);
                    new FallingTree(world.Api as ICoreClientAPI, originPos, true, leavesMesh, fallTime, fallDirection, EnumRenderStage.ShadowFar);
                }
            }

            world.RegisterCallback((dt) => {
                int i = 0;
                foreach (var toBreak in blocks)
                {
                    toBreak.Key.OnBlockBroken(world, indexable[i], byPlayer, toBreak.Value);
                    i++;
                }

            }, (int)(fallTime * 1000.0f));

            if (blocksbroken > 35)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                world.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }

            __result = true;
        }
    }

    public class Class1 : ModSystem
    {
        public const string PatchCode = "RandomTests.Modsystem.FallingTree";
        public Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony(PatchCode);
            harmony.PatchAll();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () =>
            {
                api.Shader.ReloadShaders();
            };
        }
    }

    public class FallingTree : IRenderer
    {
        public ICoreClientAPI capi;
        public MeshRef treeMesh;
        public Matrixf ModelMat = new Matrixf();
        public BlockPos pos;
        public BlockFacing fallDirection;
        public float startFallTime;
        public float fallTime;
        public bool isLeaves;

        public FallingTree(ICoreClientAPI capi, BlockPos pos, bool isLeaves, MeshData treeMesh, float fallTime, BlockFacing fallDirection, EnumRenderStage pass)
        {
            if (pos == null || treeMesh == null) return;

            this.capi = capi;
            this.pos = pos;
            this.treeMesh = capi.Render.UploadMesh(treeMesh);
            this.fallDirection = fallDirection;
            this.isLeaves = isLeaves;
            startFallTime = this.fallTime = fallTime;
            capi.Event.RegisterRenderer(this, pass);
        }

        public double RenderOrder => 1.0;

        public int RenderRange => 0;

        public void Dispose()
        {
            treeMesh?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (fallTime < -1)
            {
                capi.Event.UnregisterRenderer(this, stage);
                Dispose();
                return;
            }

            float percent = fallTime / startFallTime;

            IRenderAPI rpi = capi.Render;
            Vec3d camPos = capi.World.Player.Entity.CameraPos;

            if (isLeaves) rpi.GlDisableCullFace();

            rpi.GlToggleBlend(true);

            bool shadowPass = stage != EnumRenderStage.Opaque;

            IShaderProgram prevProg = rpi.CurrentActiveShader;
            prevProg?.Stop();

            IShaderProgram sProg = shadowPass ? rpi.GetEngineShader(EnumShaderProgram.Shadowmapentityanimated) : rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);

            var mat = ModelMat
                    .Identity()
                    .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                ;

            switch (fallDirection.Code)
            {
                case "north":
                    mat.RotateXDeg(GameMath.Max(percent * 90.0f - 90.0f, -90.0f));
                    break;
                case "south":
                    mat.Translate(0, 0, 1);
                    mat.RotateXDeg(GameMath.Min(percent * -90.0f + 90.0f, 90.0f));
                    mat.Translate(0, 0, -1);
                    break;
                case "east":
                    mat.Translate(1, 0, 1);
                    mat.RotateZDeg(GameMath.Max(percent * 90.0f - 90.0f, -90.0f));
                    mat.Translate(-1, 0, -1);
                    break;
                case "west":
                    mat.Translate(0, 0, 1);
                    mat.RotateZDeg(GameMath.Min(percent * -90.0f + 90.0f, 90.0f));
                    mat.Translate(0, 0, -1);
                    break;
                default:
                    break;
            }

            var matVals = mat.Values;

            if (!shadowPass)
            {
                var prog = (IStandardShaderProgram)sProg;

                prog.Tex2D = capi.BlockTextureAtlas.AtlasTextureIds[0];
                prog.ModelMatrix = matVals;

                prog.AlphaTest = 0.4f;

                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                if (fallTime < 0) 
                    prog.RgbaTint = new Vec4f(1, 1, 1, 1.0f - Math.Abs(fallTime));
            }
            else
            {
                sProg.Use();
                sProg.BindTexture2D("entityTex", capi.BlockTextureAtlas.AtlasTextureIds[0], 1);
                sProg.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
                sProg.UniformMatrix("modelViewMatrix", Mat4f.Mul(new float[16], capi.Render.CurrentModelviewMatrix, matVals));
            }
            
            rpi.RenderMesh(treeMesh);
            sProg.Stop();
            
            prevProg?.Use();

            fallTime -= deltaTime;
        }
    }

    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname) => (T)AccessTools.Field(instance.GetType(), fieldname).GetValue(instance);
        public static T GetProperty<T>(this object instance, string fieldname) => (T)AccessTools.Property(instance.GetType(), fieldname).GetValue(instance);
        public static T CallMethod<T>(this object instance, string method, params object[] args) => (T)AccessTools.Method(instance.GetType(), method).Invoke(instance, args);
        public static void CallMethod(this object instance, string method, params object[] args) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, args);
        public static void CallMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, null);
        public static object CreateInstance(this Type type) => AccessTools.CreateInstance(type);
        public static T[] GetFields<T>(this object instance)
        {
            List<T> fields = new List<T>();
            var declaredFields = AccessTools.GetDeclaredFields(instance.GetType())?.Where((t) => t.FieldType == typeof(T));
            foreach (var val in declaredFields)
            {
                fields.Add(instance.GetField<T>(val.Name));
            }
            return fields.ToArray();
        }

        public static void SetField(this object instance, string fieldname, object setVal) => AccessTools.Field(instance.GetType(), fieldname).SetValue(instance, setVal);
        public static MethodInfo GetMethod(this object instance, string method) => AccessTools.Method(instance.GetType(), method);
    }
}
