using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;
using SystemNameCalculator.Utils;
using Newtonsoft.Json;

namespace SystemNameCalculator.NameGen
{
    public static class ReverseSearch
    {
        #region Region Methods
        public static void FindRegionSeeds(string name, uint galaxy, int cap = 1)
        {
            List<ulong> seedList = ConstructRegionRanges(name, cap);
            if (seedList == null)
            {
                Logging.Print("Could not find a region with the given name. Sorry!");
                return;
            }
            Logging.PrintDebug($"Searching for region with name {name} in galaxy {galaxy} ({galaxy:X})");

            for (int i = 0; i < seedList.Count; i++)
            {
                Logging.Print($"Seed {i + 1}");
                Logging.Print($"    Signal Booster Coords: {StringExtensions.FormatRegionBoosterCoords((seedList[i] ^ galaxy) + (galaxy * 0x100000000u))}");
                Logging.Print($"    Save File Coords: {StringExtensions.FormatRegionXCoords((seedList[i] ^ galaxy) + (galaxy * 0x100000000u))}");
                Logging.Print($"    Portal Coords: {StringExtensions.FormatRegionGrfCoords((seedList[i] ^ galaxy) + (galaxy * 0x100000000u))}");
            }
        }

        public static List<ulong> ConstructRegionRanges(string name, int cap)
        {
            string origName = name;
            int adorn = -1;
            List<List<WeightData>> weights = new List<List<WeightData>>();
            List<SeedRange> ranges = new List<SeedRange>();
            Random r = new Random();
            string[] split = name.Split();

            for (int i = 0; i < RegionName.ProcAdornments.Length; i++)
            {
                if (split.Length == 2 && RegionName.ProcAdornments[i].Split()[1] == split[1])
                {
                    name = split[0];
                    adorn = i;
                }
                else if (split.Length > 2 && RegionName.ProcAdornments[i].Split()[0] == split[0])
                {
                    name = split[^1];
                    adorn = i;
                }
            }

            name = name.ToLower();
            Logging.PrintDebug($"name: {name}, adorn: {adorn}");
            Logging.PrintDebug($"{name.Split().Length}, {name.Length > 9}, {name.Length < 6}, {Generator.VowelInsertedAtStart(name[0], name[1])}, {Generator.VowelInsertedAtEnd(name[^1], name[^2])}, {Generator.GetConsecutiveConsonants(name) != -1}");

            if (name.Split().Length != 1
                || name.Length > 9
                || name.Length < 6
                || Generator.VowelInsertedAtStart(name[0], name[1])
                || Generator.VowelInsertedAtEnd(name[^1], name[^2])
                || Generator.GetConsecutiveConsonants(name) != -1)
                return null;

            Logging.PrintDebug("name format is correct");

            weights = GetWeightsForName(name, new byte[] { 0 });
            if (weights[0] == null) return null;

            ranges.Add(new SeedRange(new List<(uint, uint)>(), new List<(uint, uint)> { (0, 0xFFFFFFFF) }, new List<int>()));
            for (int i = 0; i <= 9 - name.Length; i++)
            {
                ranges[0].BotRange.Add(((uint)((3 - i) * 0x100000000 / 4),
                    (uint)((((3 - i) * 0x100000000) + 0xFFFFFFFF) / 4)));
                ranges[0].Updates.Add(1);
            }
            ranges[0].LinkOffset = 2;

            ranges.AddRange(GetNameGenSeedRanges(weights, name, 6, (name.Length, 9), false));

            if (adorn == -1) ranges.Add(new SeedRange(new List<(uint, uint)> { (0xCCCCCCCD, 0xFFFFFFFF) }, new List<(uint, uint)> { (0, 0xFFFFFFFF) }, new List<int> { 1 }));
            else
            {
                ranges.Add(new SeedRange(new List<(uint, uint)> { (0, 0xCCCCCCCC) }, new List<(uint, uint)> { (0, 0xFFFFFFFF) }, new List<int> { 1 }));
                ranges.Add(new SeedRange(new List<(uint, uint)> { ((uint)(adorn * 0x100000000 / 20), (uint)(((adorn * 0x100000000) + 0xFFFFFFFF) / 20)) }, 
                    new List<(uint, uint)> { (0, 0xFFFFFFFF) }, new List<int> { 1 }));
            }

            for (int i = 0; i < ranges.Count; i++)
            {
                Logging.PrintDebug($"RANGE {i + 1}");
                Logging.PrintDebug("    BotRanges");
                for (int j = 0; j < ranges[i].BotRange.Count; j++) 
                    Logging.PrintDebug($"        Min: {ranges[i].BotRange[j].Item1:X}, Max: {ranges[i].BotRange[j].Item2:X}, Updates: {ranges[i].Updates[j]}");
                Logging.PrintDebug($"    LinkOffset: {ranges[i].LinkOffset}");
            }

            return ExhaustiveRegionSearch(ranges, cap, origName);
        }

