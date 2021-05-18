using System;
using System.Collections.Generic;
using System.Text;

namespace SystemNameCalculator.Utils
{
    public class Comparers
    {
        public class UIntBoolTupleComparer : IComparer<(uint, bool)>
        {
            int IComparer<(uint, bool)>.Compare((uint, bool) tuple1, (uint, bool) tuple2)
            {
                if (tuple1.Item2 != tuple2.Item2 && tuple1.Item1 == tuple2.Item1)
                {
                    if (tuple1.Item2) return 1;
                    if (tuple2.Item2) return -1;
                }
                if (tuple1.Item1 < tuple2.Item1) return -1;
                if (tuple1.Item1 > tuple2.Item1) return 1;
                return 0;
            }
        }
    }
}
