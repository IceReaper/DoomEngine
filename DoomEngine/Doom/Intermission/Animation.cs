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

namespace DoomEngine.Doom.Intermission
{
	using System.Collections.Generic;

	public sealed class Animation
	{
		private Intermission im;
		private int number;

		private AnimationType type;
		private int period;
		private int frameCount;
		private int locationX;
		private int locationY;
		private int data;
		private string[] patches;
		private int patchNumber;
		private int nextTic;

		public Animation(Intermission intermission, AnimationInfo info, int number)
		{
			this.im = intermission;
			this.number = number;

			this.type = info.Type;
			this.period = info.Period;
			this.frameCount = info.Count;
			this.locationX = info.X;
			this.locationY = info.Y;
			this.data = info.Data;

			this.patches = new string[this.frameCount];

			for (var i = 0; i < this.frameCount; i++)
			{
				// MONDO HACK!
				if (this.im.Info.Episode != 1 || number != 8)
				{
					this.patches[i] = "WIA" + this.im.Info.Episode + number.ToString("00") + i.ToString("00");
				}
				else
				{
					// HACK ALERT!
					this.patches[i] = "WIA104" + i.ToString("00");
				}
			}
		}

		public void Reset(int bgCount)
		{
			this.patchNumber = -1;

			// Specify the next time to draw it.
			if (this.type == AnimationType.Always)
			{
				this.nextTic = bgCount + 1 + (this.im.Random.Next() % this.period);
			}
			else if (this.type == AnimationType.Random)
			{
				this.nextTic = bgCount + 1 + (this.im.Random.Next() % this.data);
			}
			else if (this.type == AnimationType.Level)
			{
				this.nextTic = bgCount + 1;
			}
		}

		public void Update(int bgCount)
		{
			if (bgCount == this.nextTic)
			{
				switch (this.type)
				{
					case AnimationType.Always:
						if (++this.patchNumber >= this.frameCount)
						{
							this.patchNumber = 0;
						}

						this.nextTic = bgCount + this.period;

						break;

					case AnimationType.Random:
						this.patchNumber++;

						if (this.patchNumber == this.frameCount)
						{
							this.patchNumber = -1;
							this.nextTic = bgCount + (this.im.Random.Next() % this.data);
						}
						else
						{
							this.nextTic = bgCount + this.period;
						}

						break;

					case AnimationType.Level:
						// Gawd-awful hack for level anims.
						if (!(this.im.State == IntermissionState.StatCount && this.number == 7) && this.im.Info.NextLevel == this.Data)
						{
							this.patchNumber++;

							if (this.patchNumber == this.frameCount)
							{
								this.patchNumber--;
							}

							this.nextTic = bgCount + this.period;
						}

						break;
				}
			}
		}

		public int LocationX => this.locationX;
		public int LocationY => this.locationY;
		public int Data => this.data;
		public IReadOnlyList<string> Patches => this.patches;
		public int PatchNumber => this.patchNumber;
	}
}
