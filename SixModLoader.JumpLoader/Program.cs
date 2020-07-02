using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SixModLoader.JumpLoader
{
    internal class Program
    {
        private const string Doorstop = "libdoorstop_x64.so";
        private const string SixModLoader = "SixModLoader/bin/SixModLoader.dll";

        public static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unsupported operating system!");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine("Just use windows Doorstop");
                }

                Console.ResetColor();
                return;
            }

            Console.WriteLine($"SixModLoader - JumpLoader {Assembly.GetExecutingAssembly().GetName().Version}! (because game hosting providers are dumb)");

            var currentDirectory = Directory.GetCurrentDirectory();

            if (!File.Exists(Doorstop))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Doorstop} not found!");
                Console.ResetColor();
                return;
            }

            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", $"{Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")}:{currentDirectory}");
            Environment.SetEnvironmentVariable("LD_PRELOAD", Doorstop);

            Environment.SetEnvironmentVariable("DOORSTOP_ENABLE", "TRUE");
            Environment.SetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH", Path.Combine(currentDirectory, SixModLoader));

            if (!File.Exists(SixModLoader))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{SixModLoader} not found!");
                Console.ResetColor();
                return;
            }

            var file = (File.Exists("jumploader.txt") ? File.ReadAllText("jumploader.txt") : "LocalAdmin {args}")
                .Replace("{args}", string.Join(" ", args))
                .Split(' ');

            Console.WriteLine($"Starting \"{string.Join(" ", file)}\" with Doorstop.Unix and SixModLoader!");
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file.First(),
                    Arguments = string.Join(" ", file.Skip(1)),
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            Console.WriteLine("Process exited");
        }
    }
}