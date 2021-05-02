using System;
using System.Collections.Generic;
using System.Text;

namespace SystemNameCalculator.Utils
{
    internal static class Logging
    {
        internal static void Print(string content)
        {
            PrintColor(content, ConsoleColor.Green);
        }
        internal static void PrintDebug(string debug)
        {
            if (Program.Debug) PrintColor(debug, ConsoleColor.DarkGray);
        }

        internal static void PrintDebug(string[] debug)
        {
            if (Program.Debug) foreach (string d in debug) PrintColor(d, ConsoleColor.DarkGray);
        }

        internal static void PrintError(string error)
        {
            PrintColor(error, ConsoleColor.Red);
        }

        internal static void PrintError(string[] error)
        {
            foreach (string e in error) PrintColor(e, ConsoleColor.Red);
        }

        internal static void PrintColor(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(content);
            Console.ForegroundColor = ConsoleColor.White;
        }

        internal static void PrintColor(string[] content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            foreach (string c in content) Console.WriteLine(c);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
