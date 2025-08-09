using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiReverse
{
    internal class MidiReverse
    {
        private static readonly Dictionary<SevenBitNumber, SevenBitNumber> _controlDefaults = new Dictionary<SevenBitNumber, SevenBitNumber>(6);

        static MidiReverse()
        {
            var bytes = new Dictionary<byte, byte>(6)
            {
                { 1, 0 },
                { 7, 100 },
                { 10, 64 },
                { 11, 127 },
                { 64, 0 },
                { 121, 0 }
            };

            foreach (var kv in bytes)
            {
                _controlDefaults.Add(new SevenBitNumber(kv.Key), new SevenBitNumber(kv.Value));
            }
        }

        public MidiFile Reverse(MidiFile midiFile)
        {
            long duration = midiFile.GetNotes().Select(n => n.Time + n.Length).DefaultIfEmpty().Max();

            var reversedTrackChunks = new List<TrackChunk>();

            // Reverse all global events and put them in chunk 0
            var reversedGlobalEvents = GetReversedGlobalEvents(midiFile, duration);

            if (reversedGlobalEvents.Count > 0)
            {
                reversedTrackChunks.Add(reversedGlobalEvents.ToTrackChunk());
            }

            // Reverse notes
            foreach (var originalChunk in midiFile.GetTrackChunks())
            {
                var reversedNotes = GetReversedNotes(originalChunk, duration);
                var newChunk = reversedNotes.ToTrackChunk();
                reversedTrackChunks.Add(newChunk);
            }

            return new MidiFile(reversedTrackChunks) { TimeDivision = midiFile.TimeDivision };
        }

        private List<TimedEvent> GetReversedGlobalEvents(MidiFile midi, long midiDuration)
        {
            var originalEvents = midi.GetTimedEvents().OrderBy(e => e.Time);
            var reversedEvents = new List<TimedEvent>();
            var state = new MidiState();
            SetTempoEvent setTempoEvent;
            ProgramChangeEvent programChangeEvent;
            ControlChangeEvent controlChangeEvent;
            FourBitNumber channel;
            SevenBitNumber programNumber;
            SevenBitNumber controlNumber;
            SevenBitNumber controlValue;
            SevenBitNumber defaultValue;
            MidiEvent newEvent;
            long time;

            foreach (var timedEvent in originalEvents)
            {
                if (timedEvent.Event.EventType == MidiEventType.TimeSignature || timedEvent.Event.EventType == MidiEventType.KeySignature)
                {
                    reversedEvents.Add(timedEvent);
                    continue;
                }

                if (timedEvent.Event.EventType == MidiEventType.SetTempo)
                {
                    setTempoEvent = (SetTempoEvent)timedEvent.Event;
                    newEvent = new SetTempoEvent(state.MicrosecondsPerQuarterNote);
                    state.MicrosecondsPerQuarterNote = setTempoEvent.MicrosecondsPerQuarterNote;
                    time = GetReversedTime(timedEvent, midiDuration);
                    reversedEvents.Add(new TimedEvent(newEvent, time));
                    continue;
                }

                if (timedEvent.Event.EventType == MidiEventType.ProgramChange)
                {
                    programChangeEvent = (ProgramChangeEvent)timedEvent.Event;
                    channel = programChangeEvent.Channel;
                    programNumber = programChangeEvent.ProgramNumber;

                    if (state.ChannelStates.ContainsKey(channel))
                    {
                        newEvent = new ProgramChangeEvent(state.ChannelStates[channel].ProgramNumber) { Channel = channel };
                        state.ChannelStates[channel].ProgramNumber = programNumber;
                    }
                    else
                    {
                        newEvent = new ProgramChangeEvent() { Channel = channel };
                        state.ChannelStates.Add(channel, new ChannelState(programNumber));
                    }

                    time = GetReversedTime(timedEvent, midiDuration);
                    reversedEvents.Add(new TimedEvent(newEvent, time));
                    continue;
                }

                if (timedEvent.Event.EventType == MidiEventType.ControlChange)
                {
                    controlChangeEvent = (ControlChangeEvent)timedEvent.Event;
                    channel = controlChangeEvent.Channel;
                    controlNumber = controlChangeEvent.ControlNumber;
                    controlValue = controlChangeEvent.ControlValue;

                    if (state.ChannelStates.ContainsKey(channel))
                    {
                        if (state.ChannelStates[channel].Controls.ContainsKey(controlNumber))
                        {
                            newEvent = new ControlChangeEvent(controlNumber, state.ChannelStates[channel].Controls[controlNumber]) { Channel = channel };
                            state.ChannelStates[channel].Controls[controlNumber] = controlValue;
                        }
                        else
                        {
                            state.ChannelStates[channel].Controls.Add(controlNumber, controlValue);

                            if (_controlDefaults.TryGetValue(controlNumber, out defaultValue))
                            {
                                newEvent = new ControlChangeEvent(controlNumber, defaultValue) { Channel = channel };
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        state.ChannelStates.Add(channel, new ChannelState());
                        state.ChannelStates[channel].Controls.Add(controlValue, controlNumber);

                        if (_controlDefaults.TryGetValue(controlNumber, out defaultValue))
                        {
                            newEvent = new ControlChangeEvent(controlNumber, defaultValue) { Channel = channel };
                        }
                        else
                        {
                            continue;
                        }
                    }

                    time = GetReversedTime(timedEvent, midiDuration);
                    reversedEvents.Add(new TimedEvent(newEvent, time));
                }
            }

            // Set starting tempo
            if (state.MicrosecondsPerQuarterNote != SetTempoEvent.DefaultMicrosecondsPerQuarterNote)
            {
                newEvent = new SetTempoEvent(state.MicrosecondsPerQuarterNote);
                reversedEvents.Add(new TimedEvent(newEvent, 0L));
            }

            foreach (var channelState in state.ChannelStates)
            {
                // Set starting programs
                programNumber = channelState.Value.ProgramNumber;

                if (programNumber != 0)
                {
                    newEvent = new ProgramChangeEvent(programNumber) { Channel = channelState.Key };
                    reversedEvents.Add(new TimedEvent(newEvent, 0L));
                }

                // Set starting control values
                foreach (var control in channelState.Value.Controls)
                {
                    controlNumber = control.Key;
                    controlValue = control.Value;

                    if (!_controlDefaults.TryGetValue(controlNumber, out defaultValue) || controlValue != defaultValue)
                    {
                        newEvent = new ControlChangeEvent(controlNumber, controlValue) { Channel = channelState.Key };
                        reversedEvents.Add(new TimedEvent(newEvent, 0L));
                    }
                }
            }

            return reversedEvents.OrderBy(e => e.Time).ToList();
        }

        private List<Note> GetReversedNotes(TrackChunk chunk, long midiDuration)
        {
            var manager = chunk.ManageNotes();
            var notes = manager.Objects.ToList();
            var reversedNotes = new List<Note>();

            foreach (var note in notes)
            {
                var noteEnd = note.Time + note.Length;
                var reversedStart = midiDuration - noteEnd;

                var reversedNote = new Note(note.NoteNumber, note.Length, reversedStart)
                {
                    Channel = note.Channel,
                    Velocity = note.Velocity,
                    OffVelocity = note.OffVelocity
                };

                reversedNotes.Add(reversedNote);
            }

            return reversedNotes.OrderBy(note => note.Time).ToList();
        }

        private long GetReversedTime(TimedEvent timedEvent, long midiDuration)
        {
            long newTime = midiDuration - timedEvent.Time;
            return newTime >= 0L ? newTime : 0L;
        }
    }
}
