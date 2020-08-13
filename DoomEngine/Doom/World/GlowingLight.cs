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
	using Map;

	public sealed class GlowingLight : Thinker
	{
		private static readonly int glowSpeed = 8;

		private World world;

		private Sector sector;
		private int minLight;
		private int maxLight;
		private int direction;

		public GlowingLight(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			switch (this.direction)
			{
				case -1:
					// Down.
					this.sector.LightLevel -= GlowingLight.glowSpeed;

					if (this.sector.LightLevel <= this.minLight)
					{
						this.sector.LightLevel += GlowingLight.glowSpeed;
						this.direction = 1;
					}

					break;

				case 1:
					// Up.
					this.sector.LightLevel += GlowingLight.glowSpeed;

					if (this.sector.LightLevel >= this.maxLight)
					{
						this.sector.LightLevel -= GlowingLight.glowSpeed;
						this.direction = -1;
					}

					break;
			}
		}

		public Sector Sector
		{
			get => this.sector;
			set => this.sector = value;
		}

		public int MinLight
		{
			get => this.minLight;
			set => this.minLight = value;
		}

		public int MaxLight
		{
			get => this.maxLight;
			set => this.maxLight = value;
		}

		public int Direction
		{
			get => this.direction;
			set => this.direction = value;
		}
	}
}
