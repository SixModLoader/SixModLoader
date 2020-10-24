using System.Linq;
using Mirror;
using UnityEngine;

namespace SixModLoader.Api.Extensions
{
    public static class Extensions
    {
        public static void SetScale(this GameObject target, Vector3 vector)
        {
            var identity = target.GetComponent<NetworkIdentity>();

            target.transform.localScale = vector;

            var destroyMessage = new ObjectDestroyMessage { netId = identity.netId };

            foreach (var player in PlayerManager.players)
            {
                var connection = player.GetComponent<NetworkIdentity>().connectionToClient;

                if (player != target)
                {
                    connection.Send(destroyMessage);
                }

                NetworkServer.SendSpawnMessage(identity, connection);
            }
        }

        public static Inventory.SyncItemInfo GetItemByUniq(this Inventory inventory, int uniq)
        {
            return inventory.items.Cast<Inventory.SyncItemInfo?>().FirstOrDefault(x => x != null && x.Value.uniq == uniq) ?? new Inventory.SyncItemInfo { id = ItemType.None };
        }

        public static void OverridePosition(this PlayerMovementSync playerMovementSync, Vector3 position, float rotation = 0, bool forceGround = false)
        {
            playerMovementSync.OverridePosition(position, rotation, forceGround);
        }

        public static string Format(this ReferenceHub referenceHub)
        {
            return $"{referenceHub.nicknameSync.MyNick} ({referenceHub.characterClassManager.UserId})";
        }
    }
}