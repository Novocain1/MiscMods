using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods;

namespace WorldGenTests
{
    public class FractalNoise : NormalizedSimplexNoise
    {
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

        readonly double[] mat = new double[]
        {
                1.6,  1.2,
                -1.2,  1.6
        };

        public double FractalFromNoise(double x, double y)
        {
            double ox = x, oy = y;

            double f = 0.5000 * Noise(x, y);
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;
            ox = x;
            oy = y;

            f += 0.2500 * Noise(x, y);
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;
            ox = x;
            oy = y;

            f += 0.1250 * Noise(x, y);
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;

            f += 0.0625 * Noise(x, y);

            return f;
        }

        public double FractalFromNoise(double x, double y, double[] amplitudes)
        {
            double ox = x, oy = y;

            double f = 0.5000 * Noise(x, y, amplitudes);
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;
            ox = x;
            oy = y;

            f += 0.2500 * Noise(x, y, amplitudes);
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;
            ox = x;
            oy = y;

            f += 0.1250 * Noise(x, y, amplitudes);
            x = mat[0] * ox + mat[1] * oy;
            y = mat[2] * ox + mat[3] * oy;

            f += 0.0625 * Noise(x, y);

            return f;
        }
    }

    public class MapLayerOreVeins : MapLayerBase
    {
        private FractalNoise noisegenA, noisegenR, noisegenG, noisegenB;
        private double ridgedMul;

        private float multiplier;
        private double[] thresholds;
        double cullTest;

