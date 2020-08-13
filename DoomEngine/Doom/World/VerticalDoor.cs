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

	public class VerticalDoor : Thinker
	{
		private World world;

		private VerticalDoorType type;
		private Sector sector;
		private Fixed topHeight;
		private Fixed speed;

		// 1 = up, 0 = waiting at top, -1 = down.
		private int direction;

		// Tics to wait at the top.
		private int topWait;

		// When it reaches 0, start going down
		// (keep in case a door going down is reset).
		private int topCountDown;

		public VerticalDoor(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			var sa = this.world.SectorAction;

			SectorActionResult result;

			switch (this.direction)
			{
				case 0:
					// Waiting.
					if (--this.topCountDown == 0)
					{
						switch (this.type)
						{
							case VerticalDoorType.BlazeRaise:
								// Time to go back down.
								this.direction = -1;
								this.world.StartSound(this.sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);

								break;

							case VerticalDoorType.Normal:
								// Time to go back down.
								this.direction = -1;
								this.world.StartSound(this.sector.SoundOrigin, Sfx.DORCLS, SfxType.Misc);

								break;

							case VerticalDoorType.Close30ThenOpen:
								this.direction = 1;
								this.world.StartSound(this.sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);

								break;

							default:
								break;
						}
					}

					break;

				case 2:
					// Initial wait.
					if (--this.topCountDown == 0)
					{
						switch (this.type)
						{
							case VerticalDoorType.RaiseIn5Mins:
								this.direction = 1;
								this.type = VerticalDoorType.Normal;
								this.world.StartSound(this.sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);

								break;

							default:
								break;
						}
					}

					break;

				case -1:
					// Down.
					result = sa.MovePlane(this.sector, this.speed, this.sector.FloorHeight, false, 1, this.direction);

					if (result == SectorActionResult.PastDestination)
					{
						switch (this.type)
						{
							case VerticalDoorType.BlazeRaise:
							case VerticalDoorType.BlazeClose:
								this.sector.SpecialData = null;

								// Unlink and free.
								this.world.Thinkers.Remove(this);
								this.world.StartSound(this.sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);

								break;

							case VerticalDoorType.Normal:
							case VerticalDoorType.Close:
								this.sector.SpecialData = null;

								// Unlink and free.
								this.world.Thinkers.Remove(this);

								break;

							case VerticalDoorType.Close30ThenOpen:
								this.direction = 0;
								this.topCountDown = 35 * 30;

								break;

							default:
								break;
						}
					}
					else if (result == SectorActionResult.Crushed)
					{
						switch (this.type)
						{
							case VerticalDoorType.BlazeClose:
							case VerticalDoorType.Close: // Do not go back up!
								break;

							default:
								this.direction = 1;
								this.world.StartSound(this.sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);

								break;
						}
					}

					break;

				case 1:
					// Up.
					result = sa.MovePlane(this.sector, this.speed, this.topHeight, false, 1, this.direction);

					if (result == SectorActionResult.PastDestination)
					{
						switch (this.type)
						{
							case VerticalDoorType.BlazeRaise:
							case VerticalDoorType.Normal:
								// Wait at top.
								this.direction = 0;
								this.topCountDown = this.topWait;

								break;

							case VerticalDoorType.Close30ThenOpen:
							case VerticalDoorType.BlazeOpen:
							case VerticalDoorType.Open:
								this.sector.SpecialData = null;

								// Unlink and free.
								this.world.Thinkers.Remove(this);

								break;

							default:
								break;
						}
					}

					break;
			}
		}

		public VerticalDoorType Type
		{
			get => this.type;
			set => this.type = value;
		}

		public Sector Sector
		{
			get => this.sector;
			set => this.sector = value;
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

		public int Direction
		{
			get => this.direction;
			set => this.direction = value;
		}

		public int TopWait
		{
			get => this.topWait;
			set => this.topWait = value;
		}

		public int TopCountDown
		{
			get => this.topCountDown;
			set => this.topCountDown = value;
		}
	}
}
