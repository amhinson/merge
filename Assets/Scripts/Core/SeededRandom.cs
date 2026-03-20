using System;

namespace MergeGame.Core
{
    /// <summary>
    /// Deterministic random number generator using a seed.
    /// Uses xorshift128 for cross-platform determinism (System.Random is not guaranteed
    /// to produce the same sequence across .NET versions).
    /// </summary>
    public class SeededRandom
    {
        private uint state0;
        private uint state1;
        private uint state2;
        private uint state3;

        public SeededRandom(int seed)
        {
            // Initialize state from seed using splitmix32
            uint s = (uint)seed;
            state0 = SplitMix32(ref s);
            state1 = SplitMix32(ref s);
            state2 = SplitMix32(ref s);
            state3 = SplitMix32(ref s);

            // Ensure state is never all zeros
            if ((state0 | state1 | state2 | state3) == 0)
                state0 = 1;
        }

        private static uint SplitMix32(ref uint state)
        {
            state += 0x9E3779B9;
            uint z = state;
            z ^= z >> 16;
            z *= 0x85EBCA6B;
            z ^= z >> 13;
            z *= 0xC2B2AE35;
            z ^= z >> 16;
            return z;
        }

        private uint Next()
        {
            uint t = state3;
            t ^= t << 11;
            t ^= t >> 8;
            state3 = state2;
            state2 = state1;
            state1 = state0;
            t ^= state0;
            t ^= state0 >> 19;
            state0 = t;
            return t;
        }

        /// <summary>Returns int in [0, max) exclusive.</summary>
        public int Range(int max)
        {
            if (max <= 0) return 0;
            return (int)(Next() % (uint)max);
        }

        /// <summary>Returns int in [min, max) exclusive.</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + Range(max - min);
        }

        /// <summary>Returns float in [0, 1).</summary>
        public float NextFloat()
        {
            return (Next() & 0x7FFFFFFF) / (float)0x80000000;
        }

        /// <summary>Generate a seed from a date string.</summary>
        public static int SeedFromDate(string dateStr)
        {
            // Use a simple hash for determinism
            int hash = 17;
            foreach (char c in dateStr)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
}
