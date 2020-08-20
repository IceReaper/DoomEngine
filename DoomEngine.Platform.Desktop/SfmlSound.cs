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
	using Doom.Info;
	using Doom.Math;
	using Doom.World;
	using Platform;
	using SFML.Audio;
	using SFML.System;
	using System;
	using System.IO;
	using System.Runtime.ExceptionServices;

	public sealed class SfmlSound : ISound
	{
		private static readonly int channelCount = 8;

		private static readonly float fastDecay = (float) Math.Pow(0.5, 1.0 / (35 / 5));
		private static readonly float slowDecay = (float) Math.Pow(0.5, 1.0 / 35);

		private static readonly float clipDist = 1200;
		private static readonly float closeDist = 160;
		private static readonly float attenuator = SfmlSound.clipDist - SfmlSound.closeDist;

		private Config config;

		private SoundBuffer[] buffers;
		private float[] amplitudes;

		private Sound[] channels;
		private ChannelInfo[] infos;

		private Sound uiChannel;
		private Sfx uiReserved;

		private Mobj listener;

		private float masterVolumeDecay;

		private DateTime lastUpdate;

		public SfmlSound(Config config)
		{
			try
			{
				Console.Write("Initialize sound: ");

				this.config = config;

				config.audio_soundvolume = Math.Clamp(config.audio_soundvolume, 0, this.MaxVolume);

				this.buffers = new SoundBuffer[DoomInfo.SfxNames.Length];
				this.amplitudes = new float[DoomInfo.SfxNames.Length];

				for (var i = 0; i < DoomInfo.SfxNames.Length; i++)
				{
					var name = "DS" + DoomInfo.SfxNames[i];

					if (!DoomApplication.Instance.FileSystem.Exists(name))
					{
						continue;
					}

					int sampleRate;
					int sampleCount;
					var samples = SfmlSound.GetSamples(name, out sampleRate, out sampleCount);

					if (samples != null)
					{
						this.buffers[i] = new SoundBuffer(samples, 1, (uint) sampleRate);
						this.amplitudes[i] = SfmlSound.GetAmplitude(samples, sampleRate, sampleCount);
					}
				}

				this.channels = new Sound[SfmlSound.channelCount];
				this.infos = new ChannelInfo[SfmlSound.channelCount];

				for (var i = 0; i < this.channels.Length; i++)
				{
					this.channels[i] = new Sound();
					this.infos[i] = new ChannelInfo();
				}

				this.uiChannel = new Sound();
				this.uiReserved = Sfx.NONE;

				this.masterVolumeDecay = (float) config.audio_soundvolume / this.MaxVolume;

				this.lastUpdate = DateTime.MinValue;

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				this.Dispose();
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private static short[] GetSamples(string name, out int sampleRate, out int sampleCount)
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(name));

			if (reader.BaseStream.Length < 8)
			{
				sampleRate = -1;
				sampleCount = -1;

				return null;
			}

			reader.BaseStream.Position = 2;
			sampleRate = reader.ReadUInt16();
			sampleCount = reader.ReadInt32();

			if (sampleCount >= 32 && SfmlSound.ContainsDmxPadding(reader, sampleCount))
			{
				reader.BaseStream.Position += 16;
				sampleCount -= 32;
			}

			if (sampleCount > 0)
			{
				var samples = new short[sampleCount];

				for (var t = 0; t < samples.Length; t++)
				{
					samples[t] = (short) ((reader.ReadByte() - 128) << 8);
				}

				return samples;
			}
			else
			{
				return null;
			}
		}

		// Check if the data contains pad bytes.
		// If the first and last 16 samples are the same,
		// the data should contain pad bytes.
		// https://doomwiki.org/wiki/Sound
		private static bool ContainsDmxPadding(BinaryReader reader, int sampleCount)
		{
			var originalPosition = reader.BaseStream.Position;

			var first = reader.ReadByte();

			for (var i = 1; i < 16; i++)
			{
				if (reader.ReadByte() == first)
					continue;

				reader.BaseStream.Position = originalPosition;

				return false;
			}

			reader.BaseStream.Position = 8 + sampleCount - 16;

			first = reader.ReadByte();

			for (var i = 1; i < 16; i++)
			{
				if (reader.ReadByte() == first)
					continue;

				reader.BaseStream.Position = originalPosition;

				return false;
			}

			reader.BaseStream.Position = originalPosition;

			return true;
		}

		private static float GetAmplitude(short[] samples, int sampleRate, int sampleCount)
		{
			var max = 0;

			if (sampleCount > 0)
			{
				var count = Math.Min(sampleRate / 5, sampleCount);

				for (var t = 0; t < count; t++)
				{
					var a = (int) samples[t];

					if (a < 0)
					{
						a = (short) (-a);
					}

					if (a > max)
					{
						max = a;
					}
				}
			}

			return (float) max / 32768;
		}

		public void SetListener(Mobj listener)
		{
			this.listener = listener;
		}

		public void Update()
		{
			var now = DateTime.Now;

			if ((now - this.lastUpdate).TotalSeconds < 0.01)
			{
				// Don't update so frequently (for timedemo).
				return;
			}

			for (var i = 0; i < this.infos.Length; i++)
			{
				var info = this.infos[i];
				var channel = this.channels[i];

				if (info.Playing != Sfx.NONE)
				{
					if (channel.Status != SoundStatus.Stopped)
					{
						if (info.Type == SfxType.Diffuse)
						{
							info.Priority *= SfmlSound.slowDecay;
						}
						else
						{
							info.Priority *= SfmlSound.fastDecay;
						}

						this.SetParam(channel, info);
					}
					else
					{
						info.Playing = Sfx.NONE;

						if (info.Reserved == Sfx.NONE)
						{
							info.Source = null;
						}
					}
				}

				if (info.Reserved != Sfx.NONE)
				{
					if (info.Playing != Sfx.NONE)
					{
						channel.Stop();
					}

					channel.SoundBuffer = this.buffers[(int) info.Reserved];
					this.SetParam(channel, info);
					channel.Play();
					info.Playing = info.Reserved;
					info.Reserved = Sfx.NONE;
				}
			}

			if (this.uiReserved != Sfx.NONE)
			{
				if (this.uiChannel.Status == SoundStatus.Playing)
				{
					this.uiChannel.Stop();
				}

				this.uiChannel.Volume = 100 * this.masterVolumeDecay;
				this.uiChannel.SoundBuffer = this.buffers[(int) this.uiReserved];
				this.uiChannel.Play();
				this.uiReserved = Sfx.NONE;
			}

			this.lastUpdate = now;
		}

		public void StartSound(Sfx sfx)
		{
			if (this.buffers[(int) sfx] == null)
			{
				return;
			}

			this.uiReserved = sfx;
		}

		public void StartSound(Mobj mobj, Sfx sfx, SfxType type)
		{
			this.StartSound(mobj, sfx, type, 100);
		}

		public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume)
		{
			if (this.buffers[(int) sfx] == null)
			{
				return;
			}

			var x = (mobj.X - this.listener.X).ToFloat();
			var y = (mobj.Y - this.listener.Y).ToFloat();
			var dist = MathF.Sqrt(x * x + y * y);

			float priority;

			if (type == SfxType.Diffuse)
			{
				priority = volume;
			}
			else
			{
				priority = this.amplitudes[(int) sfx] * this.GetDistanceDecay(dist) * volume;
			}

			if (priority < 0.001F)
			{
				return;
			}

			for (var i = 0; i < this.infos.Length; i++)
			{
				var info = this.infos[i];

				if (info.Source == mobj && info.Type == type)
				{
					info.Reserved = sfx;
					info.Priority = priority;
					info.Volume = volume;

					return;
				}
			}

			for (var i = 0; i < this.infos.Length; i++)
			{
				var info = this.infos[i];

				if (info.Reserved == Sfx.NONE && info.Playing == Sfx.NONE)
				{
					info.Reserved = sfx;
					info.Priority = priority;
					info.Source = mobj;
					info.Type = type;
					info.Volume = volume;

					return;
				}
			}

			var minPriority = float.MaxValue;
			var minChannel = -1;

			for (var i = 0; i < this.infos.Length; i++)
			{
				var info = this.infos[i];

				if (info.Priority < minPriority)
				{
					minPriority = info.Priority;
					minChannel = i;
				}
			}

			if (priority >= minPriority)
			{
				var info = this.infos[minChannel];
				info.Reserved = sfx;
				info.Priority = priority;
				info.Source = mobj;
				info.Type = type;
				info.Volume = volume;
			}
		}

		public void StopSound(Mobj mobj)
		{
			for (var i = 0; i < this.infos.Length; i++)
			{
				var info = this.infos[i];

				if (info.Source == mobj)
				{
					info.LastX = info.Source.X;
					info.LastY = info.Source.Y;
					info.Source = null;
					info.Volume /= 5;
				}
			}
		}

		public void Reset()
		{
			for (var i = 0; i < this.infos.Length; i++)
			{
				this.channels[i].Stop();
				this.infos[i].Clear();
			}

			this.listener = null;
		}

		public void Pause()
		{
			for (var i = 0; i < this.infos.Length; i++)
			{
				var channel = this.channels[i];

				if (channel.Status == SoundStatus.Playing && channel.SoundBuffer.Duration - channel.PlayingOffset > Time.FromMilliseconds(200))
				{
					this.channels[i].Pause();
				}
			}
		}

		public void Resume()
		{
			for (var i = 0; i < this.infos.Length; i++)
			{
				var channel = this.channels[i];

				if (channel.Status == SoundStatus.Paused)
				{
					channel.Play();
				}
			}
		}

		private void SetParam(Sound sound, ChannelInfo info)
		{
			if (info.Type == SfxType.Diffuse)
			{
				sound.Position = new Vector3f(0, 1, 0);
				sound.Volume = this.masterVolumeDecay * info.Volume;
			}
			else
			{
				Fixed sourceX;
				Fixed sourceY;

				if (info.Source == null)
				{
					sourceX = info.LastX;
					sourceY = info.LastY;
				}
				else
				{
					sourceX = info.Source.X;
					sourceY = info.Source.Y;
				}

				var x = (sourceX - this.listener.X).ToFloat();
				var y = (sourceY - this.listener.Y).ToFloat();

				if (Math.Abs(x) < 16 && Math.Abs(y) < 16)
				{
					sound.Position = new Vector3f(0, 1, 0);
					sound.Volume = this.masterVolumeDecay * info.Volume;
				}
				else
				{
					var dist = MathF.Sqrt(x * x + y * y);
					var angle = MathF.Atan2(y, x) - (float) this.listener.Angle.ToRadian() + MathF.PI / 2;
					sound.Position = new Vector3f(MathF.Cos(angle), MathF.Sin(angle), 0);
					sound.Volume = this.masterVolumeDecay * this.GetDistanceDecay(dist) * info.Volume;
				}
			}
		}

		private float GetDistanceDecay(float dist)
		{
			if (dist < SfmlSound.closeDist)
			{
				return 1F;
			}
			else
			{
				return Math.Max((SfmlSound.clipDist - dist) / SfmlSound.attenuator, 0F);
			}
		}

		public void Dispose()
		{
			Console.WriteLine("Shutdown sound.");

			if (this.channels != null)
			{
				for (var i = 0; i < this.channels.Length; i++)
				{
					if (this.channels[i] != null)
					{
						this.channels[i].Stop();
						this.channels[i].Dispose();
						this.channels[i] = null;
					}
				}

				this.channels = null;
			}

			if (this.buffers != null)
			{
				for (var i = 0; i < this.buffers.Length; i++)
				{
					if (this.buffers[i] != null)
					{
						this.buffers[i].Dispose();
						this.buffers[i] = null;
					}
				}

				this.buffers = null;
			}

			if (this.uiChannel != null)
			{
				this.uiChannel.Dispose();
				this.uiChannel = null;
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
				return this.config.audio_soundvolume;
			}

			set
			{
				this.config.audio_soundvolume = value;
				this.masterVolumeDecay = (float) this.config.audio_soundvolume / this.MaxVolume;
			}
		}

		private class ChannelInfo
		{
			public Sfx Reserved;
			public Sfx Playing;
			public float Priority;

			public Mobj Source;
			public SfxType Type;
			public int Volume;
			public Fixed LastX;
			public Fixed LastY;

			public void Clear()
			{
				this.Reserved = Sfx.NONE;
				this.Playing = Sfx.NONE;
				this.Priority = 0;
				this.Source = null;
				this.Type = 0;
				this.Volume = 0;
				this.LastX = Fixed.Zero;
				this.LastY = Fixed.Zero;
			}
		}
	}
}
