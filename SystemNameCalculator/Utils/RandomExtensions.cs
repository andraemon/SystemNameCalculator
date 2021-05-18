using System;
using System.Collections.Generic;
using System.Text;

namespace SystemNameCalculator.Utils
{
    public static class RandomExtensions
    {
        public static uint NextUint(this Random random, uint lower, uint upper)
        {
            return (uint)(lower + (upper - lower) * random.NextDouble());
        }

        public static ulong NextUlong(this Random random, ulong lower, ulong upper)
        {
            return (ulong)(lower + (upper - lower) * random.NextDouble());
        }
        public static void UpdateSeed(ref this ulong seed)
        {
            seed = unchecked(((seed & 0xFFFFFFFF) * 0x5A76F899) + (seed >> 32));
        }
    }
}
