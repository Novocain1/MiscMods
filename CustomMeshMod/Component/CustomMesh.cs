using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CustomMeshMod
{
    public class CustomMesh
    {
        public string Base { get; set; } = "";
        public MeshType meshType { get; set; }
        public EnumNormalShading shading { get; set; } = EnumNormalShading.flat;
        public bool BackFaceCulling { get; set; } = true;
        public CompositeTexture Texture { get; set; }
        public AssetLocation fullPath { get => new AssetLocation(Base + "." + (meshType == MeshType.meshdata ? "json" : Enum.GetName(typeof(MeshType), meshType))); }
        public float rotateX { get; set; } = 0;
        public float rotateY { get; set; } = 0;
        public float rotateZ { get; set; } = 0;

        public float scaleX { get; set; } = 1;
        public float scaleY { get; set; } = 1;
        public float scaleZ { get; set; } = 1;

        public Vec3f rot { get => new Vec3f(rotateX, rotateY, rotateZ); }
        public Vec3f scale { get => new Vec3f(scaleX, scaleY, scaleZ); }
    }
}