using CommandSystem;

namespace SixModLoader.Api.Extensions
{
    public static class PermissionExtensions
    {
        public static IPermissionProvider Provider { get; set; } = new DefaultPermissionProvider();

        public static bool HasPermission(this ICommandSender sender, string permission)
        {
            return Provider.HasPermission(sender, permission);
        }

        public static bool HasPermission(this ReferenceHub player, string permission)
        {
            return Provider.HasPermission(player, permission);
        }
    }

    public interface IPermissionProvider
    {
        bool HasPermission(ICommandSender sender, string permission);
        bool HasPermission(ReferenceHub player, string permission);
    }

    internal class DefaultPermissionProvider : IPermissionProvider
    {
        private void Warn()
        {
            Logger.Warn("No permission provider found, failing permission check! (checkout https://github.com/SixModLoader/Permissions)");
        }

        public bool HasPermission(ICommandSender sender, string permission)
        {
            Warn();
            return false;
        }

        public bool HasPermission(ReferenceHub player, string permission)
        {
            Warn();
            return false;
        }
    }
}