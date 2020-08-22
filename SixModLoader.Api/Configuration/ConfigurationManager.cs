using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using SixModLoader.Api.Configuration.Converters;
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
        public static List<EventYamlTypeConverter> Converters { get; } = new List<EventYamlTypeConverter>
        {
            new VectorsConverter()
        };

        public static Dictionary<string, Type> TagMappings { get; } = new Dictionary<string, Type>();

        public static void RegisterTagMapping(Type type)
        {
            TagMappings["!" + type.Name] = type;
        }

        public static void RegisterTagMapping<T>()
        {
            RegisterTagMapping(typeof(T));
        }

        private static readonly Lazy<IDeserializer> _deserializer = new Lazy<IDeserializer>(() =>
        {
            var deserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .IgnoreUnmatchedProperties();

            foreach (var converter in Converters)
            {
                deserializerBuilder.WithTypeConverter(converter);
            }

            foreach (var tagMapping in TagMappings)
            {
                deserializerBuilder.WithTagMapping(tagMapping.Key, tagMapping.Value);
            }

            SixModLoader.Instance.EventManager.Broadcast(new YamlDeserializerBuildEvent(deserializerBuilder));

            return deserializerBuilder.Build();
        });

        public static IDeserializer Deserializer => _deserializer.Value;

        private static readonly Lazy<ISerializer> _serializer = new Lazy<ISerializer>(() =>
        {
            // https://github.com/aaubry/YamlDotNet/issues/473#issuecomment-595954595
            IEventEmitter eventEmitter = null;

            var serializerBuilder = new SerializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .WithEventEmitter(next => eventEmitter = new ForceQuotedStringValuesEventEmitter(next))
                .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
                .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor));

            foreach (var converter in Converters)
            {
                converter.EventEmitter = () => eventEmitter;
                serializerBuilder.WithTypeConverter(converter);
            }

            foreach (var tagMapping in TagMappings)
            {
                serializerBuilder.WithTagMapping(tagMapping.Key, tagMapping.Value);
            }

            SixModLoader.Instance.EventManager.Broadcast(new YamlSerializerBuildEvent(serializerBuilder));

            return serializerBuilder.Build();
        });

        public static ISerializer Serializer => _serializer.Value;

        public static void Initialize()
        {
            SixModLoader.Instance.Harmony
                .CreateProcessor(AccessTools.Method(typeof(ModEvent), nameof(ModEvent.Call), new Type[0]))
                .AddPrefix(AccessTools.Method(typeof(Patch), nameof(Patch.Prefix)))
                .Patch();
        }

        public static T LoadConfigurationFile<T>(string file) where T : new()
        {
            return (T) LoadConfigurationFile(typeof(T), file, new T());
        }

        public static object LoadConfigurationFile(Type type, string file, object obj)
        {
            Directory.GetParent(file).Create();
            var exists = File.Exists(file);

            try
            {
                using (var fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (exists)
                    {
                        obj = Deserializer.Deserialize(new StreamReader(fileStream), type) ?? obj;
                    }
                }

                try
                {
                    var yaml = Serializer.Serialize(obj);
                    File.WriteAllText(file, yaml);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to serialize " + file + "\n" + e);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to deserialize " + file + "\n" + e);
            }

            return obj;
        }

        public class Patch
        {
            public static void Prefix(ModEvent __instance)
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
                        property.SetValue(mod.AbstractInstance,
                            LoadConfigurationFile(property.PropertyType, Path.Combine(mod.Directory, attribute.File),
                                Activator.CreateInstance(property.PropertyType)));
                    }
                }
            }
        }
    }
}