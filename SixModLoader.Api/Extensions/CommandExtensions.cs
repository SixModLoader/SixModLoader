using System;
using System.Collections.Generic;
using System.Linq;
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
                        players.AddRange(ReferenceHub.Hubs.Values.Where(x => !x.isDedicatedServer));
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