        // Alternative to smart cracker, actually pretty performant so we'll stick with this for now
        internal static List<ulong> ExhaustiveRegionSearch(List<SeedRange> ranges, int cap, string name)
        {
            List<ulong> result = new List<ulong>();
            ulong seed;
            uint bot;
            uint i = 0;

            while (true)
            {
                seed = unchecked(i * 0x64DD81482CBD31D7u);
                seed = unchecked((((seed >> 32) / 2) ^ seed) * 0xE36AA5C613612997u);
                seed = ((seed >> 32) / 2) ^ seed;
                bot = (uint)(seed & 0xFFFFFFFF);
                seed = unchecked(bot + (BitOperations.RotateLeft(bot, 16) ^ (seed >> 32) ^ bot) * 0x100000000);
                if (bot == 0) seed++;
                seed.UpdateSeed();

                if (TrySeed(seed, ranges) && RegionName.FormatName(BitConverter.GetBytes(i)) == name) result.Add(i);
                if (result.Count == cap || i == 0xFFFFFFFF) break;

                i++;
            }

            return result;
        }
        #endregion

        #region General Methods
        public static List<SeedRange> GetNameGenSeedRanges(List<List<WeightData>> data, string name, int constant, (int, int) add, bool alternate)
        {
            List<SeedRange> result = new List<SeedRange>();

            for (int i = 0; i < data[0].Count + 2; i++) result.Add(new SeedRange(new List<(uint, uint)>(), new List<(uint, uint)> { (0, 0xFFFFFFFF) }, new List<int>()));

            for (int i = 0; i < data.Count; i++)
            {
                ulong seedbase = (ulong)(AlphasetStringIndex(name[..3], data[i][0].Alphaset) / 3 * 0x100000000);
                ulong divisor = (ulong)(Generator.Alphasets[data[i][0].Alphaset].Length / 3);
                result[0].BotRange.Add(((uint)(seedbase / divisor), (uint)((seedbase + 0xFFFFFFFF) / divisor)));
                result[0].Updates.Add(2);
            }
            result[0].LinkOffset = 2;

            for (int i = 0; i <= add.Item2 - add.Item1; i++)
            {
                result[1].BotRange.Add(((uint)((name.Length - constant) * 0x100000000 / (add.Item2 - constant - i + 1)),
                    (uint)((((name.Length - constant) * 0x100000000) + 0xFFFFFFFF) / (add.Item2 - constant - i + 1))));
                result[1].Updates.Add(1);
            }

            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < data[i].Count; j++)
                {
                    if (j == data[i].Count - 1)
                    {
                        result[j + 2].Updates.Add(1);
                        result[j + 2].LinkOffset = 0;
                    }
                    else
                    {
                        result[j + 2].Updates.Add(((data[i][j + 1].Alphaset - data[i][j].Alphaset + 8) % 8) + 1);
                        result[j + 2].LinkOffset = 1;
                    }

                    if (alternate)
                    {
                        result[j + 2].BotRange.Add(((uint)((data[i][j].Index - 0.5) / (data[i][j].Weights.Count - 1) / TinyDouble),
                                (uint)((data[i][j].Index + 0.5) / (data[i][j].Weights.Count - 1) / TinyDouble)));   
                    }
                    else
                    {
                        float sum = 0;
                        for (int k = 0; k <= data[i][j].Index; k++) sum += (float)data[i][j].Weights[k].Item2;
                        result[j + 2].BotRange.Add(((uint)((sum - data[i][j].Weights[data[i][j].Index].Item2) / TinyDouble), 
                                (uint)(sum / TinyDouble)));
                    }
                }
            }

