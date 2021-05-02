using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace SystemNameCalculator.Utils
{
    public static class StringExtensions
    {
        public static string ShortToFormattedHex(this short self, int trunc)
        {
            string s = Convert.ToString(self, 2);
            if (s.Length > 12) s = s[(s.Length - 12)..];
            return Convert.ToInt16(s, 2).ToString($"X{trunc}")[^trunc..];
        }
    }
}
