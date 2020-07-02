using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ServerOutput;

namespace SixModLoader
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class Logger
    {
        public static string FilePath => Path.Combine(SixModLoader.Instance.DataPath, "log.txt");

        public static void SafeDeleteFile(string path)
        {
            if (File.Exists(path))
            {
                var index = path.LastIndexOf('.');
                File.AppendAllText($"{path.Substring(0, index)}.old{path.Substring(index)}", File.ReadAllText(path));
                File.Delete(path);
            }
        }

        static Logger()
        {
            SafeDeleteFile(FileLog.logPath);
            SafeDeleteFile(FilePath);
        }

        public static void Log(string message, LogLevel level, ConsoleColor color = ConsoleColor.Gray, Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();
            var name = typeof(Logger).Assembly == assembly ? "SixModLoader" : SixModLoader.Instance.ModManager.GetMod(assembly)?.Info?.Name ?? "UNKNOWN";

            message = $"[{Enum.GetName(typeof(LogLevel), level)?.ToUpper()}] [{name}] {message}";
            if (ServerStatic.ServerOutput == null || !(ServerStatic.ServerOutput is StandardOutput))
            {
                Console.WriteLine(message);
            }

            File.AppendAllText(FilePath, message + "\n");

            if (ServerStatic.ServerOutput != null)
            {
                ServerConsole.AddLog(message, color);
            }
        }

        public static void Info(object message, ConsoleColor color = ConsoleColor.White)
        {
            Log(message?.ToString(), LogLevel.Info, color, Assembly.GetCallingAssembly());
        }

        public static void Debug(object message, ConsoleColor color = ConsoleColor.Gray)
        {
#if DEBUG
            Log(message?.ToString(), LogLevel.Debug, color, Assembly.GetCallingAssembly());
#endif
        }

        public static void Warn(object message, ConsoleColor color = ConsoleColor.Yellow)
        {
            Log(message?.ToString(), LogLevel.Warning, color, Assembly.GetCallingAssembly());
        }

        public static void Error(object message, ConsoleColor color = ConsoleColor.Red)
        {
            Log(message?.ToString(), LogLevel.Error, color, Assembly.GetCallingAssembly());
        }
    }
}