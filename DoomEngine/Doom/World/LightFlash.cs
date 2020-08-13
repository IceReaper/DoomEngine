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

	public sealed class LightFlash : Thinker
	{
		private World world;

		private Sector sector;
		private int count;
		private int maxLight;
		private int minLight;
		private int maxTime;
		private int minTime;

		public LightFlash(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			if (--this.count > 0)
			{
				return;
			}

			if (this.sector.LightLevel == this.maxLight)
			{
				this.sector.LightLevel = this.minLight;
				this.count = (this.world.Random.Next() & this.minTime) + 1;
			}
			else
			{
				this.sector.LightLevel = this.maxLight;
				this.count = (this.world.Random.Next() & this.maxTime) + 1;
			}
		}

		public Sector Sector
		{
			get => this.sector;
			set => this.sector = value;
		}

		public int Count
		{
			get => this.count;
			set => this.count = value;
		}

		public int MaxLight
		{
			get => this.maxLight;
			set => this.maxLight = value;
		}

		public int MinLight
		{
			get => this.minLight;
			set => this.minLight = value;
		}

		public int MaxTime
		{
			get => this.maxTime;
			set => this.maxTime = value;
		}

		public int MinTime
		{
			get => this.minTime;
			set => this.minTime = value;
		}
	}
}
