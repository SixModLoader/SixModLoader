using System;
using System.Linq;
using System.Reflection;
using CommandSystem;
using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Mods;

namespace SixModLoader.Api
{
    [AutoCommandHandler(typeof(GameConsoleCommandHandler))]
    [AutoCommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SixModLoaderCommand : ParentCommand
    {
        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            return DefaultCommand.Execute(arguments, sender, out response);
        }

        public ICommand DefaultCommand;
        public override string Command => "sixmodloader";
        public override string[] Aliases => new[] { "sml" };
        public override string Description => "SixModLoader";

        public SixModLoaderCommand()
        {
            LoadGeneratedCommands();
        }

        public class ModsCommand : ICommand
        {
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var mods = SixModLoader.Instance.ModManager.Mods;
                response = $"Mods ({mods.Count}): " + string.Join(", ", mods.Select(mod => $"{mod.Info.Name} ({mod.Info.Version})"));
                return true;
            }

            public string Command => "mods";
            public string[] Aliases => new string[0];
            public string Description => "Lists mods";
        }

        public class VersionCommand : ICommand
        {
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (arguments.Count > 0)
                {
                    var modName = string.Join(" ", arguments);
                    var mod = SixModLoader.Instance.ModManager.Mods.FirstOrDefault(x => x.Info.Name.Equals(modName, StringComparison.InvariantCultureIgnoreCase));

                    if (mod == null)
                    {
                        response = $"Mod {modName} not found";
                        return false;
                    }

                    response = $"{mod.Info.Name}\n" +
                               $"Id: {mod.Info.Id}\n" +
                               $"Version: {mod.Info.Version}\n" +
                               $"{"Author".Pluralize(mod.Info.Authors.Length)}: {string.Join(", ", mod.Info.Authors)}\n" +
                               $"Type: {mod.Type}\n" +
                               $"Assembly: {mod.Assembly.GetName().Name} ({mod.Assembly.Location})";

                    return true;
                }

                response = $"SixModLoader {SixModLoader.Instance.Version}";
                return true;
            }

            public string Command => "version";
            public string[] Aliases => new[] { "ver" };
            public string Description => "Gets version of SixModLoader or mod";
        }

        public class ReloadCommand : ICommand
        {
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var mods = SixModLoader.Instance.ModManager.Mods;

                if (arguments.Count > 0)
                {
                    var modName = string.Join(" ", arguments);
                    var mod = mods.FirstOrDefault(x => x.Info.Name.Equals(modName, StringComparison.InvariantCultureIgnoreCase));

                    if (mod == null)
                    {
                        response = $"Mod {modName} not found";
                        return false;
                    }

                    mods.Clear();
                    mods.Add(mod);
                }

                new ModReloadEvent(mods).Call();

                response = $"Reloaded {mods.Count} {"mod".Pluralize(mods.Count)}";
                return true;
            }

            public string Command => "reload";
            public string[] Aliases => new[] { "rl" };
            public string Description => "Reload configs";
        }

        public class PatchesCommand : ICommand
        {
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var methodInfo = AccessTools.Method(arguments.At(0), arguments.Skip(1).Select(AccessTools.TypeByName).ToArray());
                HarmonyLib.Patches patches;

                try
                {
                    patches = Harmony.GetPatchInfo(methodInfo);
                }
                catch (Exception e) when (e is ArgumentException || e is AmbiguousMatchException)
                {
                    response = e.Message;
                    return false;
                }

                response = methodInfo.FullDescription();

                string Format(Patch patch) => $"{patch.PatchMethod.FullDescription()} ({patch.owner})";

                if (patches.Owners.Any())
                {
                    response += $"\nOwners: {patches.Owners.Join()}";
                }

                if (patches.Transpilers.Any())
                {
                    response += $"\nTranspilers: {patches.Transpilers.Select(Format).Join()}";
                }

                if (patches.Prefixes.Any())
                {
                    response += $"\nPrefixes: {patches.Prefixes.Select(Format).Join()}";
                }

                if (patches.Postfixes.Any())
                {
                    response += $"\nPostfixes: {patches.Postfixes.Select(Format).Join()}";
                }

                if (patches.Finalizers.Any())
                {
                    response += $"\nFinalizers: {patches.Finalizers.Select(Format).Join()}";
                }

                return true;
            }

            public string Command => "patches";
            public string[] Aliases => new string[0];
            public string Description => "Debug harmony patches";
        }

        public sealed override void LoadGeneratedCommands()
        {
            RegisterCommand(DefaultCommand = new VersionCommand());
            RegisterCommand(new ModsCommand());
            RegisterCommand(new ReloadCommand());
            RegisterCommand(new PatchesCommand());
        }
    }
}