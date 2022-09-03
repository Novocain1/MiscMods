using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using OpenTK.Graphics.OpenGL;

namespace VSHUD
{
    class PlacementRenderer : IRenderer
    {
        ICoreClientAPI capi;
        IClientPlayer player { get => capi?.World?.Player; }
        ItemStack invStack { get => player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;  }
        Item invItem { get => invStack?.Item;  }
        Block invBlock { get => GetInvBlock(); }
        BlockSelection playerSelection { get => player?.CurrentBlockSelection; }
        BlockPos pos { get => playerSelection?.Position; }
        Vec3d camPos { get => player?.Entity.CameraPos; }
        Vec3d playerPos { get => player?.Entity.Pos.XYZ;  }
        VSHUDConfig config { get => capi.ModLoader.GetModSystem<WaypointUtils>().Config; }
        ShapeTesselatorManager tesselatormanager { get => capi.TesselatorManager as ShapeTesselatorManager; }
        bool shouldDispose = true;
        
        List<Type> NonCulledTypes { get; set; }
        List<Type> IgnoredTypes { get; set; }
        List<Type> SneakPlacedTypes { get; set; }

        public Dictionary<Type, Action> itemActions = new Dictionary<Type, Action>();

        MeshRef mRef;

        IRenderAPI rpi;
        public Matrixf ModelMat = new Matrixf();
        Block toBlock;

        Block itemPlacedBlock;

        public PlacementRenderer(ICoreClientAPI capi)
        {
            this.capi = capi;
            rpi = capi.Render;

            itemActions[typeof(ItemStone)] = () => { itemPlacedBlock = capi.World.GetBlock(invItem.CodeWithPath("loosestones-" + invItem.LastCodePart() + "-free")); };
            NonCulledTypes = new List<Type>()
            {
                typeof(BlockFernTree),
                typeof(BlockPlant),
                typeof(BlockVines),
                typeof(BlockLeaves),
                typeof(BlockSeaweed)
            };
            IgnoredTypes = new List<Type>()
            {
                typeof(BlockMushroom)
            };
            SneakPlacedTypes = new List<Type>()
            {
                typeof(BlockBehaviorRightClickPickup),
                typeof(BlockMeal),
                typeof(BlockBucket),
                typeof(ItemStone)
            };
        }

        public Block GetInvBlock() 
        {
            Block block = invStack?.Block;
            if (block == null && invItem != null)
            {
                if (itemActions.TryGetValue(invItem.GetType(), out Action action))
                {
                    action.Invoke();
                    block = itemPlacedBlock;
                }
                else itemPlacedBlock = block = null;
            }
            return block;
        }

        public void UpdateBlockMesh(Block toBlock, BlockPos altPos)
        {
            MeshData mesh;
            MealMeshCache meshCache = capi.ModLoader.GetModSystem<MealMeshCache>();
            var lod0 = toBlock.Lod0Mesh;
            var lod1 = tesselatormanager.blockModelDatas[toBlock.Id].Clone();
            var lod2 = toBlock.Lod2Mesh;

            var lod0alt = tesselatormanager.altblockModelDatasLod0[toBlock.Id];
            var lod1alt = tesselatormanager.altblockModelDatasLod1[toBlock.Id];
            var lod2alt = tesselatormanager.altblockModelDatasLod2[toBlock.Id];


            if (toBlock is BlockMeal)
            {
                mRef = meshCache.GetOrCreateMealInContainerMeshRef(toBlock, ((BlockMeal)toBlock).GetCookingRecipe(capi.World, invStack), ((BlockMeal)toBlock).GetNonEmptyContents(capi.World, invStack));
                shouldDispose = false;
                return;
            }
            else if (toBlock is BlockCookedContainer)
            {
                mRef = meshCache.GetOrCreateMealInContainerMeshRef(toBlock, ((BlockCookedContainer)toBlock).GetCookingRecipe(capi.World, invStack), ((BlockCookedContainer)toBlock).GetNonEmptyContents(capi.World, invStack), new Vec3f(0.0f, 2.5f / 16f, 0.0f));
                shouldDispose = false;
                return;
            }
            else if (toBlock is BlockChisel)
            {
                mRef = capi.ModLoader.GetModSystem<ChiselBlockModelCache>().GetOrCreateMeshRef(invStack);
                shouldDispose = false;
                return;
            }
            else if (toBlock.HasAlternates && lod1alt != null)
            {
                long alternateIndex = toBlock.RandomizeAxes == EnumRandomizeAxes.XYZ ? GameMath.MurmurHash3Mod(altPos.X, altPos.Y, altPos.Z, lod1alt.Length) : GameMath.MurmurHash3Mod(altPos.X, 0, altPos.Z, lod1alt.Length);
                mesh = lod1alt[alternateIndex].Clone();
                var lod_0 = lod0alt?[alternateIndex].Clone();
                var lod_2 = lod2alt?[alternateIndex].Clone();

                mesh.IndicesMax = mesh.Indices.Count();
                if (lod_0 != null)
                {
                    lod_0.IndicesMax = lod_0.Indices.Count();
                    if (!mesh.Equals(lod_0)) mesh.AddMeshData(lod_0);
                }
                if (lod_2 != null)
                {
                    lod_2.IndicesMax = lod_2.Indices.Count();
                    if (!mesh.Equals(lod_2)) mesh.AddMeshData(lod_2);
                }
                mesh.CompactBuffers();
            }
            else
            { 
                mesh = lod1;
                mesh.IndicesMax = mesh.Indices.Count();
                if (lod0 != null)
                {
                    lod0.IndicesMax = lod0.Indices.Count();
                    if (!mesh.Equals(lod0)) mesh.AddMeshData(lod0);
                }
                if (lod2 != null)
                {
                    lod2.IndicesMax = lod2.Indices.Count();
                    if (!mesh.Equals(lod2)) mesh.AddMeshData(lod2);
                }
                mesh.CompactBuffers();
            }

            if (toBlock.RandomizeRotations)
            {
                int index = GameMath.MurmurHash3Mod(altPos.X, altPos.Y, altPos.Z, JsonTesselator.randomRotations.Length);
                mesh = mesh.Clone().MatrixTransform(JsonTesselator.randomRotMatrices[index]);
            }
            if (mRef != null && shouldDispose) mRef.Dispose();
            shouldDispose = true;
            MeshData rotMesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, toBlock.GetRotY(playerPos, playerSelection), 0);
            
