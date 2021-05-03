using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SystemNameCalculator.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Globalization;

namespace SystemNameCalculator.NameGen
{
    // 1618 2 -687 457 0
    // 1C90002D51652
    // 850003D51653
    // 0364:0078:0CE5:010A
    public static class SystemName
    {
        public static void FormatName(byte[] seed)
        {
            byte[] galacticCoords = seed.Shl(4);
            byte[] systemIndex = seed.Shr(4);
            byte[][] register = new byte[2][];
            byte[][] cache0 = new byte [2][];
            byte[][] cache1 = new byte[3][];
            string name;

            cache1[1] = new byte[] { 0x06 };

            Logging.PrintDebug(new string[] { seed.Format(), galacticCoords.Format(), systemIndex.Format() });

            //Steps 1-6
            if (BitConverter.ToInt32(galacticCoords) == 0) register[0] =
                    galacticCoords.Add(new byte[] { 0x01 }).Multiply(Generator.Multiplier).Add(galacticCoords.Rol(2).Xor(galacticCoords.Xor(systemIndex)));
            else register[0] = galacticCoords.Multiply(Generator.Multiplier).Add(galacticCoords.Rol(2).Xor(galacticCoords.Xor(systemIndex)));

            //Step 8
            register[1] = register[0].Shl(4).Multiply(new byte[] { 0x5 }).Shr(4);
            if (register[1].Length == 0) cache1[0] = new byte[] { 0x02 };
            else
            {
                if (register[1][0] < 4) cache1[0] = new byte[] { (byte)(register[1][0] + 0x02) };
                else cache1[0] = new byte[] { 0x07 };
            }

            //Steps 9-12
            register[1] = register[0].Shl(4).Multiply(Generator.Multiplier).Add(register[0].Shr(4));
            cache0[0] = register[1].Shl(4);
            cache1[2] = cache0[0].Multiply(new byte[] { 0x04 }).Shr(4).Add(new byte[] { 0x06 });

            //Steps 13-14
            cache0[1] = register[1].Shr(4);

            Logging.PrintDebug($"Cache0: {cache0[0].Format()} // {cache0[1].Format()}");
            Logging.PrintDebug($"Cache1: {cache1[0].Format()} // {cache1[1].Format()} // {cache1[2].Format()}");

            name = Generator.GenerateName(ref cache0, ref cache1);
            name = char.ToUpper(name[0]) + name[1..];

            Logging.PrintDebug(name);
        }

        #region Command Parsers
        public static byte[] SystemCoordsToByteArray(this string self)
        {
            string result;
            string[] coords = self.Split(':');

            if (coords.Length != 5)
            {
                Logging.PrintError("\nCould not parse coordinates!");
                Logging.PrintError("Details: Wrong number of arguments\n");
                return null;
            }

            for (int i = 0; i < 5; i++)
            {
                if (!short.TryParse(coords[i].ToLower(), NumberStyles.HexNumber, null, out short sh))
                {
                    Logging.PrintError("\nCould not parse coordinates!");
                    Logging.PrintError($"Details: Could not convert coordinate {i + 1} to short\n");
                    return null;
                }
                switch (i)
                {
                    case 0:
                    case 2:
                        sh -= 2047;
                        break;
                    case 1:
                        sh -= 127;
                        break;
                    default: break;
                }
                coords[i] = sh.ShortToFormattedHex(Misc.GetSystemTruncation(i));
                if (Program.Debug) Logging.PrintDebug($"Dec: {sh} // Hex: {coords[i]}");
            }
            result = string.Concat(coords[3], coords[4], coords[1], coords[2], coords[0]);
            if (Program.Debug) Logging.PrintDebug(new string[] { $"GRF: {result}" });

            return result.Parse();
        }

        public static byte[] SystemXCoordsToByteArray(this string[] coords)
        {
            string result;
            coords = coords.Skip(2).ToArray();
            if (coords.Length != 5)
            {
                Logging.PrintError("\nCould not parse coordinates!");
                Logging.PrintError("Details: Wrong number of arguments\n");
                return null;
            }

            for (int i = 0; i < 5; i++)
            {
                if (!short.TryParse(coords[i], out short sh))
                {
                    Logging.PrintError("\nCould not parse coordinates!");
                    Logging.PrintError($"Details: Could not convert coordinate {i + 1} to short\n");
                    return null;
                }
                coords[i] = sh.ShortToFormattedHex(Misc.GetSystemTruncation(i));
                Logging.PrintDebug($"Dec: {sh} // Hex: {coords[i]}");
            }
            result = string.Concat(coords[3], coords[4], coords[1], coords[2], coords[0]);
            Logging.PrintDebug(new string[] { $"GRF: {result}" });

            return result.Parse();
        }
        #endregion
    }
}
