#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SixModLoader.Api.Configuration.Converters
{
    /// <summary>
    /// Yaml type converter for <see cref="Vector2"/>, <see cref="Vector3"/> and <see cref="Vector4"/>
    /// </summary>
    public class VectorsConverter : EventYamlTypeConverter
    {
        private readonly Dictionary<Type, ushort> _vectors = new Dictionary<Type, ushort>
        {
            [typeof(Vector2)] = 2,
            [typeof(Vector3)] = 3,
            [typeof(Vector4)] = 4
        };

        public override bool Accepts(Type type)
        {
            return _vectors.ContainsKey(type);
        }

        /// <summary>
        /// Reads VectorX in [x, y, z, w] format (any sequence style, size must match vector axes count)
        /// </summary>
        public override object? ReadYaml(IParser parser, Type type)
        {
            parser.Require<SequenceStart>();
            parser.MoveNext();

            var scalars = new List<Scalar>();

            while (!parser.TryConsume<SequenceEnd>(out _))
            {
                scalars.Add(parser.Require<Scalar>());
                parser.MoveNext();
            }

            if (scalars.Count == _vectors[type])
            {
                return Activator.CreateInstance(type, scalars.Select(x => float.Parse(x.Value, CultureInfo.InvariantCulture)).Cast<object>().ToArray());
            }

            throw new YamlException($"Invalid {type.Name}");
        }

        /// <summary>
        /// Writes VectorX in [x, y, z, w] format
        /// </summary>
        public override void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            var eventEmitter = EventEmitter.Invoke();
            var axes = new List<float>();

            switch (value)
            {
                case Vector2 vector:
                    axes.AddRange(new[] { vector.x, vector.y });
                    break;
                case Vector3 vector:
                    axes.AddRange(new[] { vector.x, vector.y, vector.z });
                    break;
                case Vector4 vector:
                    axes.AddRange(new[] { vector.x, vector.y, vector.z, vector.w });
                    break;
            }

            eventEmitter.Emit(new SequenceStartEventInfo(new ObjectDescriptor(axes, axes.GetType(), axes.GetType())) { Style = SequenceStyle.Flow }, emitter);

            foreach (var axis in axes)
            {
                eventEmitter.Emit(new ScalarEventInfo(new ObjectDescriptor(axis, typeof(float), typeof(float))), emitter);
            }

            eventEmitter.Emit(new SequenceEndEventInfo(new ObjectDescriptor(axes, axes.GetType(), axes.GetType())), emitter);
        }
    }
}