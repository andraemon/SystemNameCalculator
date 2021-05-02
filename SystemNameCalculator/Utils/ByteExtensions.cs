using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SystemNameCalculator.NameGen;

namespace SystemNameCalculator.Utils
{
    public static class ByteExtensions
    {
        public static string Format(this byte[] arr, bool littleEndian = true)
        {
            string result = "";
            if (arr == null || arr.Length < 1) return "Bad array";
            for (int i = 0; i < arr.Length; i++)
            {
                if (littleEndian) result += arr[^(i + 1)].ToString("X2");
                else result += arr[i].ToString("X2");
            }
            return result;
        }

        public static byte[] Parse(this string val, bool littleEndian = true)
        {
            List<byte> result = new List<byte>();
            if (val.Length % 2 != 0) val = "0" + val;
            for (int i = 0; i < val.Length; i += 2)
            {
                if (littleEndian) result.Add(Convert.ToByte(val[^(i + 2)..^i], 16)); 
                else result.Add(Convert.ToByte(val[i..(i + 2)]));
            }

            return result.ToArray();
        }

        private static string ParseTest(this string val, bool littleEndian = true)
        {
            string result = "";
            if (val.Length % 2 != 0) val = "0" + val;
            for (int i = 0; i < val.Length; i += 2)
            {
                if (littleEndian) result += val[^(i + 2)..^i];
                else result += val[i..(i + 2)];
            }

            return result;
        }

        public static byte[] FormatShort(this byte[] op1)
        {
            List<byte> result = new List<byte>(op1);
            if (op1.Length < 2)
            {
                for (int i = 0; i < 2 - op1.Length; i++) result.Add(0x00);
            }

            return result.ToArray();
        }

        #region Arithmetic Operations
        public static byte[] Multiply(this byte[] op1, byte[] op2)
        {
            List<byte> result = new List<byte>();

            byte rem, res;
            int idx = 0;
            for (int i = 0; i < op1.Length; i++)
            {
                rem = 0;
                for (int j = 0; j < op2.Length; j++)
                {
                    short prd = (short)(op1[i] * op2[j] + rem);
                    rem = (byte)(prd >> 8);
                    res = (byte)prd;
                    idx = i + j;

                    if (idx < (result.Count))
                        result = res.Add(result, idx);
                    else result.Add(res);
                }
                if (rem > 0)
                    if (idx + 1 < (result.Count))
                        result = rem.Add(result, idx + 1);
                    else result.Add(rem);
            }

            return result.ToArray();
        }

        public static byte[] Add(this byte[] op1, byte[] op2)
        {
            List<byte> result = new List<byte>(op2);
            for (int i = 0; i < op1.Length; i++)
            {
                result = op1[i].Add(result, i);
            }

            return result.ToArray();
        }

        private static List<byte> Add(this byte op1, List<byte> op2, int ind)
        {
            if (ind < op2.Count)
            {
                byte rem;
                short sum = (short)(op1 + op2[ind]);
                op2[ind] = (byte)(sum & 0xFF);
                rem = (byte)(sum >> 8);
                if (rem != 0)
                {
                    rem.Add(op2, ind + 1);
                }
            }
            else op2.Add(op1);

            return op2;
        }
        public static byte[] Sub(this byte[] op1, byte[] op2)
        {
            List<byte> result = new List<byte>(op2);
            for (int i = 0; i < op1.Length; i++)
            {
                result = op1[i].Sub(result, i);
            }

            return result.ToArray();
        }

        private static List<byte> Sub(this byte op1, List<byte> op2, int ind)
        {
            if (ind < op2.Count)
            {
                byte rem;
                short sum = (short)(op1 - op2[ind]);
                op2[ind] = (byte)(sum & 0xFF);
                rem = (byte)(sum >> 8);
                if (rem != 0)
                {
                    rem.Sub(op2, ind + 1);
                }
            }
            else op2.Add(op1);

            return op2;
        }
        #endregion

