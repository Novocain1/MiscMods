using Vintagestory.API.MathTools;

namespace WorldGenTests
{
    internal struct Argb8
    {
        private int val;

        public int Value 
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                a = (byte)((val >> 24) & 0xFF);
                r = (byte)((val >> 16) & 0xFF);
                g = (byte)((val >> 08) & 0xFF);
                b = (byte)((val >> 00) & 0xFF);
            }
        }
        public int Inverse => ~Value;
        private byte a, r, g, b;

        public Argb8(byte a, byte r, byte g, byte b)
        {
            this.a = a;
            this.r = r;
            this.g = g;
            this.b = b;

            val = (this.a << 24) | (this.r << 16) | (this.g << 08) | (this.b << 00);
        }

        public Argb8(int value)
        {
            a = r = g = b = 0;
            val = value;
        }

        public Argb8(float a, float r, float g, float b)
        {
            this.a = (byte)(a * 255f);
            this.r = (byte)(r * 255f);
            this.g = (byte)(g * 255f);
            this.b = (byte)(b * 255f);

            val = (this.a << 24) | (this.r << 16) | (this.g << 08) | (this.b << 00);
        }

        public byte A { get => (byte)(((uint)Value & 0xFF000000) >> 24); set { a = value; SetValue(); } }
        public float Arel
        {
            get => A / 255f;
            set => A = (byte)(GameMath.Clamp(value * 255f, 0, 255));
        }

        public byte B { get => (byte)(((uint)Value & 0x000000FF) >> 00); set { b = value; SetValue(); } }
        public float Brel {
            get => B / 255f;
            set => B = (byte)(GameMath.Clamp(value * 255f, 0, 255));
        }

        public byte G { get => (byte)(((uint)Value & 0x0000FF00) >> 08); set { g = value; SetValue(); } }
        public float Grel {
            get => G / 255f;
            set => G = (byte)(GameMath.Clamp(value * 255f, 0, 255));
        }

        public byte R { get => (byte)(((uint)Value & 0x00FF0000) >> 16); set { r = value; SetValue(); } }
        public float Rrel {
            get => R / 255f;
            set => R = (byte)(GameMath.Clamp(value * 255f, 0, 255));
        }

        private void SetValue()
        {
            val = (a << 24) | (r << 16) | (g << 08) | (b << 00);
        }
    }
}