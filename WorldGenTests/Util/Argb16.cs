using Vintagestory.API.MathTools;

namespace WorldGenTests
{
    internal struct Argb16
    {
        private long val;

        public long Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                a = (short)((val >> 48) & 0xFFFF);
                r = (short)((val >> 32) & 0xFFFF);
                g = (short)((val >> 16) & 0xFFFF);
                b = (short)((val >> 00) & 0xFFFF);
            }
        }
        public long Inverse => ~Value;

        private short a, r, g, b;

        public Argb16(short a, short r, short g, short b)
        {
            this.a = a;
            this.r = r;
            this.g = g;
            this.b = b;

            val = (this.a << 48) | (this.r << 32) | (this.g << 16) | (this.b << 00);
        }

        public Argb16(long value)
        {
            a = r = g = b = 0;
            val = value;
        }

        public Argb16(double a, double r, double g, double b)
        {
            this.a = (short)(a * 65535d);
            this.r = (short)(r * 65535d);
            this.g = (short)(g * 65535d);
            this.b = (short)(b * 65535d);

            val = (this.a << 48) | (this.r << 32) | (this.g << 16) | (this.b << 00);
        }

        public short A { get => (short)(((ulong)Value & 0xFFFF000000000000) >> 48); set { a = value; SetValue(); } }
        public double Arel
        {
            get => A / 65535d;
            set => A = (short)(GameMath.Clamp(value * 65535d, 0, ushort.MaxValue));
        }

        public short B { get => (short)(((ulong)Value & 0x000000000000FFFF) >> 00); set { b = value; SetValue(); } }
        public double Brel
        {
            get => B / 65535d;
            set => R = (short)(GameMath.Clamp(value * 65535d, 0, ushort.MaxValue));
        }

        public short G { get => (short)(((ulong)Value & 0x00000000FFFF0000) >> 16); set { g = value; SetValue(); } }
        public double Grel {
            get => G / 65535d;
            set => G = (short)(GameMath.Clamp(value * 65535d, 0, ushort.MaxValue));
        }

        public short R { get => (short)(((ulong)Value & 0x0000FFFF00000000) >> 32); set { r = value; SetValue(); } }
        public double Rrel {
            get => R / 65535d;
            set => B = (short)(GameMath.Clamp(value * 65535d, 0, ushort.MaxValue));
        }

        private void SetValue()
        {
            Value = (a << 48) | (r << 32) | (g << 16) | (b << 00);
        }
    }
}