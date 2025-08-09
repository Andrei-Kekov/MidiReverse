using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace MidiReverse
{
    internal class MidiState
    {
        public long MicrosecondsPerQuarterNote { get; set; } = SetTempoEvent.DefaultMicrosecondsPerQuarterNote;

        public Dictionary<FourBitNumber, ChannelState> ChannelStates { get; private set; } = new Dictionary<FourBitNumber, ChannelState>();
    }
}
