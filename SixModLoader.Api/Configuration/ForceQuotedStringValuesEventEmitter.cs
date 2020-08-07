using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace SixModLoader.Api.Configuration
{
    // https://github.com/aaubry/YamlDotNet/issues/428#issuecomment-525822859
    public class ForceQuotedStringValuesEventEmitter : ChainedEventEmitter
    {
        private class EmitterState
        {
            private int valuePeriod;
            private int currentIndex;

            public EmitterState(int valuePeriod)
            {
                this.valuePeriod = valuePeriod;
            }

            public bool VisitNext()
            {
                ++currentIndex;
                return (currentIndex % valuePeriod) == 0;
            }
        }

        private readonly Stack<EmitterState> state = new Stack<EmitterState>();

        public ForceQuotedStringValuesEventEmitter(IEventEmitter nextEmitter)
            : base(nextEmitter)
        {
            this.state.Push(new EmitterState(1));
        }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (this.state.Peek().VisitNext())
            {
                if (eventInfo.Source.Type == typeof(string))
                {
                    eventInfo.Style = ScalarStyle.DoubleQuoted;
                }
            }

            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Peek().VisitNext();
            this.state.Push(new EmitterState(2));
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Pop();
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Peek().VisitNext();
            this.state.Push(new EmitterState(1));
            base.Emit(eventInfo, emitter);
        }

        public override void Emit(SequenceEndEventInfo eventInfo, IEmitter emitter)
        {
            this.state.Pop();
            base.Emit(eventInfo, emitter);
        }
    }
}