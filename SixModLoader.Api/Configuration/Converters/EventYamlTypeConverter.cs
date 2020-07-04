#nullable enable
using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SixModLoader.Api.Configuration.Converters
{
    public abstract class EventYamlTypeConverter : IYamlTypeConverter
    {
        public Func<IEventEmitter> EventEmitter { get; set; } = null!;

        public abstract bool Accepts(Type type);
        public abstract object? ReadYaml(IParser parser, Type type);
        public abstract void WriteYaml(IEmitter emitter, object? value, Type type);
    }
}