            return result;
        }

        public static bool TrySeed(ulong seed, List<SeedRange> ranges)
        {
            List<(int, int)> linkInfo = new List<(int, int)>();
            uint bot;
            int index;
            int linkIndex;

            for (int i = 0; i < ranges.Count; i++)
            {
                bot = (uint)(seed & 0xFFFFFFFF);
                if ((linkIndex = linkInfo.FindIndex(x => x.Item1 == i)) != -1)
                {
                    index = linkInfo[linkIndex].Item2;
                    linkInfo.RemoveAt(linkIndex);
                }
                else index = ranges[i].BotRange.FindIndex(x => bot >= x.Item1 && bot <= x.Item2);
                if (ranges[i].LinkOffset != 0) linkInfo.Add((i + ranges[i].LinkOffset, index));
                if (index == -1 || !(bot >= ranges[i].BotRange[index].Item1 && bot <= ranges[i].BotRange[index].Item2)) return false;
                for (int j = 0; j < ranges[i].Updates[index]; j++) seed.UpdateSeed();
            }

            return true;
        }

        public static int AlphasetStringIndex(string str, int set)
        {
            for (int i = 0; i < Generator.Alphasets[set].Length; i += 3)
            {
                if (str == Generator.Alphasets[set][i..(i + 3)]) return i;
            }

            return -1;
        }

        public static List<List<WeightData>> GetWeightsForName(string name, byte[] alphasets)
        {
            int loop = name.Length - 3;
            List<List<WeightData>> result = new List<List<WeightData>>();
            (int, WeightData)[,] graph = new (int, WeightData)[8, loop];

            for (byte i = 0; i < alphasets.Length; i++)
            {
                List<(char, double)> start = Generator.GetStringWeights(name[..3], alphasets[i]);
                int index;
                if (start == null || (index = start.FindIndex(x => x.Item1 == name[3])) == -1)
                {
                    result.Add(null);
                    continue;
                }
                else result.Add(new List<WeightData> { new WeightData(start, alphasets[i], index) });

                int prev = alphasets[i], tries = 8;

                for (byte j = 1; j < loop; j++)
                {
                    if (result[i] == null) break;
                    for (byte k = 0; k < tries; k++)
                    {
                        byte loc = (byte)((k + prev) % 8);
                        if (graph[loc, j] == default)
                        {
                            List<(char, double)> weights = Generator.GetStringWeights(name[j..(j + 3)], loc);
                            if (weights == null)
                            {
                                graph[loc, j] = (-1, null);
                                if (k == tries - 1)
                                {
                                    result[i] = null;
                                    break;
                                }
                            }
                            else if ((index = weights.FindIndex(x => x.Item1 == name[j + 3])) != -1)
                            {
                                graph[loc, j] = (1, new WeightData(weights, loc, index));
                                result[i].Add(graph[loc, j].Item2);
                                prev = loc;
                                tries -= k;
                                break;
                            }
                            else
                            {
                                graph[loc, j] = (-2, null);
                                result[i] = null;
                                break;
                            }
                        }
                        else if (graph[loc, j].Item2 == null)
                        {
                            if (graph[loc, j].Item1 == -1)
                            {
                                if (k == tries - 1)
                                {
                                    result[i] = null;
                                    break;
                                }
                            }
                            else
                            {
                                result[i] = null;
                                break;
                            }
                        }
                        else
                        {
                            result[i].Add(graph[loc, j].Item2);
                            prev = loc;
                            tries -= k;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        // Doesn't work (for reasons which are admittedly obvious in retrospect) so we're just going with an exhaustive search for now
        public static (uint, uint) TopDownCracker(List<SeedRange> ranges)
        {
            List<(int, int)> linkInfo = new List<(int, int)>();
            (uint, uint) seed;
            Random r = new Random();
            (int, int) link;
            uint top;
            ulong prev;
            int index = r.Next(0, ranges[^1].BotRange.Count), nextIndex = (ranges[^1].LinkOffset == 1) ? index : r.Next(0, ranges[^2].BotRange.Count);

            seed = (r.NextUint(ranges[^1].BotRange[index].Item1, ranges[^1].BotRange[index].Item2 + 1),
                r.NextUint(ranges[^2].BotRange[nextIndex].Item1, ranges[^2].BotRange[nextIndex].Item2 + 1) * 0x5A76F899 >> 32);
            if (ranges[^1].LinkOffset > 1) linkInfo.Add((ranges[^1].LinkOffset + 1, index));
            Logging.PrintDebug($"{seed.Item1:X}, {seed.Item2:X}");

            for (int i = 2; i < ranges.Count; i++)
            {
                if (ranges[^i].LinkOffset > 1) 
                { 
                    linkInfo.Add((ranges[^i].LinkOffset + i, nextIndex)); 
                    nextIndex = r.Next(0, ranges[^(i + 1)].BotRange.Count);
                }
                else if ((link = linkInfo.FirstOrDefault(x => x.Item1 == i + 1)).Item1 == i + 1) nextIndex = link.Item2;
                else if (ranges[^i].LinkOffset == 0) nextIndex = r.Next(0, ranges[^(i + 1)].BotRange.Count);

                for (int j = 0; j < ranges[^(i + 1)].Updates[nextIndex] - 1; j++)
                {
                    top = r.NextUint(0, 0xFFFFFFFF) * 0x5A76F899 >> 32;
                    prev = (ulong)(seed.Item1 + (seed.Item2 * 0x100000000));
                    seed = ((uint)((prev - top) / 0x5A76F899), top);
                }

                top = r.NextUint(ranges[^(i + 1)].BotRange[nextIndex].Item1, ranges[^(i + 1)].BotRange[nextIndex].Item2 + 1) * 0x5A76F899 >> 32;
                prev = (ulong)(seed.Item1 + (seed.Item2 * 0x100000000));
                seed = ((uint)((prev - top) / 0x5A76F899), top);
            }

            top = r.NextUint(0, 0xFFFFFFFF) * 0x5A76F899 >> 32;
            prev = (ulong)(seed.Item1 + (seed.Item2 * 0x100000000));
            return ((uint)((prev - top) / 0x5A76F899), top);
        }

        // Given one multiplicand and a desired product, attempts to find the other multiplicand, which will have at most as many bits as the first.
        // Always works if the input multiplicand is odd, may not if it's even.
        public static bool TryFindMultiplicand(uint input, uint product, out uint multiplicand)
        {
            multiplicand = 0;
            if (input == 0 && product != 0) return false;
            else if (input == 1)
            {
                multiplicand = product;
                return true;
            }
            else if (product == 0) return true;

            uint runningSum = 0, addend = input % 2;

            for (int i = 0; i < 32; i++)
            {
                uint pow = (uint)Math.Pow(2, i);
                uint inputColumn = runningSum & pow, productColumn = product & pow;

                if ((addend == 1 && (inputColumn ^ productColumn) == pow) || (addend == 0 && (inputColumn == productColumn)))
                {
                    multiplicand = unchecked(multiplicand + pow);
                    runningSum = unchecked(runningSum + input * pow);
                }
                else if (addend == 0) return false;
            }

            return true;
        }

        public static SeedRange CollapseRanges(this SeedRange data)
        {
            data.BotRange = data.BotRange.Collapse();
            data.TopRange = data.TopRange.Collapse();

            return data;
        }

        private static List<(uint, uint)> Collapse(this List<(uint, uint)> ranges)
        {
            List<(uint, bool)> endpoints = new List<(uint, bool)>();
            List<(uint, uint)> groups = new List<(uint, uint)>();
            int le = 0, re = 0;
            uint currentStart;

            for (int i = 0; i < ranges.Count; i++)
            {
                endpoints.Add((ranges[i].Item1, true));
                endpoints.Add((ranges[i].Item2, false));
            }

            endpoints.Sort(new Comparers.UIntBoolTupleComparer());
            currentStart = endpoints[0].Item1;

            for (int i = 0; i < endpoints.Count; i++)
            {
                if (endpoints[i].Item2) le++;
                else re++;

                if (le - re == 0)
                {
                    if (i + 1 != endpoints.Count && endpoints[i].Item1 + 1 != endpoints[i + 1].Item1)
                    {
                        groups.Add((currentStart, endpoints[i].Item1));
                        currentStart = endpoints[i + 1].Item1;
                    }
                    else groups.Add((currentStart, endpoints[i].Item1));
                }
            }

            return groups;
        }
        #endregion

        public class SeedRange
        {
            public List<(uint, uint)> BotRange;

            public List<(uint, uint)> TopRange;

            public List<int> Updates;

            public int LinkOffset;

            public SeedRange(List<(uint, uint)> bot, List<(uint, uint)> top, List<int> updates, int link = 0)
            {
                BotRange = bot;
                TopRange = top;
                Updates = updates;
                LinkOffset = link;
            }
        }

        public class WeightData
        {
            public List<(char, double)> Weights;

            public int Index;

            public byte Alphaset;

            public WeightData(List<(char, double)> weights, byte alphaset, int index)
            {
                Weights = weights;
                Alphaset = alphaset;
                Index = index;
            }
        }

        public const double TinyDouble = 2.3283064370807974E-10;
    }
}
