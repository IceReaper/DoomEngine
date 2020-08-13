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

	public sealed class CeilingMove : Thinker
	{
		private World world;

		private CeilingMoveType type;
		private Sector sector;
		private Fixed bottomHeight;
		private Fixed topHeight;
		private Fixed speed;
		private bool crush;

		// 1 = up, 0 = waiting, -1 = down.
		private int direction;

		// Corresponding sector tag.
		private int tag;

		private int oldDirection;

		public CeilingMove(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			SectorActionResult result;

			var sa = this.world.SectorAction;

			switch (this.direction)
			{
				case 0:
					// In statis.
					break;

				case 1:
					// Up.
					result = sa.MovePlane(
						this.sector,
						this.speed,
						this.topHeight,
						false,
						1,
						this.direction);

					if ((this.world.LevelTime & 7) == 0)
					{
						switch (this.type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
								break;

							default:
								this.world.StartSound(this.sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
								break;
						}
					}

					if (result == SectorActionResult.PastDestination)
					{
						switch (this.type)
						{
							case CeilingMoveType.RaiseToHighest:
								sa.RemoveActiveCeiling(this);
								break;

							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.FastCrushAndRaise:
							case CeilingMoveType.CrushAndRaise:
								if (this.type == CeilingMoveType.SilentCrushAndRaise)
								{
									this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
								}
								this.direction = -1;
								break;

							default:
								break;
						}

					}
					break;

				case -1:
					// Down.
					result = sa.MovePlane(
						this.sector,
						this.speed,
						this.bottomHeight,
						this.crush,
						1,
						this.direction);

					if ((this.world.LevelTime & 7) == 0)
					{
						switch (this.type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
								break;

							default:
								this.world.StartSound(this.sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
								break;
						}
					}

					if (result == SectorActionResult.PastDestination)
					{
						switch (this.type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.CrushAndRaise:
							case CeilingMoveType.FastCrushAndRaise:
								if (this.type == CeilingMoveType.SilentCrushAndRaise)
								{
									this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
								}
								if (this.type == CeilingMoveType.CrushAndRaise)
								{
									this.speed = SectorAction.CeilingSpeed;
								}
								this.direction = 1;
								break;

							case CeilingMoveType.LowerAndCrush:
							case CeilingMoveType.LowerToFloor:
								sa.RemoveActiveCeiling(this);
								break;

							default:
								break;
						}
					}
					else
					{
						if (result == SectorActionResult.Crushed)
						{
							switch (this.type)
							{
								case CeilingMoveType.SilentCrushAndRaise:
								case CeilingMoveType.CrushAndRaise:
								case CeilingMoveType.LowerAndCrush:
									this.speed = SectorAction.CeilingSpeed / 8;
									break;

								default:
									break;
							}
						}
					}
					break;
			}
		}

		public CeilingMoveType Type
		{
			get => this.type;
			set => this.type = value;
		}

		public Sector Sector
		{
			get => this.sector;
			set => this.sector = value;
		}

		public Fixed BottomHeight
		{
			get => this.bottomHeight;
			set => this.bottomHeight = value;
		}

		public Fixed TopHeight
		{
			get => this.topHeight;
			set => this.topHeight = value;
		}

		public Fixed Speed
		{
			get => this.speed;
			set => this.speed = value;
		}

		public bool Crush
		{
			get => this.crush;
			set => this.crush = value;
		}

		public int Direction
		{
			get => this.direction;
			set => this.direction = value;
		}

		public int Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		public int OldDirection
		{
			get => this.oldDirection;
			set => this.oldDirection = value;
		}
	}
}
