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

namespace DoomEngine.Doom.World
{
	using Audio;
	using Map;
	using Math;
	using System;
	using System.Collections.Generic;

	public class Specials
	{
		private static readonly int maxButtonCount = 32;
		private static readonly int buttonTime = 35;

		private World world;

		private bool levelTimer;
		private int levelTimeCount;

		private Button[] buttonList;

		private int[] textureTranslation;
		private int[] flatTranslation;

		private LineDef[] scrollLines;

		public Specials(World world)
		{
			this.world = world;

			this.levelTimer = false;

			this.buttonList = new Button[Specials.maxButtonCount];

			for (var i = 0; i < this.buttonList.Length; i++)
			{
				this.buttonList[i] = new Button();
			}

			this.textureTranslation = new int[world.Map.Textures.Count];

			for (var i = 0; i < this.textureTranslation.Length; i++)
			{
				this.textureTranslation[i] = i;
			}

			this.flatTranslation = new int[world.Map.Flats.Count];

			for (var i = 0; i < this.flatTranslation.Length; i++)
			{
				this.flatTranslation[i] = i;
			}
		}

		/// <summary>
		/// After the map has been loaded, scan for specials that spawn thinkers.
		/// </summary>
		public void SpawnSpecials(int levelTimeCount)
		{
			this.levelTimer = true;
			this.levelTimeCount = levelTimeCount;
			this.SpawnSpecials();
		}

		/// <summary>
		/// After the map has been loaded, scan for specials that spawn thinkers.
		/// </summary>
		public void SpawnSpecials()
		{
			// Init special sectors.
			var lc = this.world.LightingChange;
			var sa = this.world.SectorAction;

			foreach (var sector in this.world.Map.Sectors)
			{
				if (sector.Special == 0)
				{
					continue;
				}

				switch ((int) sector.Special)
				{
					case 1:
						// Flickering lights.
						lc.SpawnLightFlash(sector);

						break;

					case 2:
						// Strobe fast.
						lc.SpawnStrobeFlash(sector, StrobeFlash.FastDark, false);

						break;

					case 3:
						// Strobe slow.
						lc.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, false);

						break;

					case 4:
						// Strobe fast / death slime.
						lc.SpawnStrobeFlash(sector, StrobeFlash.FastDark, false);
						sector.Special = (SectorSpecial) 4;

						break;

					case 8:
						// Glowing light.
						lc.SpawnGlowingLight(sector);

						break;

					case 9:
						// Secret sector.
						this.world.TotalSecrets++;

						break;

					case 10:
						// Door close in 30 seconds.
						sa.SpawnDoorCloseIn30(sector);

						break;

					case 12:
						// Sync strobe slow.
						lc.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, true);

						break;

					case 13:
						// Sync strobe fast.
						lc.SpawnStrobeFlash(sector, StrobeFlash.FastDark, true);

						break;

					case 14:
						// Door raise in 5 minutes.
						sa.SpawnDoorRaiseIn5Mins(sector);

						break;

					case 17:
						lc.SpawnFireFlicker(sector);

						break;
				}
			}

			var scrollList = new List<LineDef>();

			foreach (var line in this.world.Map.Lines)
			{
				switch ((int) line.Special)
				{
					case 48:
						// Texture scroll.
						scrollList.Add(line);

						break;
				}
			}

