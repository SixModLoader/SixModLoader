using CommandSystem;
using HarmonyLib;
using RemoteAdmin;
using SixModLoader.Api.Configuration;
using SixModLoader.Api.Events.Server;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using SixModLoader.Mods;

namespace SixModLoader.Api
{
    [Mod("SixModLoader.SixModLoaderApi")]
    public class SixModLoaderApi
    {
        public Harmony Harmony { get; } = new Harmony("SixModLoader.SixModLoaderApi");

        public CommandManager CommandManager { get; } = new CommandManager();
        public CustomEffectManager CustomEffectManager { get; } = new CustomEffectManager();
        public LibraryManager LibraryManager { get; } = new LibraryManager();

        public SixModLoaderApi(SixModLoader loader)
        {
            loader.EventManager.RegisterStatic(typeof(BroadcastExtensions));
            loader.EventManager.RegisterStatic(typeof(ConfigurationManager));
            ConfigurationManager.Initialize();
            HarmonyExtensions.Initialize();
        }

        [EventHandler(typeof(ModEnableEvent))]
        public void OnEnable()
        {
            Harmony.PatchAll();
            LibraryManager.Download();
        }

        [EventHandler(typeof(ModDisableEvent))]
        public void OnDisable()
        {
            Harmony.UnpatchAll(Harmony.Id);
        }

        [EventHandler(typeof(ServerConsoleReadyEvent))]
        public void OnServerConsoleReady()
        {
            Logger.Debug("server console rdy");
            foreach (var mod in SixModLoader.Instance.ModManager.Mods)
            {
                CommandManager.CommandHandlers.AddRange(new CommandHandler[]
                {
                    CommandProcessor.RemoteAdminCommandHandler,
                    GameCore.Console.singleton.ConsoleCommandHandler,
                    QueryProcessor.DotCommandHandler
                });

                CommandManager.RegisterCommands(mod);
            }
        }
    }
}