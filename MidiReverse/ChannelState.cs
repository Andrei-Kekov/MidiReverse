using Melanchall.DryWetMidi.Common;

namespace MidiReverse
{
    internal class ChannelState
    {
        public SevenBitNumber ProgramNumber { get; set; }

        public Dictionary<SevenBitNumber, SevenBitNumber> Controls { get; private set; } = new Dictionary<SevenBitNumber, SevenBitNumber>();

        public ChannelState() { }

        public ChannelState(SevenBitNumber programNumber) => ProgramNumber = programNumber;
    }
}