        #region Bitwise Operations
        public static byte[][] UpdateSeed(this byte[][] cache, int move = 1)
        {
            for (int i = 0; i < move; i++)
            {
                byte[] result = cache[0].Multiply(Generator.Multiplier).Add(cache[1]);
                cache[0] = result.Shl(4);
                cache[1] = result.Shr(4);
                Logging.PrintDebug($"Cache0: {cache[0].Format()} // {cache[1].Format()}");
            }

            return cache;
        }

        public static byte[] Sxd(this byte[] op1, int extend)
        {
            List<byte> result = new List<byte>(op1);
            byte val = 0x00;
            if (op1.Length >= extend) return op1;
            if ((op1[^1] >> 7) == 1) val = 0xFF;
            for (int i = 0; i < extend - op1.Length; i++)
            {
                result.Add(val);
            }

            return result.ToArray();
        }

        public static byte[] Zxd(this byte[] op1, int extend)
        {
            List<byte> result = new List<byte>(op1);
            if (op1.Length >= extend) return op1;
            for (int i = 0; i < extend - op1.Length; i++)
            {
                result.Add(0x00);
            }

            return result.ToArray();
        }

        public static byte[] Shr(this byte[] op1, int shift)
        {
            if (op1.Length > shift) return op1[shift..];
            else return new byte[] { 0x00 };
        }

        public static byte[] Rol(this byte[] op1, int roll)
        {
            if (roll < op1.Length) return op1[roll..].Concat(op1[..^(op1.Length - roll)]).ToArray();
            else if (roll == op1.Length) return op1;
            return op1[(roll % op1.Length)..].Concat(op1[..^(op1.Length - (roll % op1.Length))]).ToArray();
        }

        public static byte[] Shl(this byte[] op1, int shift)
        {
            if (op1.Length > shift) return op1[..shift];
            else return new byte[] { 0x00 };
        }

        #region Logical Operators
        public static byte[] Xor(this byte[] op1, byte[] op2)
        {
            return op1.GenericLogical(op2, LogicalOperators.Xor);
        }
        public static byte[] And(this byte[] op1, byte[] op2)
        {
            return op1.GenericLogical(op2, LogicalOperators.And);
        }
        public static byte[] Or(this byte[] op1, byte[] op2)
        {
            return op1.GenericLogical(op2, LogicalOperators.Or);
        }

        public static byte[] GenericLogical(this byte[] op1, byte[] op2, LogicalOperators op)
        {
            List<byte> manlet;
            List<byte> chungus;
            if (op1.Length > op2.Length)
            {
                manlet = new List<byte>(op2);
                chungus = new List<byte>(op1);
                for (int i = 0; i < op1.Length - op2.Length; i++)
                {
                    manlet.Add(0x00);
                }
            }
            else
            {
                manlet = new List<byte>(op1);
                chungus = new List<byte>(op2);
                for (int i = 0; i < op2.Length - op1.Length; i++)
                {
                    manlet.Add(0x00);
                }
            }

            switch (op)
            {
                case LogicalOperators.Xor:
                    for (int i = 0; i < chungus.Count; i++)
                    {
                        chungus[i] = (byte)(chungus[i] ^ manlet[i]);
                    }
                    return chungus.ToArray();
                case LogicalOperators.And:
                    for (int i = 0; i < chungus.Count; i++)
                    {
                        chungus[i] = (byte)(chungus[i] & manlet[i]);
                    }
                    return chungus.ToArray();
                case LogicalOperators.Or:
                    for (int i = 0; i < chungus.Count; i++)
                    {
                        chungus[i] = (byte)(chungus[i] | manlet[i]);
                    }
                    return chungus.ToArray();
                default: throw new NotImplementedException("The specified logical operator is not implemented!");
            }
        }

        public enum LogicalOperators
        {
            And,
            Or,
            Xor
        }
        #endregion
        #endregion
    }
}
