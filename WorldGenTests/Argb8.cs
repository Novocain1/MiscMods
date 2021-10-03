namespace WorldGenTests
{
    struct Argb8
    {
        public int Value;

        public float A { get => (((uint)Value & 0xFF000000) >> 24) / 255f; }
        public float R { get => (((uint)Value & 0x00FF0000) >> 16) / 255f; }
        public float G { get => (((uint)Value & 0x0000FF00) >> 08) / 255f; }
        public float B { get => (((uint)Value & 0x000000FF) >> 00) / 255f; }

        public Argb8(byte a, byte r, byte g, byte b)
        {
            Value = 0;
            Value |= (a << 24) | (r << 16) | (g << 08) | (b << 00);
        }

        public Argb8(int value)
        {
            Value = value;
        }
    }
}