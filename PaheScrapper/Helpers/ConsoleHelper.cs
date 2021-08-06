using System;

namespace PaheScrapper.Helpers
{
    public class ConsoleHelper
    {
        public static void LogCritical(string text)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("Criticals: " + text);
        }

        public static void LogError(string text)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("Error: " + text);
        }

        public static void LogBranch(string text)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("Branch: " + text);
        }

        public static void LogStats(string text)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("Stats: " + text);
        }

        public static void LogInfo(string text)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("Info: " + text);
        }

        public static string LogInput(string text)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(text);
            return Console.ReadLine();
        }

        public static void LogStorage(string text)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("Storage: " + text);
        }

        public static void LogCommandHandled(string text)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(text);
            LogWriter lw = new LogWriter("CommandHandled: " + text);
        }
    }
}