			this.scrollLines = scrollList.ToArray();
		}

		public void ChangeSwitchTexture(LineDef line, bool useAgain)
		{
			if (!useAgain)
			{
				line.Special = 0;
			}

			var frontSide = line.FrontSide;
			var topTexture = frontSide.TopTexture;
			var middleTexture = frontSide.MiddleTexture;
			var bottomTexture = frontSide.BottomTexture;

			var sound = Sfx.SWTCHN;

			// Exit switch?
			if ((int) line.Special == 11)
			{
				sound = Sfx.SWTCHX;
			}

			var switchList = this.world.Map.Textures.SwitchList;

			for (var i = 0; i < switchList.Length; i++)
			{
				if (switchList[i] == topTexture)
				{
					this.world.StartSound(line.SoundOrigin, sound, SfxType.Misc);
					frontSide.TopTexture = switchList[i ^ 1];

					if (useAgain)
					{
						this.StartButton(line, ButtonPosition.Top, switchList[i], Specials.buttonTime);
					}

					return;
				}
				else
				{
					if (switchList[i] == middleTexture)
					{
						this.world.StartSound(line.SoundOrigin, sound, SfxType.Misc);
						frontSide.MiddleTexture = switchList[i ^ 1];

						if (useAgain)
						{
							this.StartButton(line, ButtonPosition.Middle, switchList[i], Specials.buttonTime);
						}

						return;
					}
					else
					{
						if (switchList[i] == bottomTexture)
						{
							this.world.StartSound(line.SoundOrigin, sound, SfxType.Misc);
							frontSide.BottomTexture = switchList[i ^ 1];

							if (useAgain)
							{
								this.StartButton(line, ButtonPosition.Bottom, switchList[i], Specials.buttonTime);
							}

							return;
						}
					}
				}
			}
		}

		private void StartButton(LineDef line, ButtonPosition w, int texture, int time)
		{
			// See if button is already pressed.
			for (var i = 0; i < Specials.maxButtonCount; i++)
			{
				if (this.buttonList[i].Timer != 0 && this.buttonList[i].Line == line)
				{
					return;
				}
			}

			for (var i = 0; i < Specials.maxButtonCount; i++)
			{
				if (this.buttonList[i].Timer == 0)
				{
					this.buttonList[i].Line = line;
					this.buttonList[i].Position = w;
					this.buttonList[i].Texture = texture;
					this.buttonList[i].Timer = time;
					this.buttonList[i].SoundOrigin = line.SoundOrigin;

					return;
				}
			}

			throw new Exception("No button slots left!");
		}

		/// <summary>
		/// Animate planes, scroll walls, etc.
		/// </summary>
		public void Update()
		{
			// Level timer.
			if (this.levelTimer)
			{
				this.levelTimeCount--;

				if (this.levelTimeCount == 0)
				{
					this.world.ExitLevel();
				}
			}

			// Animate flats and textures globally.
			var animations = this.world.Map.Animation.Animations;

			for (var k = 0; k < animations.Length; k++)
			{
				var anim = animations[k];

				for (var i = anim.BasePic; i < anim.BasePic + anim.NumPics; i++)
				{
					var pic = anim.BasePic + ((this.world.LevelTime / anim.Speed + i) % anim.NumPics);

					if (anim.IsTexture)
					{
						this.textureTranslation[i] = pic;
					}
					else
					{
						this.flatTranslation[i] = pic;
					}
				}
			}

			// Animate line specials.
			foreach (var line in this.scrollLines)
			{
				line.FrontSide.TextureOffset += Fixed.One;
			}

			// Do buttons.
			for (var i = 0; i < Specials.maxButtonCount; i++)
			{
				if (this.buttonList[i].Timer > 0)
				{
					this.buttonList[i].Timer--;

					if (this.buttonList[i].Timer == 0)
					{
						switch (this.buttonList[i].Position)
						{
							case ButtonPosition.Top:
								this.buttonList[i].Line.FrontSide.TopTexture = this.buttonList[i].Texture;

								break;

							case ButtonPosition.Middle:
								this.buttonList[i].Line.FrontSide.MiddleTexture = this.buttonList[i].Texture;

								break;

							case ButtonPosition.Bottom:
								this.buttonList[i].Line.FrontSide.BottomTexture = this.buttonList[i].Texture;

								break;
						}

						this.world.StartSound(this.buttonList[i].SoundOrigin, Sfx.SWTCHN, SfxType.Misc, 50);
						this.buttonList[i].Clear();
					}
				}
			}
		}

		public int[] TextureTranslation => this.textureTranslation;
		public int[] FlatTranslation => this.flatTranslation;
	}
}
