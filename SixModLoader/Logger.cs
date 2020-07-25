using System;
using System.IO;
using System.Linq;
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
        public static IdentifiedLogger Unknown { get; } = new IdentifiedLogger("UNKNOWN");

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

        public static IdentifiedLogger GetLogger(Assembly assembly)
        {
            return SixModLoader.Instance.ModManager.GetMod(assembly)?.Info?.Logger ?? Unknown;
        }

        public static void Info(object message)
        {
            GetLogger(Assembly.GetCallingAssembly()).Info(message);
        }

        public static void Debug(object message)
        {
            GetLogger(Assembly.GetCallingAssembly()).Debug(message);
        }

        public static void Warn(object message)
        {
            GetLogger(Assembly.GetCallingAssembly()).Warn(message);
        }

        public static void Error(object message)
        {
            GetLogger(Assembly.GetCallingAssembly()).Error(message);
        }
    }

    public class IdentifiedLogger
    {
        public string Identifier { get; set; }

        public IdentifiedLogger(string identifier)
        {
            Identifier = identifier;
        }

        public void Log(string message, LogLevel level, ConsoleColor color = ConsoleColor.Gray)
        {
            message = $"[{Enum.GetName(typeof(LogLevel), level)?.ToUpper()}] [{Identifier}] {message}";
            if (ServerStatic.ServerOutput == null || !(ServerStatic.ServerOutput is StandardOutput))
            {
                Console.WriteLine(message);
            }

            File.AppendAllText(Logger.FilePath, message + "\n");

#if !DEBUG
            if (level == LogLevel.Debug)
                return;
#endif

            if (ServerStatic.ServerOutput != null)
            {
                ServerConsole.AddLog(message, color);
            }
        }

        public void Info(object message)
        {
            Log(message?.ToString(), LogLevel.Info, ConsoleColor.White);
        }

        public void Debug(object message)
        {
            Log(message?.ToString(), LogLevel.Debug);
        }

        public void Warn(object message)
        {
            Log(message?.ToString(), LogLevel.Warning, ConsoleColor.Yellow);
        }

        public void Error(object message)
        {
            Log(message?.ToString(), LogLevel.Error, ConsoleColor.Red);
            if (message is ReflectionTypeLoadException typeLoadException)
            {
                Error($"Exceptions: {typeLoadException.LoaderExceptions.Select(x => x.ToString()).Join()}");
            }
        }
    }
}