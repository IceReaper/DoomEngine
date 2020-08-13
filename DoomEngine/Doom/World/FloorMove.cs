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

	public sealed class FloorMove : Thinker
	{
		private World world;

		private FloorMoveType type;
		private bool crush;
		private Sector sector;
		private int direction;
		private SectorSpecial newSpecial;
		private int texture;
		private Fixed floorDestHeight;
		private Fixed speed;

		public FloorMove(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			SectorActionResult result;

			var sa = this.world.SectorAction;

			result = sa.MovePlane(
				this.sector,
				this.speed,
				this.floorDestHeight,
				this.crush,
				0,
				this.direction);

			if (((this.world.LevelTime + this.sector.Number) & 7) == 0)
			{
				this.world.StartSound(this.sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
			}

			if (result == SectorActionResult.PastDestination)
			{
				this.sector.SpecialData = null;

				if (this.direction == 1)
				{
					switch (this.type)
					{
						case FloorMoveType.DonutRaise:
							this.sector.Special = this.newSpecial;
							this.sector.FloorFlat = this.texture;
							break;
					}
				}
				else if (this.direction == -1)
				{
					switch (this.type)
					{
						case FloorMoveType.LowerAndChange:
							this.sector.Special = this.newSpecial;
							this.sector.FloorFlat = this.texture;
							break;
					}
				}

				this.world.Thinkers.Remove(this);

				this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
			}
		}

		public FloorMoveType Type
		{
			get => this.type;
			set => this.type = value;
		}

		public bool Crush
		{
			get => this.crush;
			set => this.crush = value;
		}

		public Sector Sector
		{
			get => this.sector;
			set => this.sector = value;
		}

		public int Direction
		{
			get => this.direction;
			set => this.direction = value;
		}

		public SectorSpecial NewSpecial
		{
			get => this.newSpecial;
			set => this.newSpecial = value;
		}

		public int Texture
		{
			get => this.texture;
			set => this.texture = value;
		}

		public Fixed FloorDestHeight
		{
			get => this.floorDestHeight;
			set => this.floorDestHeight = value;
		}

		public Fixed Speed
		{
			get => this.speed;
			set => this.speed = value;
		}
	}
}