            mRef = rpi.UploadMesh(rotMesh);
        }

        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        public void Dispose()
        {
            if (shouldDispose) mRef?.Dispose();
        }

        public bool SneakCheck { get => SneakChecked && !player.Entity.Controls.Sneak; }

        public bool NonCulled { get => IsNonCulled(invBlock) || invBlock.BlockBehaviors.Any((b) => IsNonCulled(b)); }
        public bool Ignored { get => ShouldIgnore(invBlock) || invBlock.BlockBehaviors.Any((b) => ShouldIgnore(b)); }
        public bool SneakChecked { get => IsSneakChecked((object)invItem ?? invBlock) || invBlock.BlockBehaviors.Any((b) => IsSneakChecked(b)); }

        public bool IsSneakChecked(object obj)
        {
            return SneakPlacedTypes.Contains(obj.GetType()) || SneakPlacedTypes.Contains(obj.GetType().BaseType);
        }

        public bool IsNonCulled(object obj)
        {
            return NonCulledTypes.Contains(obj.GetType()) || NonCulledTypes.Contains(obj.GetType().BaseType);
        }

        public bool ShouldIgnore(object obj)
        {
            return capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival && (IgnoredTypes.Contains(obj.GetType()) || IgnoredTypes.Contains(obj.GetType().BaseType));
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            var selClone = playerSelection?.Clone();

            if (selClone == null || invBlock == null || pos == null || !config.PRShow || Ignored || SneakCheck) return;
            selClone.Position = selClone.Position.GetBlock(capi).IsReplacableBy(invBlock) ? selClone.Position : selClone.Position.Offset(selClone.Face);

            toBlock = capi.World.BlockAccessor.GetBlock(SetBlockRedirectMaster.blockId);
            if (toBlock == null || toBlock.Id == 0) return;
            
            BlockPos adjPos = selClone.Position;

            UpdateBlockMesh(toBlock, adjPos);
            if (mRef == null) return;
            
            if (!capi.World.BlockAccessor.GetBlock(adjPos).IsReplacableBy(invBlock)) return;
            rpi.GlToggleBlend(true);

            if (NonCulled) rpi.GlDisableCullFace();

            Vec2f offset = adjPos.GetOffset(toBlock);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(adjPos.X, adjPos.Y, adjPos.Z);
            prog.Tex2D = capi.BlockTextureAtlas.AtlasTextureIds[0];

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(adjPos.X - camPos.X, adjPos.Y - camPos.Y, adjPos.Z - camPos.Z)
                .Translate(offset.X, 0, offset.Y)
                .Values;
            
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            Vec4f col = new Vec4f(1.0f, 1.0f, 1.0f, config.PROpacity);

            if (config.PRTint)
            {
                col.Add(new Vec4f(config.PRTintColor[0], config.PRTintColor[1], config.PRTintColor[2], 0.0f));
            }
            
            prog.SsaoAttn = 0;
            prog.AlphaTest = 0.05f;
            prog.RgbaTint = new Vec4f(col.R, col.G, col.B, config.PROpacity);
            prog.RgbaGlowIn = new Vec4f(col.R, col.G, col.B, 1.0f);
            prog.ExtraGlow = 255 / (int)(capi.World.Calendar.SunLightStrength * 64.0f);
            rpi.RenderMesh(mRef);
            prog.Stop();

            rpi.GLDisableDepthTest();
            GL.Enable(EnableCap.LineSmooth);

            var origdrawmode = (mRef as VAO).drawMode;
            if (config.PRDrawLines) (mRef as VAO).drawMode = PrimitiveType.LineLoop;


            prog.Use();
            if (config.PRDrawLines) prog.Tex2D = rpi.GetOrLoadTexture(new AssetLocation("block/creative/col78.png"));
            prog.RgbaTint = new Vec4f(col.R, col.G, col.B, config.PROpacity / 8.0f);
            prog.RgbaGlowIn = new Vec4f(col.R, col.G, col.B, 1.0f);
            prog.ExtraGlow = config.PRDrawLines ? 127 : 0;
            rpi.RenderMesh(mRef);
            prog.Stop();

            (mRef as VAO).drawMode = origdrawmode;

            rpi.GLEnableDepthTest();
            GL.Disable(EnableCap.LineSmooth);

            rpi.GlEnableCullFace();
        }
    }
}
