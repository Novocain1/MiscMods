using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using OpenTK.Graphics.OpenGL;

namespace CustomMeshMod
{
    public class CustomMesh
    {
        public string Base { get; set; } = "";
        public MeshType MeshType { get; set; }
        public EnumNormalShading NormalShading { get; set; } = EnumNormalShading.Flat;
        public TextureMagFilter Interpolation { get; set; } = TextureMagFilter.Nearest;
        public bool BackFaceCulling { get; set; } = true;
        public CompositeTexture Texture { get; set; }
        public AssetLocation FullPath { get => new AssetLocation(Base + "." + (MeshType == MeshType.meshdata ? "json" : Enum.GetName(typeof(MeshType), MeshType))); }
        public float RotateX { get; set; } = 0;
        public float RotateY { get; set; } = 0;
        public float RotateZ { get; set; } = 0;

        public float ScaleX { get; set; } = 1;
        public float ScaleY { get; set; } = 1;
        public float ScaleZ { get; set; } = 1;

        public Vec3f Rot { get => new Vec3f(RotateX, RotateY, RotateZ); }
        public Vec3f Scale { get => new Vec3f(ScaleX, ScaleY, ScaleZ); }
    }
}