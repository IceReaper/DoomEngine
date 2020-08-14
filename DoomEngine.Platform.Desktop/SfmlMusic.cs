//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

namespace DoomEngine.Platform.Desktop
{
	using Audio;
	using AudioSynthesis.Midi;
	using AudioSynthesis.Sequencer;
	using AudioSynthesis.Synthesis;
	using Doom.Info;
	using SFML.Audio;
	using SFML.System;
	using System;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;

	public sealed class SfmlMusic : IMusic
	{
		private Config config;

		private MusStream stream;
		private Bgm current;

		public SfmlMusic(Config config, string sfPath)
		{
			try
			{
				Console.Write("Initialize music: ");

				this.config = config;

				this.stream = new MusStream(this, config, sfPath);
				this.current = Bgm.NONE;

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				this.Dispose();
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public void StartMusic(Bgm bgm, bool loop)
		{
			if (bgm == this.current)
			{
				return;
			}

			var decoder = this.ReadData(DoomApplication.Instance.FileSystem.Read("D_" + DoomInfo.BgmNames[(int) bgm]), loop);
			this.stream.SetDecoder(decoder);

			this.current = bgm;
		}

		private IDecoder ReadData(Stream stream, bool loop)
		{
			var reader = new BinaryReader(stream);

			var isMus = reader.ReadBytes(MusDecoder.MusHeader.Length).SequenceEqual(MusDecoder.MusHeader);
			reader.BaseStream.Position = 0;

			if (isMus)
				return new MusDecoder(reader, loop);

			var isMidi = reader.ReadBytes(MidiDecoder.MidiHeader.Length).SequenceEqual(MidiDecoder.MidiHeader);
			reader.BaseStream.Position = 0;

			if (isMidi)
				return new MidiDecoder(reader, loop);

			throw new Exception("Unknown format!");
		}

		public void Dispose()
		{
			Console.WriteLine("Shutdown music.");

			if (this.stream != null)
			{
				this.stream.Stop();
				this.stream.Dispose();
				this.stream = null;
			}
		}

		public int MaxVolume
		{
			get
			{
				return 15;
			}
		}

		public int Volume
		{
			get
			{
				return this.config.audio_musicvolume;
			}

			set
			{
				this.config.audio_musicvolume = value;
			}
		}

		private class MusStream : SoundStream
		{
			private SfmlMusic parent;
			private Config config;

			private Synthesizer synthesizer;
			private int synthBufferLength;
			private int stepCount;
			private int batchLength;

			private IDecoder current;
			private IDecoder reserved;

			private short[] batch;

			public MusStream(SfmlMusic parent, Config config, string sfPath)
			{
				this.parent = parent;
				this.config = config;

				config.audio_musicvolume = Math.Clamp(config.audio_musicvolume, 0, parent.MaxVolume);

				this.synthesizer = new Synthesizer(MusDecoder.SampleRate, 2, MusDecoder.BufferLength, 1);
				this.synthesizer.LoadBank(sfPath);
				this.synthBufferLength = this.synthesizer.sampleBuffer.Length;

				var synthBufferDuration = (double) (this.synthBufferLength / 2) / MusDecoder.SampleRate;
				this.stepCount = (int) Math.Ceiling(0.02 / synthBufferDuration);
				this.batchLength = this.synthBufferLength * this.stepCount;
				this.batch = new short[this.batchLength];

				this.Initialize(2, (uint) MusDecoder.SampleRate);
			}

			public void SetDecoder(IDecoder decoder)
			{
				this.reserved = decoder;

				if (this.Status == SoundStatus.Stopped)
				{
					this.Play();
				}
			}

			protected override bool OnGetData(out short[] samples)
			{
				if (this.reserved != this.current)
				{
					this.synthesizer.NoteOffAll(true);
					this.synthesizer.ResetSynthControls();
					this.current = this.reserved;
				}

				var a = 32768 * (6.0F * this.config.audio_musicvolume / this.parent.MaxVolume);

				//
				// Due to a design error, this implementation makes the music
				// playback speed a bit slower.
				// The slowdown is around 1 sec per minute, so I hope no one
				// will notice.
				//

				var t = 0;

				for (var i = 0; i < this.stepCount; i++)
				{
					this.current.FillBuffer(this.synthesizer);
					var buffer = this.synthesizer.sampleBuffer;

					for (var j = 0; j < buffer.Length; j++)
					{
						var sample = (int) (a * buffer[j]);

						if (sample < short.MinValue)
						{
							sample = short.MinValue;
						}
						else if (sample > short.MaxValue)
						{
							sample = short.MaxValue;
						}

						this.batch[t++] = (short) sample;
					}
				}

				samples = this.batch;

				return true;
			}

			protected override void OnSeek(Time timeOffset)
			{
			}
		}

		private interface IDecoder
		{
			void FillBuffer(Synthesizer synthesizer);
		}

		private class MusDecoder : IDecoder
		{
			public static readonly int SampleRate = 44100;
			public static readonly int BufferLength = MusDecoder.SampleRate / 140;

			public static readonly byte[] MusHeader = new byte[] {(byte) 'M', (byte) 'U', (byte) 'S', 0x1A};

			private BinaryReader reader;
			private bool loop;

			private int scoreLength;
			private int scoreStart;
			private int channelCount;
			private int channelCount2;
			private int instrumentCount;
			private int[] instruments;

			private MusEvent[] events;
			private int eventCount;

			private int[] lastVolume;
			private int delay;

			public MusDecoder(BinaryReader reader, bool loop)
			{
				this.reader = reader;

				reader.BaseStream.Position = 4;
				this.loop = loop;

				this.scoreLength = reader.ReadUInt16();
				this.scoreStart = reader.ReadUInt16();
				this.channelCount = reader.ReadUInt16();
				this.channelCount2 = reader.ReadUInt16();
				this.instrumentCount = reader.ReadUInt16();
				this.instruments = new int[this.instrumentCount];

				reader.BaseStream.Position += 2;

				for (var i = 0; i < this.instruments.Length; i++)
				{
					this.instruments[i] = reader.ReadUInt16();
				}

				this.events = new MusEvent[128];

				for (var i = 0; i < this.events.Length; i++)
				{
					this.events[i] = new MusEvent();
				}

				this.eventCount = 0;

				this.lastVolume = new int[16];

				this.Reset();
			}

			public void FillBuffer(Synthesizer synthesizer)
			{
				if (this.delay > 0)
				{
					this.delay--;
				}

				if (this.delay == 0)
				{
					this.delay = this.ReadSingleEventGroup();
					this.SendEvents(synthesizer);

					if (this.delay == -1)
					{
						synthesizer.NoteOffAll(true);

						if (this.loop)
						{
							this.Reset();
						}
					}
				}

				synthesizer.GetNext();
			}

			private void Reset()
			{
				for (var i = 0; i < this.lastVolume.Length; i++)
				{
					this.lastVolume[i] = 0;
				}

				this.reader.BaseStream.Position = this.scoreStart;

				this.delay = 0;
			}

			private int ReadSingleEventGroup()
			{
				this.eventCount = 0;

				while (true)
				{
					var result = this.ReadSingleEvent();

					if (result == ReadResult.EndOfGroup)
					{
						break;
					}
					else if (result == ReadResult.EndOfFile)
					{
						return -1;
					}
				}

				var time = 0;

				while (true)
				{
					var value = this.reader.ReadByte();
					time = time * 128 + (value & 127);

					if ((value & 128) == 0)
					{
						break;
					}
				}

				return time;
			}

			private ReadResult ReadSingleEvent()
			{
				var value = this.reader.ReadByte();
				var channelNumber = value & 0xF;

				if (channelNumber == 15)
				{
					channelNumber = 9;
				}

				var eventType = (value & 0x70) >> 4;
				var last = (value >> 7) != 0;

				var me = this.events[this.eventCount];
				this.eventCount++;

				switch (eventType)
				{
					case 0: // RELEASE NOTE
						me.Type = 0;
						me.Channel = channelNumber;

						var releaseNote = this.reader.ReadByte();

						me.Data1 = releaseNote;
						me.Data2 = 0;

						break;

					case 1: // PLAY NOTE
						me.Type = 1;
						me.Channel = channelNumber;

						var playNote = this.reader.ReadByte();
						var noteNumber = playNote & 127;
						var noteVolume = (playNote & 128) != 0 ? this.reader.ReadByte() : -1;

						me.Data1 = noteNumber;

						if (noteVolume == -1)
						{
							me.Data2 = this.lastVolume[channelNumber];
						}
						else
						{
							me.Data2 = noteVolume;
							this.lastVolume[channelNumber] = noteVolume;
						}

						break;

					case 2: // PITCH WHEEL
						me.Type = 2;
						me.Channel = channelNumber;

						var pitchWheel = this.reader.ReadByte();

						var pw2 = (pitchWheel << 7) / 2;
						var pw1 = pw2 & 127;
						pw2 >>= 7;
						me.Data1 = pw1;
						me.Data2 = pw2;

						break;

					case 3: // SYSTEM EVENT
						me.Type = 3;
						me.Channel = -1;

						var systemEvent = this.reader.ReadByte();
						me.Data1 = systemEvent;
						me.Data2 = 0;

						break;

					case 4: // CONTROL CHANGE
						me.Type = 4;
						me.Channel = channelNumber;

						var controllerNumber = this.reader.ReadByte();
						var controllerValue = this.reader.ReadByte();

						me.Data1 = controllerNumber;
						me.Data2 = controllerValue;

						break;

					case 6: // END OF FILE
						return ReadResult.EndOfFile;

					default:
						throw new Exception("Unknown event type!");
				}

				if (last)
				{
					return ReadResult.EndOfGroup;
				}
				else
				{
					return ReadResult.Ongoing;
				}
			}

			private void SendEvents(Synthesizer synthesizer)
			{
				for (var i = 0; i < this.eventCount; i++)
				{
					var me = this.events[i];

					switch (me.Type)
					{
						case 0: // RELEASE NOTE
							synthesizer.NoteOff(me.Channel, me.Data1);

							break;

						case 1: // PLAY NOTE
							synthesizer.NoteOn(me.Channel, me.Data1, me.Data2);

							break;

						case 2: // PITCH WHEEL
							synthesizer.ProcessMidiMessage(me.Channel, 0xE0, me.Data1, me.Data2);

							break;

						case 3: // SYSTEM EVENT
							switch (me.Data1)
							{
								case 11: // ALL NOTES OFF
									synthesizer.NoteOffAll(true);

									break;

								case 14: // RESET ALL CONTROLS
									synthesizer.ResetSynthControls();

									break;
							}

							break;

						case 4: // CONTROL CHANGE
							switch (me.Data1)
							{
								case 0: // PROGRAM CHANGE
									if (me.Channel == 9)
									{
										break;
									}

									synthesizer.ProcessMidiMessage(me.Channel, 0xC0, me.Data2, 0);

									break;

								case 1: // BANK SELECTION
									synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x00, me.Data2);

									break;

								case 2: // MODULATION
									synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x01, me.Data2);

									break;

								case 3: // VOLUME
									synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x07, me.Data2);

									break;

								case 4: // PAN
									synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x0A, me.Data2);

									break;

								case 5: // EXPRESSION
									synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x0B, me.Data2);

									break;

								case 8: // PEDAL
									synthesizer.ProcessMidiMessage(me.Channel, 0xB0, 0x40, me.Data2);

									break;
							}

							break;
					}
				}
			}

			private class MusEvent
			{
				public int Type;
				public int Channel;
				public int Data1;
				public int Data2;
			}

			private enum ReadResult
			{
				Ongoing,
				EndOfGroup,
				EndOfFile
			}
		}

		private class MidiDecoder : IDecoder
		{
			public static readonly byte[] MidiHeader = new byte[] {(byte) 'M', (byte) 'T', (byte) 'h', (byte) 'd'};

			private MidiFile midi;
			private MidiFileSequencer sequencer;

			private bool loop;

			public MidiDecoder(BinaryReader reader, bool loop)
			{
				this.midi = new MidiFile(reader.BaseStream);

				this.loop = loop;
			}

			public void FillBuffer(Synthesizer synthesizer)
			{
				if (this.sequencer == null)
				{
					this.sequencer = new MidiFileSequencer(synthesizer);
					this.sequencer.LoadMidi(this.midi);
					this.sequencer.Play();
				}

				this.sequencer.FillMidiEventQueue(this.loop);
				synthesizer.GetNext();
			}
		}
	}
}
