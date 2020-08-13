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

	public sealed class StrobeFlash : Thinker
	{
		public static readonly int StrobeBright = 5;
		public static readonly int FastDark = 15;
		public static readonly int SlowDark = 35;

		private World world;

		private Sector sector;
		private int count;
		private int minLight;
		private int maxLight;
		private int darkTime;
		private int brightTime;

		public StrobeFlash(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			if (--this.count > 0)
			{
				return;
			}

			if (this.sector.LightLevel == this.minLight)
			{
				this.sector.LightLevel = this.maxLight;
				this.count = this.brightTime;
			}
			else
			{
				this.sector.LightLevel = this.minLight;
				this.count = this.darkTime;
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

		public int DarkTime
		{
			get => this.darkTime;
			set => this.darkTime = value;
		}

		public int BrightTime
		{
			get => this.brightTime;
			set => this.brightTime = value;
		}
	}
}
