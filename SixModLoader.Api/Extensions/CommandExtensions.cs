using System;
using System.Collections.Generic;
using CommandSystem;
using RemoteAdmin;

namespace SixModLoader.Api.Extensions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoCommandHandlerAttribute : CommandHandlerAttribute
    {
        public Type Type { get; }

        public AutoCommandHandlerAttribute(Type type) : base(type)
        {
            if (!typeof(ICommandHandler).IsAssignableFrom(type))
            {
                throw new ArgumentException("Type must inherit from ICommandHandler!");
            }

            Type = type;
        }
    }
    
    public static class CommandExtensions
    {
        public static bool CheckPermission(this ICommandSender sender, string permission)
        {
            if (sender is PlayerCommandSender playerSender)
            {
                var player = ReferenceHub.GetHub(playerSender.Processor.gameObject);
                return player.CheckPermission(permission);
            }
            return true;
        }
        
        public static bool CheckPermission(this ReferenceHub player, string permission)
        {
            return player.gameObject == PlayerManager.localPlayer; // || TODO permissions
        }

        public static List<ReferenceHub> MatchPlayers(string text, ICommandSender sender = null)
        {
            if (text == null)
                return null;
            
            var players = new List<ReferenceHub>();

            foreach (var s in text.Split('.', ';'))
            {
                switch (s)
                {
                    case "^":
                        if (sender is PlayerCommandSender playerSender)
                        {
                            var player = ReferenceHub.GetHub(playerSender.Processor.gameObject);
                            if (player != null)
                            {
                                players.Add(player);
                                continue;
                            }
                        }
                        break;
                    case "*":
                        players.AddRange(ReferenceHub.Hubs.Values);
                        continue;
                }

                if (int.TryParse(s, out var id))
                {
                    var player = ReferenceHub.GetHub(id);
                    if (player != null)
                    {
                        players.Add(player);
                        continue;
                    }
                }

                sender?.Respond($"Couldn't find player: {s}", false);
            }

            return players;
        }
    }
}
