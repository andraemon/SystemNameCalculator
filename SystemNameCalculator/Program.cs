using System;
using System.Diagnostics;
using SystemNameCalculator.Utils;
using SystemNameCalculator.NameGen;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace SystemNameCalculator
{
    public static class Program
    {
        internal static bool Debug = true;

        public static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("NMS System Name Calculator\r");
            Console.WriteLine("Built for game version 3.37 (build ID 6546017)\r");
            Console.WriteLine("--------------------------\r");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Commands:\n");
            Console.WriteLine("    /system <command> <arguments>\n");
            Console.WriteLine("        coords <string> - Finds the name of a system from its galactic coordinates, as seen in the signal booster.");
            Console.WriteLine("            Format is XXXX:YYYY:ZZZZ:SYST:GLXY, where the signal booster has the format STRING:XXXX:YYYY:ZZZZ:SYST.\n");
            Console.WriteLine("        xcoords <x> <y> <z> <systemindex> <galaxyindex> - Finds the name of a system from its galactic coordinates,");
            Console.WriteLine("            as seen in the save file.\n");
            Console.WriteLine("        find <string> - Attempts to find coordinates for a system with the given name. Not implemented yet.\n");
            Console.WriteLine("    /region <command> <arguments>\n");
            Console.WriteLine("        coords <string> - Finds the name of a region from its galactic coordinates, as seen in the signal booster.");
            Console.WriteLine("            Format is XXXX:YYYY:ZZZZ:GLXY, where the signal booster has the format STRING:XXXX:YYYY:ZZZZ:SYST.\n");
            Console.WriteLine("        xcoords <x> <y> <z> <galaxyindex> - Finds the name of a region from its galactic coordinates,");
            Console.WriteLine("            as seen in the save file.\n");
            Console.WriteLine("        find <string> - Attempts to find coordinates for a region with the given name. Not Implemented yet.\n");
            Console.WriteLine("    /debug - Toggles debug output.\n");
             
        input:
            Console.ForegroundColor = ConsoleColor.White;
            Logging.Print("\nWhat is your command?\n");
            Console.ReadLine().ParseCommand();
            goto input;
        }

        internal static void ParseCommand(this string self)
        {
            string[] vars = self.Split();

            if (vars[0] == "/system")
            {
                if (vars[1] == "find")
                {
                    Logging.Print("\nPlaceholder\n");
                }
                else if (vars[1] == "coords")
                {
                    Console.WriteLine();
                    byte[] seed = vars[2].SystemCoordsToByteArray();
                    if (seed != null) SystemName.FormatName(seed);
                }
                else if (vars[1] == "xcoords")
                {
                    Console.WriteLine();
                    byte[] seed = vars.SystemXCoordsToByteArray();
                    if (seed != null) SystemName.FormatName(seed);
                }
                else if (vars[1] == "grfcoords")
                {
                    Console.WriteLine();
                    SystemName.FormatName(vars[2].Parse());
                }
                else
                {
                    Logging.PrintError("\nCould not parse input! Are you sure you typed the command correctly?\n");
                }
            }
            else if (vars[0] == "/region")
            {
                if (vars[1] == "find")
                {
                    Logging.Print("\nPlaceholder\n");
                }
                else if (vars[1] == "coords")
                {
                    Console.WriteLine();
                    byte[] seed = vars[2].RegionCoordsToByteArray();
                    if (seed != null) RegionName.FormatName(seed);
                }
                else if (vars[1] == "xcoords")
                {
                    Console.WriteLine();
                    byte[] seed = vars.RegionXCoordsToByteArray();
                    if (seed != null) RegionName.FormatName(seed);
                }
                else if (vars[1] == "grfcoords")
                {
                    Console.WriteLine();
                    RegionName.FormatName(vars[2].Parse());
                }
                else
                {
                    Logging.PrintError("\nCould not parse input! Are you sure you typed the command correctly?\n");
                }
            }
            else if (vars[0] == "debug")
            {
                Debug = !Debug;
                Logging.Print("Toggled debug mode!");
            }
            else
            {
                Logging.PrintError("\nCould not parse input! Are you sure you typed the command correctly?\n");
            }
        }
    }
}
