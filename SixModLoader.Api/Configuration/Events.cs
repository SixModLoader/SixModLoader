using SixModLoader.Events;
using YamlDotNet.Serialization;

namespace SixModLoader.Api.Configuration
{
    public class YamlDeserializerBuildEvent : Event
    {
        public YamlDeserializerBuildEvent(DeserializerBuilder builder)
        {
            Builder = builder;
        }

        public DeserializerBuilder Builder { get; }
    }

    public class YamlSerializerBuildEvent : Event
    {
        public YamlSerializerBuildEvent(SerializerBuilder builder)
        {
            Builder = builder;
        }

        public SerializerBuilder Builder { get; }
    }
}