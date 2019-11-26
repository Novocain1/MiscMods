using Newtonsoft.Json;
using System.Collections.Generic;

namespace SwingingDoor
{
    public partial class ModelglTf
    {
        [JsonProperty("asset")]
        public Asset Asset { get; set; }

        [JsonProperty("scene")]
        public long Scene { get; set; }

        [JsonProperty("scenes")]
        public List<Scene> Scenes { get; set; }

        [JsonProperty("nodes")]
        public List<Node> Nodes { get; set; }

        [JsonProperty("materials")]
        public List<Material> Materials { get; set; }

        [JsonProperty("meshes")]
        public List<Mesh> Meshes { get; set; }

        [JsonProperty("textures")]
        public List<TextureElement> Textures { get; set; }

        [JsonProperty("images")]
        public List<Image> Images { get; set; }

        [JsonProperty("accessors")]
        public List<Accessor> Accessors { get; set; }

        [JsonProperty("bufferViews")]
        public List<BufferView> BufferViews { get; set; }

        [JsonProperty("buffers")]
        public List<Buffer> Buffers { get; set; }
    }

    public partial class Accessor
    {
        [JsonProperty("bufferView")]
        public long BufferView { get; set; }

        [JsonProperty("componentType")]
        public long ComponentType { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("max", NullValueHandling = NullValueHandling.Ignore)]
        public List<double> Max { get; set; }

        [JsonProperty("min", NullValueHandling = NullValueHandling.Ignore)]
        public List<double> Min { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class Asset
    {
        [JsonProperty("generator")]
        public string Generator { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public partial class BufferView
    {
        [JsonProperty("buffer")]
        public long Buffer { get; set; }

        [JsonProperty("byteLength")]
        public long ByteLength { get; set; }

        [JsonProperty("byteOffset")]
        public long ByteOffset { get; set; }
    }

    public partial class Buffer
    {
        [JsonProperty("byteLength")]
        public long ByteLength { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public partial class Image
    {
        [JsonProperty("bufferView")]
        public long BufferView { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class Material
    {
        [JsonProperty("doubleSided")]
        public bool DoubleSided { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("normalTexture")]
        public Texture NormalTexture { get; set; }

        [JsonProperty("pbrMetallicRoughness")]
        public PbrMetallicRoughness PbrMetallicRoughness { get; set; }
    }

    public partial class Texture
    {
        [JsonProperty("index")]
        public long Index { get; set; }

        [JsonProperty("texCoord")]
        public long TexCoord { get; set; }
    }

    public partial class PbrMetallicRoughness
    {
        [JsonProperty("baseColorTexture")]
        public Texture BaseColorTexture { get; set; }

        [JsonProperty("metallicRoughnessTexture")]
        public Texture MetallicRoughnessTexture { get; set; }
    }

    public partial class Mesh
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("primitives")]
        public List<Primitive> Primitives { get; set; }
    }

    public partial class Primitive
    {
        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }

        [JsonProperty("indices")]
        public long Indices { get; set; }

        [JsonProperty("material")]
        public long Material { get; set; }
    }

    public partial class Attributes
    {
        [JsonProperty("POSITION")]
        public long Position { get; set; }

        [JsonProperty("NORMAL")]
        public long Normal { get; set; }

        [JsonProperty("TEXCOORD_0")]
        public long Texcoord0 { get; set; }
    }

    public partial class Node
    {
        [JsonProperty("mesh")]
        public long Mesh { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rotation")]
        public List<double> Rotation { get; set; }
    }

    public partial class Scene
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nodes")]
        public List<long> Nodes { get; set; }
    }

    public partial class TextureElement
    {
        [JsonProperty("source")]
        public long Source { get; set; }
    }
}