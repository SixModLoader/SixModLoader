using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using SixModLoader.Mods;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SixModLoader.Api.Configuration
{
    public enum ConfigurationType
    {
        Configuration,
        Translations
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoConfiguration : Attribute
    {
        public string File { get; }

        public AutoConfiguration(string file)
        {
            File = file;
        }

        public AutoConfiguration(ConfigurationType type)
        {
            switch (type)
            {
                case ConfigurationType.Configuration:
                    File = "config.yml";
                    break;
                case ConfigurationType.Translations:
                    File = "translations.yml";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public class ConfigurationManager
    {
        public static IDeserializer Deserializer { get; } = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public static ISerializer Serializer { get; } = new SerializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .WithEventEmitter(next => new ForceQuotedStringValuesEventEmitter(next))
            .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
            .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
            .Build();

        public static T LoadConfigurationFile<T>(string file) where T : new()
        {
            return (T) LoadConfigurationFile(typeof(T), file, new T());
        }

        public static object LoadConfigurationFile(Type type, string file, object obj)
        {
            Directory.GetParent(file).Create();
            var exists = File.Exists(file);

            using (var fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                if (exists)
                {
                    obj = Deserializer.Deserialize(new StreamReader(fileStream), type);
                }
            }

            using (var streamWriter = new StreamWriter(file, false))
            {
                Serializer.Serialize(streamWriter, obj);
            }

            return obj;
        }

        [HarmonyPatch(typeof(ModEvent), nameof(ModEvent.Call), new Type[0])]
        public class Patch
        {
            private static void Prefix(ModEvent __instance)
            {
                if (__instance.GetType() != typeof(ModReloadEvent))
                    return;

                foreach (var mod in __instance.Mods)
                {
                    foreach (var property in mod.Type.GetProperties())
                    {
                        var attribute = property.GetCustomAttribute<AutoConfiguration>();
                        if (attribute == null)
                            continue;

                        Logger.Info($"[{mod.Info.Name}] Reloading {attribute.File}");
                        property.SetValue(mod.AbstractInstance, LoadConfigurationFile(property.PropertyType, Path.Combine(mod.Directory, attribute.File), Activator.CreateInstance(property.PropertyType)));
                    }
                }
            }
        }
    }
}