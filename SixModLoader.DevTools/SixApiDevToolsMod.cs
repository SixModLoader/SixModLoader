using System;
using System.Globalization;
using System.Linq;
using CommandSystem;
using GameCore;
using HarmonyLib;
using Hints;
using MEC;
using RemoteAdmin;
using SixModLoader.Api.Events.Player;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using SixModLoader.Mods;
using UnityEngine;

namespace SixModLoader.DevTools
{
    [Mod("SixModLoader.DevTools")]
    public class SixApiDevToolsMod
    {
        [AutoHarmony]
        public Harmony Harmony { get; set; }
        
        [EventHandler]
        private void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (RoundStart.singleton.NetworkTimer != -1)
            {
                RoundSummary.RoundLock = true;
                CharacterClassManager.ForceRoundStart();
            }

            if (ev.Player.isLocalPlayer)
                return;
            
            Timing.CallDelayed(1, () => ev.Player.characterClassManager.SetPlayersClass(ev.Player.characterClassManager.ForceClass, ev.Player.gameObject));
        }
    }

    [AutoCommandHandler(typeof(RemoteAdminCommandHandler))]
    public class HintCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerCommandSender)
            {
                ReferenceHub.GetHub(playerCommandSender.Processor.gameObject).hints.Show(new TextHint(
                    arguments.Join(delimiter: " "),
                    new HintParameter[] {new StringHintParameter(string.Empty)},
                    HintEffectPresets.FadeInAndOut(0.25f)
                ));

                response = "ok";
                return true;
            }

            response = "no";
            return false;
        }

        public string Command => "hint";
        public string[] Aliases => new string[0];
        public string Description => "Shows hint";
    }

    [AutoCommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ScaleCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var targets = CommandExtensions.MatchPlayers(arguments.ElementAtOrDefault(0), sender);

            if (targets != null)
            {
                float.TryParse(arguments.ElementAtOrDefault(1), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var x);
                float.TryParse(arguments.ElementAtOrDefault(2), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var y);
                float.TryParse(arguments.ElementAtOrDefault(3), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var z);
                var scale = new Vector3(x, y, z);

                foreach (var target in targets)
                {
                    target.gameObject.SetScale(scale);
                }

                response = $"Scaled {targets.Count} players to {scale}";
                return true;
            }

            response = "no";
            return false;
        }

        public string Command => "scale";
        public string[] Aliases => new string[0];
        public string Description => "Scales someone";
    }
}