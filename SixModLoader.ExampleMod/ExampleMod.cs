using System;
using CommandSystem;
using SixModLoader.Api.Configuration;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using SixModLoader.Mods;

namespace SixModLoader.ExampleMod
{
    public class Configuration
    {
        public string X { get; set; } = "Hello!";
    }

    [Mod("SixModLoader.ExampleMod")]
    public class ExampleMod
    {
        [Inject]
        public ModContainer ModContainer { get; set; }

        [EventHandler(typeof(ModEnableEvent))]
        public void OnEnable()
        {
            Logger.Info($"{ModContainer.Info.Name} {ModContainer.Info.Version} loaded!");
        }

        [AutoConfiguration(ConfigurationType.Configuration)]
        public Configuration Configuration { get; set; }

        [EventHandler(typeof(ModReloadEvent))]
        public void OnReload()
        {
            Logger.Info("ExampleMod reloaded!");
            Logger.Info($"X = {Configuration.X}");
        }
    }

    [AutoCommandHandler(typeof(GameConsoleCommandHandler))]
    [AutoCommandHandler(typeof(ClientCommandHandler))]
    [AutoCommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ExampleCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"Hello {(sender is CommandSender commandSender ? commandSender.Nickname : "someone")}!";

            return true;
        }

        public string Command => "example";
        public string[] Aliases => new[] { "ex" };
        public string Description => "Example command!";
    }
}