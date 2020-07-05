using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandSystem;
using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Mods;

namespace SixModLoader.Api
{
    public class CommandManager
    {
        public static List<ICommandHandler> CommandHandlers { get; } = new List<ICommandHandler>();

        public void RegisterCommands(ModContainer mod)
        {
            foreach (var commandType in mod.Assembly.GetTypes().Where(x => typeof(ICommand).IsAssignableFrom(x)))
            {
                RegisterCommand(commandType);
            }
        }

        public void RegisterCommand<T>() where T : ICommand
        {
            RegisterCommand(typeof(T));
        }

        public void RegisterCommand(Type commandType)
        {
            foreach (var attribute in commandType.GetCustomAttributes<AutoCommandHandlerAttribute>())
            {
                var commandHandler = CommandHandlers.FirstOrDefault(x => x.GetType().IsAssignableFrom(attribute.Type));
                if (commandHandler == null)
                {
                    Logger.Warn($"Not found {attribute.Type} for {commandType}");
                    continue;
                }

                if (commandHandler.AllCommands.All(x => x.GetType() != commandType))
                {
                    Logger.Debug($"Registering {commandType} in {attribute.Type}");
                    commandHandler.RegisterCommand((ICommand) AccessTools.CreateInstance(commandType));
                }
            }
        }
    }
}