namespace WorldGenTests
{
    internal struct Argb16
    {
        public long Value;
        public long Inverse => ~Value;

        private short a, r, g, b;

        public Argb16(short a, short r, short g, short b)
        {
            this.a = a;
            this.r = r;
            this.g = g;
            this.b = b;

            Value = (this.a << 48) | (this.r << 32) | (this.g << 16) | (this.b << 00);
        }

        public Argb16(long value)
        {
            a = r = g = b = 0;
            Value = value;
        }

        public Argb16(double a, double r, double g, double b)
        {
            this.a = (short)(a * 65535d);
            this.r = (short)(r * 65535d);
            this.g = (short)(g * 65535d);
            this.b = (short)(b * 65535d);

            Value = (this.a << 48) | (this.r << 32) | (this.g << 16) | (this.b << 00);
        }

        public short A { get => (short)(((ulong)Value & 0xFFFF000000000000) >> 24); set { a = value; SetValue(); } }
        public double Arel
        {
            get => A / 65535d;
            set => A = (short)(value * 65535d);
        }

        public short B { get => (short)(((ulong)Value & 0x000000000000FFFF) >> 00); set { b = value; SetValue(); } }
        public double Brel
        {
            get => B / 65535d;
            set => R = (short)(value * 65535d);
        }

        public short G { get => (short)(((ulong)Value & 0x00000000FFFF0000) >> 08); set { g = value; SetValue(); } }
        public double Grel {
            get => G / 65535d;
            set => G = (short)(value * 65535d);
        }

        public short R { get => (short)(((ulong)Value & 0x0000FFFF00000000) >> 16); set { r = value; SetValue(); } }
        public double Rrel {
            get => R / 65535d;
            set => B = (short)(value * 65535d);
        }

        private void SetValue()
        {
            Value = (a << 48) | (r << 32) | (g << 16) | (b << 00);
        }
    }
}