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

	public sealed class Platform : Thinker
	{
		private World world;

		private Sector sector;
		private Fixed speed;
		private Fixed low;
		private Fixed high;
		private int wait;
		private int count;
		private PlatformState status;
		private PlatformState oldStatus;
		private bool crush;
		private int tag;
		private PlatformType type;

		public Platform(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			var sa = this.world.SectorAction;

			SectorActionResult result;

			switch (this.status)
			{
				case PlatformState.Up:
					result = sa.MovePlane(this.sector, this.speed, this.high, this.crush, 0, 1);

					if (this.type == PlatformType.RaiseAndChange || this.type == PlatformType.RaiseToNearestAndChange)
					{
						if (((this.world.LevelTime + this.sector.Number) & 7) == 0)
						{
							this.world.StartSound(this.sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
						}
					}

					if (result == SectorActionResult.Crushed && !this.crush)
					{
						this.count = this.wait;
						this.status = PlatformState.Down;
						this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
					}
					else
					{
						if (result == SectorActionResult.PastDestination)
						{
							this.count = this.wait;
							this.status = PlatformState.Waiting;
							this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);

							switch (this.type)
							{
								case PlatformType.BlazeDwus:
								case PlatformType.DownWaitUpStay:
									sa.RemoveActivePlatform(this);

									break;

								case PlatformType.RaiseAndChange:
								case PlatformType.RaiseToNearestAndChange:
									sa.RemoveActivePlatform(this);

									break;

								default:
									break;
							}
						}
					}

					break;

				case PlatformState.Down:
					result = sa.MovePlane(this.sector, this.speed, this.low, false, 0, -1);

					if (result == SectorActionResult.PastDestination)
					{
						this.count = this.wait;
						this.status = PlatformState.Waiting;
						this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
					}

					break;

				case PlatformState.Waiting:
					if (--this.count == 0)
					{
						if (this.sector.FloorHeight == this.low)
						{
							this.status = PlatformState.Up;
						}
						else
						{
							this.status = PlatformState.Down;
						}

						this.world.StartSound(this.sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
					}

					break;

				case PlatformState.InStasis:
					break;
			}
		}

		public Sector Sector
		{
			get => this.sector;
			set => this.sector = value;
		}

		public Fixed Speed
		{
			get => this.speed;
			set => this.speed = value;
		}

		public Fixed Low
		{
			get => this.low;
			set => this.low = value;
		}

		public Fixed High
		{
			get => this.high;
			set => this.high = value;
		}

		public int Wait
		{
			get => this.wait;
			set => this.wait = value;
		}

		public int Count
		{
			get => this.count;
			set => this.count = value;
		}

		public PlatformState Status
		{
			get => this.status;
			set => this.status = value;
		}

		public PlatformState OldStatus
		{
			get => this.oldStatus;
			set => this.oldStatus = value;
		}

		public bool Crush
		{
			get => this.crush;
			set => this.crush = value;
		}

		public int Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		public PlatformType Type
		{
			get => this.type;
			set => this.type = value;
		}
	}
}
