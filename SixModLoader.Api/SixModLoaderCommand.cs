using System;
using System.Linq;
using CommandSystem;
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
        public override string[] Aliases => new[] {"sml"};
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
                response = $"Mods ({mods.Count}): " + string.Join(", ", mods.Select(mod => mod.Info.Name));
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
            public string[] Aliases => new[] {"ver"};
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

                new ModReloadEvent {Mods = mods}.Call();

                response = $"Reloaded {mods.Count} {"mod".Pluralize(mods.Count)}";
                return true;
            }

            public string Command => "reload";
            public string[] Aliases => new[] {"rl"};
            public string Description => "Reload configs";
        }

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(DefaultCommand = new VersionCommand());
            RegisterCommand(new ModsCommand());
            RegisterCommand(new ReloadCommand());
        }
    }
}