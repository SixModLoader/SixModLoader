using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MEC;
using Mirror;
using SixModLoader.Api.Events.Player;
using SixModLoader.Events;
using UnityEngine;

namespace SixModLoader.Api.Extensions
{
    public class BroadcastMessage
    {
        public NetworkConnection Connection { get; set; }
        public string Data { get; set; }
        public float Time { get; set; }
        public Broadcast.BroadcastFlags Flags { get; set; }
        public DateTimeOffset StartedAt { get; set; }

        public BroadcastMessage(string data, float time, Broadcast.BroadcastFlags flags = Broadcast.BroadcastFlags.Normal, NetworkConnection connection = null)
        {
            Data = data;
            Time = time;
            Flags = flags;
            Connection = connection;
        }
    }

    public class BroadcastConnection
    {
        public ReferenceHub Player { get; }
        public List<BroadcastMessage> Messages { get; set; } = new List<BroadcastMessage>();
        public BroadcastMessage CurrentMessage { get; set; }
        public string[] StaticMessages { get; set; } = new string[3];

        public void KillCoroutines()
        {
            CurrentMessage = null;

            if (Player != null && Player.gameObject != null)
            {
                Timing.KillCoroutines(Player.gameObject);
            }
        }

        public BroadcastConnection(ReferenceHub player)
        {
            Player = player;
        }
    }

    /// <summary>
    /// This thing is weird and very experimental, but provides nice way to display forever staying broadcast
    /// </summary>
    public static class BroadcastExtensions
    {
        private static Broadcast _broadcast;

        public static Broadcast BroadcastComponent
        {
            get
            {
                if (PlayerManager.localPlayer == null || !PlayerManager.LocalPlayerSet)
                    return null;

                if (_broadcast == null)
                    _broadcast = PlayerManager.localPlayer.GetComponent<Broadcast>();

                return _broadcast;
            }
        }

        internal static bool DisablePatches;
        public const uint MaxBroadcastTime = 300;

        public static Dictionary<NetworkConnection, BroadcastConnection> Connections { get; } = new Dictionary<NetworkConnection, BroadcastConnection>();

        [EventHandler]
        [Priority(Priority.Highest)]
        internal static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            var connection = ev.Player.networkIdentity.connectionToClient;
            Connections[connection] = new BroadcastConnection(ev.Player);
        }

        [EventHandler]
        [Priority(Priority.Highest)]
        internal static void OnPlayerLeft(PlayerLeftEvent ev)
        {
            var connection = ev.Player.networkIdentity.connectionToClient;

            if (Connections.TryGetValue(connection, out var broadcastConnection))
            {
                broadcastConnection.KillCoroutines();
                Connections.Remove(connection);
            }
        }

        public static void SetStaticMessage(this ReferenceHub player, int i, string status, float? time = null)
        {
            var connection = player.playerMovementSync.connectionToClient;
            Connections[connection].StaticMessages[i] = status;

            Update(connection);

            if (time.HasValue)
            {
                Timing.CallDelayed(time.Value, () =>
                {
                    if (Connections[connection].StaticMessages.TryGet(i, out var newStatus) && newStatus == status)
                    {
                        player.SetStaticMessage(i, null);
                    }
                }, player.gameObject);
            }
        }

        public static void Broadcast(this ReferenceHub player, string data, ushort time, Broadcast.BroadcastFlags flags = global::Broadcast.BroadcastFlags.Normal)
        {
            BroadcastComponent.TargetAddElement(player.networkIdentity.connectionToClient, data, time, flags);
        }

        public static void ClearBroadcasts(this ReferenceHub player)
        {
            BroadcastComponent.TargetClearElements(player.networkIdentity.connectionToClient);
        }

        public static void Broadcast(string data, ushort time, Broadcast.BroadcastFlags flags = global::Broadcast.BroadcastFlags.Normal)
        {
            BroadcastComponent.CallRpcAddElement(data, time, flags);
        }

        public static void ClearBroadcasts()
        {
            BroadcastComponent.RpcClearElements();
        }

        internal static void Update(NetworkConnection connection)
        {
            if (!connection.isReady)
            {
                return;
            }

            var broadcastConnection = Connections[connection];
            var statusOnly = broadcastConnection.CurrentMessage == null && broadcastConnection.Messages.Count <= 0;

            var message = statusOnly ? new BroadcastMessage(string.Empty, MaxBroadcastTime, connection: connection) : broadcastConnection.CurrentMessage ?? broadcastConnection.Messages[0];
            if (broadcastConnection.CurrentMessage != null)
            {
                message.Time = (float) (message.Time - (DateTimeOffset.Now - broadcastConnection.CurrentMessage.StartedAt).TotalSeconds);
                broadcastConnection.KillCoroutines();
            }

            if (!statusOnly)
            {
                broadcastConnection.CurrentMessage = message;
            }

            var data = message.Data;

            if (!statusOnly)
            {
                data += "\n";
            }

            data += string.Join("\n", broadcastConnection.StaticMessages.Where(x => !string.IsNullOrEmpty(x)));

            InvokeOriginal(() =>
            {
                BroadcastComponent.TargetClearElements(connection);
                if (!string.IsNullOrEmpty(data))
                {
                    BroadcastComponent.TargetAddElement(connection, data, (ushort) Mathf.CeilToInt(message.Time), message.Flags);
                }
            });

            message.StartedAt = DateTimeOffset.Now;
            Timing.CallDelayed(message.Time, () =>
            {
                if (!statusOnly)
                {
                    broadcastConnection.Messages.Remove(message);
                    broadcastConnection.KillCoroutines();
                }

                Update(connection);
            }, broadcastConnection.Player.gameObject);
        }

        internal static void InvokeOriginal(Action action)
        {
            DisablePatches = true;
            action.Invoke();
            DisablePatches = false;
        }

        [HarmonyPatch(typeof(Broadcast), nameof(global::Broadcast.TargetAddElement))]
        public static class TargetAddElementPatch
        {
            public static bool Prefix(NetworkConnection conn, string data, uint time, Broadcast.BroadcastFlags flags)
            {
                if (DisablePatches)
                {
                    return true;
                }

                var broadcastConnection = Connections.GetValueSafe(conn);
                if (broadcastConnection != null)
                {
                    broadcastConnection.Messages.Add(new BroadcastMessage(data, time, flags, conn));
                    Update(conn);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Broadcast), nameof(global::Broadcast.RpcAddElement))]
        public static class RpcAddElementPatch
        {
            public static bool Prefix(string data, uint time, Broadcast.BroadcastFlags flags)
            {
                if (DisablePatches)
                {
                    return true;
                }

                foreach (var pair in Connections)
                {
                    pair.Value.Messages.Add(new BroadcastMessage(data, time, flags));
                    Update(pair.Key);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Broadcast), nameof(global::Broadcast.TargetClearElements))]
        public static class TargetClearElementsPatch
        {
            public static void Prefix(NetworkConnection conn)
            {
                if (DisablePatches)
                {
                    return;
                }

                var broadcastConnection = Connections.GetValueSafe(conn);
                if (broadcastConnection != null)
                {
                    broadcastConnection.KillCoroutines();
                    broadcastConnection.Messages.Clear();
                }
            }
        }

        [HarmonyPatch(typeof(Broadcast), nameof(global::Broadcast.RpcClearElements))]
        public static class RpcClearElementsPatch
        {
            public static void Prefix()
            {
                if (DisablePatches)
                {
                    return;
                }

                foreach (var broadcastConnection in Connections.Values)
                {
                    broadcastConnection.KillCoroutines();
                    broadcastConnection.Messages.Clear();
                }
            }
        }
    }
}