        public MapLayerOreVeins(long seed, int octaves, float persistence, int multiplier, int scaleA, int scaleR, int scaleG, int scaleB, double ridgedMul = 1.0, double cullTest = 0.8) : base(seed)
        {
            this.ridgedMul = ridgedMul;
            this.cullTest = cullTest;

            noisegenA = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleA, persistence, seed + 7312654);
            noisegenR = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleR, persistence, seed + 5498987);
            noisegenG = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleG, persistence, seed + 2987992);
            noisegenB = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleB, persistence, seed + 4987462);

            this.multiplier = multiplier;
        }

        public MapLayerOreVeins(long seed, int octaves, float persistence, int scale, int multiplier, int scaleA, int scaleR, int scaleG, int scaleB, double[] thresholds, double ridgedMul = 1.0, double cullTest = 0.8) : base(seed)
        {
            this.ridgedMul = ridgedMul;
            this.cullTest = cullTest;

            noisegenA = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleA, persistence, seed + 7312654);
            noisegenR = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleR, persistence, seed + 5498987);
            noisegenG = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleG, persistence, seed + 2987992);
            noisegenB = FractalNoise.FromDefaultOctaves(octaves, 1f / scaleB, persistence, seed + 4987462);

            this.multiplier = multiplier;
            this.thresholds = thresholds;
        }

        public int GetRGBANoise(int xCoord, int x, int zCoord, int z, int flags = 0, double[] thresholds = null)
        {
            bool inverse = (flags & 0b10000) > 0;

            double nR, nG, nB, nA;

            double nRX = xCoord + x;
            double nRZ = zCoord + z;
            double nGX = xCoord + x;
            double nGZ = zCoord + z;
            double nBX = xCoord + x;
            double nBZ = zCoord + z;
            double nAX = xCoord + x;
            double nAZ = zCoord + z;
            
            int onCol = flags >> 5;

            if (thresholds != null)
            {
                switch (onCol)
                {
                    case 1:
                        nA = noisegenA.FractalFromNoise(nAX, nAZ, thresholds);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                        if (nA < cullTest)
                        {
                            nR = noisegenR.FractalFromNoise(nRX, nRZ, thresholds);
                            nG = noisegenG.FractalFromNoise(nGX, nGZ, thresholds);
                            nB = noisegenB.FractalFromNoise(nBX, nBZ, thresholds);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    case 2:
                        nR = noisegenR.FractalFromNoise(nRX, nRZ, thresholds);
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                        if (nR < cullTest)
                        {
                            nA = noisegenA.FractalFromNoise(nAX, nAZ, thresholds);
                            nG = noisegenG.FractalFromNoise(nGX, nGZ, thresholds);
                            nB = noisegenB.FractalFromNoise(nBX, nBZ, thresholds);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    case 3:
                        nG = noisegenG.FractalFromNoise(nGX, nGZ, thresholds);
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                        if (nG < cullTest)
                        {
                            nA = noisegenA.FractalFromNoise(nAX, nAZ, thresholds);
                            nR = noisegenR.FractalFromNoise(nRX, nRZ, thresholds);
                            nB = noisegenB.FractalFromNoise(nBX, nBZ, thresholds);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    case 4:
                        nB = noisegenB.FractalFromNoise(nBX, nBZ, thresholds);
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        if (nB < cullTest)
                        {
                            nA = noisegenA.FractalFromNoise(nAX, nAZ, thresholds);
                            nR = noisegenR.FractalFromNoise(nRX, nRZ, thresholds);
                            nG = noisegenG.FractalFromNoise(nGX, nGZ, thresholds);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    default:
                        nA = noisegenA.FractalFromNoise(nAX, nAZ, thresholds);
                        nR = noisegenR.FractalFromNoise(nRX, nRZ, thresholds);
                        nG = noisegenG.FractalFromNoise(nGX, nGZ, thresholds);
                        nB = noisegenB.FractalFromNoise(nBX, nBZ, thresholds);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        break;
                }
            }
            else
            {
                switch (onCol)
                {
                    case 1:
                        nA = noisegenA.FractalFromNoise(nAX, nAZ);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                        if (nA < cullTest)
                        {
                            nR = noisegenR.FractalFromNoise(nRX, nRZ);
                            nG = noisegenG.FractalFromNoise(nGX, nGZ);
                            nB = noisegenB.FractalFromNoise(nBX, nBZ);
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    case 2:
                        nR = noisegenR.FractalFromNoise(nRX, nRZ);
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                        if (nR < cullTest)
                        {
                            nA = noisegenA.FractalFromNoise(nAX, nAZ);
                            nG = noisegenG.FractalFromNoise(nGX, nGZ);
                            nB = noisegenB.FractalFromNoise(nBX, nBZ);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    case 3:
                        nG = noisegenG.FractalFromNoise(nGX, nGZ);
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                        if (nG < cullTest)
                        {
                            nA = noisegenA.FractalFromNoise(nAX, nAZ);
                            nR = noisegenR.FractalFromNoise(nRX, nRZ);
                            nB = noisegenB.FractalFromNoise(nBX, nBZ);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    case 4:
                        nB = noisegenB.FractalFromNoise(nBX, nBZ);
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        if (nB < cullTest)
                        {
                            nA = noisegenA.FractalFromNoise(nAX, nAZ);
                            nR = noisegenR.FractalFromNoise(nRX, nRZ);
                            nG = noisegenG.FractalFromNoise(nGX, nGZ);
                            if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                            if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                        }
                        else nA = nR = nG = nB = inverse ? 1 : 0;
                        break;
                    default:
                        nA = noisegenA.FractalFromNoise(nAX, nAZ);
                        nR = noisegenR.FractalFromNoise(nRX, nRZ);
                        nG = noisegenG.FractalFromNoise(nGX, nGZ);
                        nB = noisegenB.FractalFromNoise(nBX, nBZ);
                        if ((flags & 0b01000) > 0) nA = Math.Abs((nA - 0.5) * 2) * ridgedMul;
                        if ((flags & 0b00001) > 0) nR = Math.Abs((nR - 0.5) * 2) * ridgedMul;
                        if ((flags & 0b00010) > 0) nG = Math.Abs((nG - 0.5) * 2) * ridgedMul;
                        if ((flags & 0b00100) > 0) nB = Math.Abs((nB - 0.5) * 2) * ridgedMul;
                        break;
                }
            }

            byte r = (byte)GameMath.Clamp(multiplier * nR, 0, 255);
            byte g = (byte)GameMath.Clamp(multiplier * nG, 0, 255);
            byte b = (byte)GameMath.Clamp(multiplier * nB, 0, 255);
            byte a = (byte)GameMath.Clamp(multiplier * nA, 0, 255);

            int rgba = b | g << 8 | r << 16 | a << 24;

            return inverse ? ~rgba : rgba;
        }

        public int GetRGBAScribbleNoise(int xCoord, int x, int zCoord, int z, int flags = 0, double[] thresholds = null, int depth = 2)
        {
            double nR, nG, nB, nA;
            nR = nG = nB = nA = 0.0;

            double nRX = xCoord + x + 0000000;
            double nRZ = zCoord + z + 0000000;
            double nGX = xCoord + x + 5498987;
            double nGZ = zCoord + z + 5498987;
            double nBX = xCoord + x + 2987992;
            double nBZ = zCoord + z + 2987992;
            double nAX = xCoord + x + 4987462;
            double nAZ = zCoord + z + 4987462;

            for (int i = 1; i <= depth; i++)
            {
                double nRt, nGt, nBt, nAt;

                if (thresholds != null)
                {
                    nRt = noisegenR.FractalFromNoise((nRX * i) + (i * 512), (nRZ * i) + (i * 512), thresholds) / depth;
                    nGt = noisegenG.FractalFromNoise((nGX * i) + (i * 512), (nGZ * i) + (i * 512), thresholds) / depth;
                    nBt = noisegenB.FractalFromNoise((nBX * i) + (i * 512), (nBZ * i) + (i * 512), thresholds) / depth;
                    nAt = noisegenA.FractalFromNoise((nAX * i) + (i * 512), (nAZ * i) + (i * 512), thresholds) / depth;
                }
                else
                {
                    nRt = noisegenR.FractalFromNoise((nRX * i) + (i * 512), (nRZ * i) + (i * 512)) / depth;
                    nGt = noisegenG.FractalFromNoise((nGX * i) + (i * 512), (nGZ * i) + (i * 512)) / depth;
                    nBt = noisegenB.FractalFromNoise((nBX * i) + (i * 512), (nBZ * i) + (i * 512)) / depth;
                    nAt = noisegenA.FractalFromNoise((nAX * i) + (i * 512), (nAZ * i) + (i * 512)) / depth;
                }

                nR += (flags & 0b00001) > 0 ? Math.Abs((nRt * depth - 0.5) * 2.0) / depth : nRt;
                nG += (flags & 0b00010) > 0 ? Math.Abs((nGt * depth - 0.5) * 2.0) / depth : nGt;
                nB += (flags & 0b00100) > 0 ? Math.Abs((nBt * depth - 0.5) * 2.0) / depth : nBt;
                nA += (flags & 0b01000) > 0 ? Math.Abs((nAt * depth - 0.5) * 2.0) / depth : nAt;
            }

            bool inverse = (flags & 0b10000) > 0;

            byte r = (byte)GameMath.Clamp(multiplier * nR, 0, 255);
            byte g = (byte)GameMath.Clamp(multiplier * nG, 0, 255);
            byte b = (byte)GameMath.Clamp(multiplier * nB, 0, 255);
            byte a = (byte)GameMath.Clamp(multiplier * nA, 0, 255);

            int rgba = b | g << 8 | r << 16 | a << 24;

            return inverse ? ~rgba : rgba;
        }

        public virtual int[] GenLayer(int xCoord, int zCoord, int sizeXSmall, int sizeZSmall, int sizeXLarge, int sizeZLarge, int flags)
        {
            int smallSize = (sizeXSmall + sizeZSmall) / 2;
            int largeSize = (sizeXLarge + sizeZLarge) / 2;

            int step = largeSize / smallSize / 2;

            int[] smallData = GenLayer(xCoord, zCoord, largeSize, largeSize, step * 2, flags);
            int[] largeData = new int[largeSize * largeSize];

            for (int z = 0; z < largeSize; ++z)
            {
                for (int x = 0; x < largeSize; ++x)
                {
                    int pX = (int)((float)x / largeSize * smallSize);
                    int pZ = (int)((float)z / largeSize * smallSize);

                    largeData[z * largeSize + x] = smallData[pZ * smallSize + pX];
                }
            }

            return largeData;
        }

        public virtual int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ, int step, int flags)
        {
            int[] outData = new int[sizeX * sizeZ / step];

            int? li = null;
            for (int z = 0; z < sizeZ; ++z)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    int ssX = sizeX / step;
                    int ssZ = sizeZ / step;

                    int lx = (int)((float)x / sizeX * ssX);
                    int lz = (int)((float)z / sizeZ * ssZ);

                    int li2 = lz * ssX + lx;

                    if (li2 == li) continue;

                    li = li2;

                    outData[li ?? 0] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                }
            }

            return outData;
        }

        public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            int[] outData = new int[sizeX * sizeZ];
            
            int flags = 0b1010001;

            if (thresholds != null)
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        outData[z * sizeX + x] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                    }
                }
            }
            else
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    for (int x = 0; x < sizeX; ++x)
                    {
                        outData[z * sizeX + x] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                    }
                }
            }

            return outData;
        }

        public int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ, double[] thresholds)
        {
            int[] outData = new int[sizeX * sizeZ];

            for (int z = 0; z < sizeZ; ++z)
            {
                for (int x = 0; x < sizeX; ++x)
                {
                    int flags = 0b10001;
                    outData[z * sizeX + x] = GetRGBANoise(xCoord, x, zCoord, z, flags, thresholds);
                }
            }

            return outData;
        }
    }
}