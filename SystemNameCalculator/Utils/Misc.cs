using System;
using System.Collections.Generic;
using System.Text;

namespace SystemNameCalculator.Utils
{
    internal static class Misc
    {
        internal static int GetSystemTruncation(int i)
        {
            if (i == 1 || i == 4) return 2;
            else return 3;
        }

        internal static int GetRegionTruncation(int i)
        {
            if (i == 1 || i == 3) return 2;
            else return 3;
        }
    }
}
