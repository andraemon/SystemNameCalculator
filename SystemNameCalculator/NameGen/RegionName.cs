﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SystemNameCalculator.Utils;

namespace SystemNameCalculator.NameGen
{
    public static class RegionName
    {
        public static void FormatName(byte[] seed)
        {
            byte[][] register = new byte[2][];
            byte[][] cache0 = new byte[2][];
            byte[][] cache1 = new byte[3][];
            string name;

            cache1[0] = new byte[] { 0x00 };
            cache1[1] = new byte[] { 0x06 };

            register[0] = seed.Shr(4);
            register[0][0] /= 2;
            register[0] = seed.Xor(register[0]).Multiply(new byte[] { 0xD7, 0x31, 0xBD, 0x2C, 0x48, 0x81, 0xDD, 0x64 });
            Array.Resize(ref register[0], 8);
            register[0] = BitConverter.GetBytes(BitConverter.ToInt32(register[0].Shr(4)) / 2).Xor(register[0])
                .Multiply(new byte[] { 0x97, 0x29, 0x61, 0x13, 0xC6, 0xA5, 0x6A, 0xE3});
            Array.Resize(ref register[0], 8);
            register[0] = BitConverter.GetBytes(BitConverter.ToInt32(register[0].Shr(4)) / 2).Xor(register[0]);

            cache0[0] = register[0].Shl(4);
            cache0[1] = register[0].Shl(4).Rol(2).Xor(register[0].Shr(4)).Xor(register[0].Shl(4));

            if (BitConverter.ToInt32(register[0].Shl(4)) == 0) cache0[0] = cache0[0].Add(new byte[] { 0x01 });

            cache0 = cache0.UpdateSeed();
            cache1[2] = cache0[0].Multiply(new byte[] { 0x04 }).Shr(4).Add(cache1[1]);

            Logging.PrintDebug($"Cache1: {cache1[0].Format()} // {cache1[1].Format()} // {cache1[2].Format()}");

            name = Generator.GenerateName(ref cache0, ref cache1);
            name = char.ToUpper(name[0]) + name[1..];

            Logging.PrintDebug(name);
        }

        public static byte[] RegionCoordsToByteArray(this string self)
        {
            string result;
            string[] coords = self.Split(':');

            if (coords.Length != 4)
            {
                Logging.PrintError("\nCould not parse coordinates!\n");
                Logging.PrintError("\nDetails: Wrong number of arguments\n");
                return null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!short.TryParse(coords[i].ToLower(), NumberStyles.HexNumber, null, out short sh))
                {
                    Logging.PrintError("\nCould not parse coordinates!\n");
                    Logging.PrintError($"\nDetails: Could not convert coordinate {i + 1} to short\n");
                    return null;
                }

                if (i == 1) sh -= 127;
                else if (i % 2 == 0) sh -= 2047;

                coords[i] = sh.ShortToFormattedHex(Misc.GetRegionTruncation(i));
                if (Program.Debug) Logging.PrintDebug($"Dec: {sh} // Hex: {coords[i]}");
            }

            result = string.Concat(coords[3], coords[1], coords[2], coords[0]);

            if (Program.Debug) Logging.PrintDebug(new string[] { $"GRF: {result}" });

            return result.Parse();
        }

        public static byte[] RegionXCoordsToByteArray(this string[] coords)
        {
            string result;
            Logging.PrintDebug(coords.Length.ToString());
            coords = coords.Skip(2).ToArray();
            if (coords.Length != 4)
            {
                Logging.PrintError("\nCould not parse coordinates!\n");
                Logging.PrintError("\nDetails: Wrong number of arguments\n");
                return null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!short.TryParse(coords[i], out short sh))
                {
                    Logging.PrintError("\nCould not parse coordinates!\n");
                    Logging.PrintError($"\nDetails: Could not convert coordinate {i + 1} to short\n");
                    return null;
                }
                coords[i] = sh.ShortToFormattedHex(Misc.GetRegionTruncation(i));
                if (Program.Debug) Logging.PrintDebug($"Dec: {sh} // Hex: {coords[i]}");
            }

            result = string.Concat(coords[3], coords[1], coords[2], coords[0]);

            if (Program.Debug) Logging.PrintDebug(new string[] { $"GRF: {result}" });

            return result.Parse();
        }
    }
}