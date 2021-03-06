﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.API.Datastructures;

namespace VSHUD
{
    class PlacementPreview : ClientModSystem
    {
        PlacementRenderer renderer;
        public override void StartClientSide(ICoreClientAPI api)
        {
            renderer = new PlacementRenderer(api);
            api.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
            api.Input.RegisterHotKey("placementpreviewtoggle", "Toggle Placement Preview", GlKeys.Quote);
            api.Input.SetHotKeyHandler("placementpreviewtoggle", (a) => 
            {
                VSHUDConfig config = api.ModLoader.GetModSystem<WaypointUtils>().Config;
                config.PRShow = !config.PRShow;
                api.ModLoader.GetModSystem<ConfigLoader>().SaveConfig();
                return true;
            });

            api.RegisterCommand("pconfig", "Config Placement Preview System", "[enabled|tinted|tintcolorhex|tintcolorrgb|tintdefault|opacity]", (id, args) =>
            {
                VSHUDConfig config = api.ModLoader.GetModSystem<WaypointUtils>().Config;
                string arg = args.PopWord();
                bool? enabled;

                switch (arg)
                {
                    case "enabled":
                        enabled = args.PopBool();
                        config.PRShow = enabled ?? !config.PRShow;
                        api.ShowChatMessage("Block preview set to " + config.PRShow);
                        break;
                    case "tinted":
                        enabled = args.PopBool();
                        config.PRTint = enabled ?? !config.PRTint;
                        api.ShowChatMessage("Block preview tinting set to " + config.PRTint);
                        break;
                    case "tintcolorhex":
                        string col = args.PopWord();
                        if (col?[0] == '#')
                        {
                            var color = ColorUtil.Hex2Doubles(col);
                            config.PRTintColor = new float[]
                            {
                                (float)(color[0]) * 10.0f,
                                (float)(color[1]) * 10.0f,
                                (float)(color[2]) * 10.0f,
                            };
                        }
                        break;
                    case "opacity":
                        config.PROpacity = args.PopFloat() ?? config.PROpacity;
                        break;
                    case "opacitydefault":
                        config.PROpacity = new VSHUDConfig().PROpacity;
                        break;
                    case "tintcolorrgb":
                        config.PRTintColor[0] = args.PopFloat() ?? config.PRTintColor[0];
                        config.PRTintColor[1] = args.PopFloat() ?? config.PRTintColor[1];
                        config.PRTintColor[2] = args.PopFloat() ?? config.PRTintColor[2];
                        break;
                    case "tintdefault":
                        config.PRTintColor = new VSHUDConfig().PRTintColor;
                        break;
                    default:
                        break;
                }
                api.ModLoader.GetModSystem<ConfigLoader>().SaveConfig();
            });
            api.Event.LevelFinalize += () => api.Shader.ReloadShaders();
        }
    }

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

        public Dictionary<Type, Vintagestory.API.Common.Action> itemActions = new Dictionary<Type, Vintagestory.API.Common.Action>();

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
                if (itemActions.TryGetValue(invItem.GetType(), out Vintagestory.API.Common.Action action))
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
            var lod0 = tesselatormanager.blockModelDatasLod0.ContainsKey(toBlock.Id) ? tesselatormanager.blockModelDatasLod0[toBlock.Id] : null;
            var lod1 = tesselatormanager.blockModelDatas[toBlock.Id].Clone();

            var lod0alt = tesselatormanager.altblockModelDatasLod0[toBlock.Id];
            var lod1alt = tesselatormanager.altblockModelDatasLod1[toBlock.Id];


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
                var lod = lod0alt?[alternateIndex].Clone();

                if (lod != null && mesh != lod)
                {
                    mesh.IndicesMax = mesh.Indices.Count();
                    lod.IndicesMax = lod.Indices.Count();

                    mesh.AddMeshData(lod);
                    mesh.CompactBuffers();
                }
            }
            else
            { 
                mesh = lod1;
                mesh.IndicesMax = mesh.Indices.Count();
                if (lod0 != null)
                {
                    lod0.IndicesMax = lod0.Indices.Count();
                    if (mesh != lod0) mesh.AddMeshData(lod0);
                }
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
            mRef?.Dispose();
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

            toBlock = capi.World.BlockAccessor.GetBlock(SetBlockRedirect.blockId);
            if (toBlock == null || toBlock.Id == 0) return;
            
            BlockPos adjPos = selClone.Position;

            UpdateBlockMesh(toBlock, adjPos);
            if (mRef == null) return;
            
            if (!capi.World.BlockAccessor.GetBlock(adjPos).IsReplacableBy(invBlock)) return;
            rpi.GlToggleBlend(true);

            if (NonCulled) rpi.GlDisableCullFace();

            Vec2f offset = adjPos.GetOffset(toBlock);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(adjPos.X, adjPos.Y, adjPos.Z);
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(adjPos.X - camPos.X, adjPos.Y - camPos.Y, adjPos.Z - camPos.Z)
                .Translate(offset.X, 0, offset.Y)
                .Values;
            
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            Vec4f col;

            if (config.PRTint) col = prog.RgbaTint = new Vec4f(1 + config.PRTintColor[0], 1 + config.PRTintColor[1], 1 + config.PRTintColor[2], config.PROpacity);
            else prog.RgbaTint = col = new Vec4f(1, 1, 1, config.PROpacity);

            prog.RgbaGlowIn = new Vec4f(col.R, col.G, col.B, 1.0f);
            prog.ExtraGlow = 255 / (int)(capi.World.Calendar.SunLightStrength * 64.0f);
            
            rpi.RenderMesh(mRef);
            prog.Stop();

            rpi.GlEnableCullFace();
        }
    }
}
