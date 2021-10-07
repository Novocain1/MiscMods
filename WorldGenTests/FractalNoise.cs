using System;
using Vintagestory.API.MathTools;

namespace WorldGenTests
{
    public class FractalNoise : NormalizedSimplexNoise
    {
        readonly double[] mat = new double[]
        {
                1.6,  1.2,
                -1.2,  1.6
        };

        public FractalNoise(double[] inputAmplitudes, double[] frequencies, long seed) : base(inputAmplitudes, frequencies, seed)
        {
        }

        public static new FractalNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
        {
            double[] frequencies = new double[quantityOctaves];
            double[] amplitudes = new double[quantityOctaves];

            for (int i = 0; i < quantityOctaves; i++)
            {
                frequencies[i] = Math.Pow(2, i) * baseFrequency;
                amplitudes[i] = Math.Pow(persistence, i);
            }

            return new FractalNoise(amplitudes, frequencies, seed);
        }

        public new virtual double Noise(double x, double y, double[] amplitudes = null)
        {
            double ox = x, oy = y;

            double f = 0.5000 * (amplitudes == null ? base.Noise(x, y) : base.Noise(x, y, amplitudes));
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;
            ox = x;
            oy = y;

            f += 0.2500 * (amplitudes == null ? base.Noise(x, y) : base.Noise(x, y, amplitudes));
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;
            ox = x;
            oy = y;

            f += 0.1250 * (amplitudes == null ? base.Noise(x, y) : base.Noise(x, y, amplitudes));
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;

            f += 0.0625 * (amplitudes == null ? base.Noise(x, y) : base.Noise(x, y, amplitudes));

            return f;
        }
    }
}