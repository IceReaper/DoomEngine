﻿//
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
	using Info;
	using Map;
	using Math;
	using System;

	public sealed class SectorAction
	{
		//
		// SECTOR HEIGHT CHANGING
		// After modifying a sectors floor or ceiling height,
		// call this routine to adjust the positions
		// of all things that touch the sector.
		//
		// If anything doesn't fit anymore, true will be returned.
		// If crunch is true, they will take damage
		// as they are being crushed.
		// If Crunch is false, you should set the sector height back
		// the way it was and call P_ChangeSector again
		// to undo the changes.
		//

		private World world;

		public SectorAction(World world)
		{
			this.world = world;

			this.InitSectorChange();
		}

		private bool crushChange;
		private bool noFit;
		private Func<Mobj, bool> crushThingFunc;

		private void InitSectorChange()
		{
			this.crushThingFunc = this.CrushThing;
		}

		private bool ThingHeightClip(Mobj thing)
		{
			var onFloor = (thing.Z == thing.FloorZ);

			var tm = this.world.ThingMovement;

			tm.CheckPosition(thing, thing.X, thing.Y);

			// What about stranding a monster partially off an edge?

			thing.FloorZ = tm.CurrentFloorZ;
			thing.CeilingZ = tm.CurrentCeilingZ;

			if (onFloor)
			{
				// Walking monsters rise and fall with the floor.
				thing.Z = thing.FloorZ;
			}
			else
			{
				// Don't adjust a floating monster unless forced to.
				if (thing.Z + thing.Height > thing.CeilingZ)
				{
					thing.Z = thing.CeilingZ - thing.Height;
				}
			}

			if (thing.CeilingZ - thing.FloorZ < thing.Height)
			{
				return false;
			}

			return true;
		}

		private bool CrushThing(Mobj thing)
		{
			if (this.ThingHeightClip(thing))
			{
				// Keep checking.
				return true;
			}

			// Crunch bodies to giblets.
			if (thing.Health <= 0)
			{
				thing.SetState(MobjState.Gibs);
				thing.Flags &= ~MobjFlags.Solid;
				thing.Height = Fixed.Zero;
				thing.Radius = Fixed.Zero;

				// Keep checking.
				return true;
			}

			// Crunch dropped items.
			if ((thing.Flags & MobjFlags.Dropped) != 0)
			{
				this.world.ThingAllocation.RemoveMobj(thing);

				// Keep checking.
				return true;
			}

			if ((thing.Flags & MobjFlags.Shootable) == 0)
			{
				// Assume it is bloody gibs or something.
				return true;
			}

			this.noFit = true;

			if (this.crushChange && (this.world.LevelTime & 3) == 0)
			{
				this.world.ThingInteraction.DamageMobj(thing, null, null, 10);

				// Spray blood in a random direction.
				var blood = this.world.ThingAllocation.SpawnMobj(thing.X, thing.Y, thing.Z + thing.Height / 2, MobjType.Blood);

				var random = this.world.Random;
				blood.MomX = new Fixed((random.Next() - random.Next()) << 12);
				blood.MomY = new Fixed((random.Next() - random.Next()) << 12);
			}

			// Keep checking (crush other things).	
			return true;
		}

		private bool ChangeSector(Sector sector, bool crunch)
		{
			this.noFit = false;
			this.crushChange = crunch;

			var bm = this.world.Map.BlockMap;
			var blockBox = sector.BlockBox;

			// Re-check heights for all things near the moving sector.
			for (var x = blockBox.Left(); x <= blockBox.Right(); x++)
			{
				for (var y = blockBox.Bottom(); y <= blockBox.Top(); y++)
				{
					bm.IterateThings(x, y, this.crushThingFunc);
				}
			}

			return this.noFit;
		}

		/// <summary>
		/// Move a plane (floor or ceiling) and check for crushing.
		/// </summary>
		public SectorActionResult MovePlane(Sector sector, Fixed speed, Fixed dest, bool crush, int floorOrCeiling, int direction)
		{
			switch (floorOrCeiling)
			{
				case 0:
					// Floor.
					switch (direction)
					{
						case -1:
							// Down.
							if (sector.FloorHeight - speed < dest)
							{
								var lastPos = sector.FloorHeight;
								sector.FloorHeight = dest;

								if (this.ChangeSector(sector, crush))
								{
									sector.FloorHeight = lastPos;
									this.ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								var lastPos = sector.FloorHeight;
								sector.FloorHeight -= speed;

								if (this.ChangeSector(sector, crush))
								{
									sector.FloorHeight = lastPos;
									this.ChangeSector(sector, crush);

									return SectorActionResult.Crushed;
								}
							}

							break;

						case 1:
							// Up.
							if (sector.FloorHeight + speed > dest)
							{
								var lastPos = sector.FloorHeight;
								sector.FloorHeight = dest;

								if (this.ChangeSector(sector, crush))
								{
									sector.FloorHeight = lastPos;
									this.ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								// Could get crushed.
								var lastPos = sector.FloorHeight;
								sector.FloorHeight += speed;

								if (this.ChangeSector(sector, crush))
								{
									if (crush)
									{
										return SectorActionResult.Crushed;
									}

									sector.FloorHeight = lastPos;
									this.ChangeSector(sector, crush);

									return SectorActionResult.Crushed;
								}
							}

							break;
					}

					break;

				case 1:
					// Ceiling.
					switch (direction)
					{
						case -1:
							// Down.
							if (sector.CeilingHeight - speed < dest)
							{
								var lastPos = sector.CeilingHeight;
								sector.CeilingHeight = dest;

								if (this.ChangeSector(sector, crush))
								{
									sector.CeilingHeight = lastPos;
									this.ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								// Could get crushed.
								var lastPos = sector.CeilingHeight;
								sector.CeilingHeight -= speed;

								if (this.ChangeSector(sector, crush))
								{
									if (crush)
									{
										return SectorActionResult.Crushed;
									}

									sector.CeilingHeight = lastPos;
									this.ChangeSector(sector, crush);

									return SectorActionResult.Crushed;
								}
							}

							break;

						case 1:
							// UP
							if (sector.CeilingHeight + speed > dest)
							{
								var lastPos = sector.CeilingHeight;
								sector.CeilingHeight = dest;

								if (this.ChangeSector(sector, crush))
								{
									sector.CeilingHeight = lastPos;
									this.ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								sector.CeilingHeight += speed;
								this.ChangeSector(sector, crush);
							}

							break;
					}

					break;
			}

			return SectorActionResult.OK;
		}

		private Sector GetNextSector(LineDef line, Sector sector)
		{
			if ((line.Flags & LineFlags.TwoSided) == 0)
			{
				return null;
			}

			if (line.FrontSector == sector)
			{
				return line.BackSector;
			}

			return line.FrontSector;
		}

		private Fixed FindLowestFloorSurrounding(Sector sector)
		{
			var floor = sector.FloorHeight;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = this.GetNextSector(check, sector);

				if (other == null)
				{
					continue;
				}

				if (other.FloorHeight < floor)
				{
					floor = other.FloorHeight;
				}
			}

			return floor;
		}

		private Fixed FindHighestFloorSurrounding(Sector sector)
		{
			var floor = Fixed.FromInt(-500);

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = this.GetNextSector(check, sector);

				if (other == null)
				{
					continue;
				}

				if (other.FloorHeight > floor)
				{
					floor = other.FloorHeight;
				}
			}

			return floor;
		}

		private Fixed FindLowestCeilingSurrounding(Sector sector)
		{
			var height = Fixed.MaxValue;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = this.GetNextSector(check, sector);

				if (other == null)
				{
					continue;
				}

				if (other.CeilingHeight < height)
				{
					height = other.CeilingHeight;
				}
			}

			return height;
		}

		private Fixed FindHighestCeilingSurrounding(Sector sector)
		{
			var height = Fixed.Zero;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = this.GetNextSector(check, sector);

				if (other == null)
				{
					continue;
				}

				if (other.CeilingHeight > height)
				{
					height = other.CeilingHeight;
				}
			}

			return height;
		}

		private int FindSectorFromLineTag(LineDef line, int start)
		{
			var sectors = this.world.Map.Sectors;

			for (var i = start + 1; i < sectors.Length; i++)
			{
				if (sectors[i].Tag == line.Tag)
				{
					return i;
				}
			}

			return -1;
		}

		////////////////////////////////////////////////////////////
		// Door
		////////////////////////////////////////////////////////////

		private static readonly Fixed doorSpeed = Fixed.FromInt(2);
		private static readonly int doorWait = 150;

		/// <summary>
		/// Open a door manually, no tag value.
		/// </summary>
		public void DoLocalDoor(LineDef line, Mobj thing)
		{
			//	Check for locks.
			var player = thing.Player;

			switch ((int) line.Special)
			{
				// Blue Lock.
				case 26:
				case 32:
					if (player == null)
					{
						return;
					}

					if (!player.Cards[(int) CardType.BlueCard] && !player.Cards[(int) CardType.BlueSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_BLUEK);
						this.world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);

						return;
					}

					break;

				// Yellow Lock.
				case 27:
				case 34:
					if (player == null)
					{
						return;
					}

					if (!player.Cards[(int) CardType.YellowCard] && !player.Cards[(int) CardType.YellowSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_YELLOWK);
						this.world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);

						return;
					}

					break;

				// Red Lock.
				case 28:
				case 33:
					if (player == null)
					{
						return;
					}

					if (!player.Cards[(int) CardType.RedCard] && !player.Cards[(int) CardType.RedSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_REDK);
						this.world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);

						return;
					}

					break;
			}

			var sector = line.BackSide.Sector;

			// If the sector has an active thinker, use it.
			if (sector.SpecialData != null)
			{
				var door = (VerticalDoor) sector.SpecialData;

				switch ((int) line.Special)
				{
					// Only for "raise" doors, not "open"s.
					case 1:
					case 26:
					case 27:
					case 28:
					case 117:
						if (door.Direction == -1)
						{
							// Go back up.
							door.Direction = 1;
						}
						else
						{
							if (thing.Player == null)
							{
								// Bad guys never close doors.
								return;
							}

							// Start going down immediately.
							door.Direction = -1;
						}

						return;
				}
			}

			// For proper sound.
			switch ((int) line.Special)
			{
				// Blazing door raise.
				case 117:

				// Blazing door open.
				case 118:
					this.world.StartSound(sector.SoundOrigin, Sfx.BDOPN, SfxType.Misc);

					break;

				// Normal door sound.
				case 1:
				case 31:
					this.world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);

					break;

				// Locked door sound.
				default:
					this.world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);

					break;
			}

			// New door thinker.
			var newDoor = new VerticalDoor(this.world);
			this.world.Thinkers.Add(newDoor);
			sector.SpecialData = newDoor;
			newDoor.Sector = sector;
			newDoor.Direction = 1;
			newDoor.Speed = SectorAction.doorSpeed;
			newDoor.TopWait = SectorAction.doorWait;

			switch ((int) line.Special)
			{
				case 1:
				case 26:
				case 27:
				case 28:
					newDoor.Type = VerticalDoorType.Normal;

					break;

				case 31:
				case 32:
				case 33:
				case 34:
					newDoor.Type = VerticalDoorType.Open;
					line.Special = 0;

					break;

				// Blazing door raise.
				case 117:
					newDoor.Type = VerticalDoorType.BlazeRaise;
					newDoor.Speed = SectorAction.doorSpeed * 4;

					break;

				// Blazing door open.
				case 118:
					newDoor.Type = VerticalDoorType.BlazeOpen;
					line.Special = 0;
					newDoor.Speed = SectorAction.doorSpeed * 4;

					break;
			}

			// Find the top and bottom of the movement range.
			newDoor.TopHeight = this.FindLowestCeilingSurrounding(sector);
			newDoor.TopHeight -= Fixed.FromInt(4);
		}

		public bool DoDoor(LineDef line, VerticalDoorType type)
		{
			var sectors = this.world.Map.Sectors;
			var setcorNumber = -1;
			var result = false;

			while ((setcorNumber = this.FindSectorFromLineTag(line, setcorNumber)) >= 0)
			{
				var sector = sectors[setcorNumber];

				if (sector.SpecialData != null)
				{
					continue;
				}

				result = true;

				// New door thinker.
				var door = new VerticalDoor(this.world);
				this.world.Thinkers.Add(door);
				sector.SpecialData = door;
				door.Sector = sector;
				door.Type = type;
				door.TopWait = SectorAction.doorWait;
				door.Speed = SectorAction.doorSpeed;

				switch (type)
				{
					case VerticalDoorType.BlazeClose:
						door.TopHeight = this.FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						door.Direction = -1;
						door.Speed = SectorAction.doorSpeed * 4;
						this.world.StartSound(door.Sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);

						break;

					case VerticalDoorType.Close:
						door.TopHeight = this.FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						door.Direction = -1;
						this.world.StartSound(door.Sector.SoundOrigin, Sfx.DORCLS, SfxType.Misc);

						break;

					case VerticalDoorType.Close30ThenOpen:
						door.TopHeight = sector.CeilingHeight;
						door.Direction = -1;
						this.world.StartSound(door.Sector.SoundOrigin, Sfx.DORCLS, SfxType.Misc);

						break;

					case VerticalDoorType.BlazeRaise:
					case VerticalDoorType.BlazeOpen:
						door.Direction = 1;
						door.TopHeight = this.FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						door.Speed = SectorAction.doorSpeed * 4;

						if (door.TopHeight != sector.CeilingHeight)
						{
							this.world.StartSound(door.Sector.SoundOrigin, Sfx.BDOPN, SfxType.Misc);
						}

						break;

					case VerticalDoorType.Normal:
					case VerticalDoorType.Open:
						door.Direction = 1;
						door.TopHeight = this.FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);

						if (door.TopHeight != sector.CeilingHeight)
						{
							this.world.StartSound(door.Sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
						}

						break;

					default:
						break;
				}
			}

			return result;
		}

		public bool DoLockedDoor(LineDef line, VerticalDoorType type, Mobj thing)
		{
			var player = thing.Player;

			if (player == null)
			{
				return false;
			}

			switch ((int) line.Special)
			{
				// Blue Lock.
				case 99:
				case 133:
					if (player == null)
					{
						return false;
					}

					if (!player.Cards[(int) CardType.BlueCard] && !player.Cards[(int) CardType.BlueSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_BLUEO);
						this.world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);

						return false;
					}

					break;

				// Red Lock.
				case 134:
				case 135:
					if (player == null)
					{
						return false;
					}

					if (!player.Cards[(int) CardType.RedCard] && !player.Cards[(int) CardType.RedSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_REDO);
						this.world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);

						return false;
					}

					break;

				// Yellow Lock.
				case 136:
				case 137:
					if (player == null)
					{
						return false;
					}

					if (!player.Cards[(int) CardType.YellowCard] && !player.Cards[(int) CardType.YellowSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_YELLOWO);
						this.world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);

						return false;
					}

					break;
			}

			return this.DoDoor(line, type);
		}

		////////////////////////////////////////////////////////////
		// Platform
		////////////////////////////////////////////////////////////

		// In plutonia MAP23, number of adjoining sectors can be 44.
		private static readonly int maxAdjoiningSectorCount = 64;
		private Fixed[] heightList = new Fixed[SectorAction.maxAdjoiningSectorCount];

		private Fixed FindNextHighestFloor(Sector sector, Fixed currentHeight)
		{
			var height = currentHeight;
			var h = 0;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = this.GetNextSector(check, sector);

				if (other == null)
				{
					continue;
				}

				if (other.FloorHeight > height)
				{
					this.heightList[h++] = other.FloorHeight;
				}

				// Check for overflow.
				if (h >= this.heightList.Length)
				{
					// Exit.
					throw new Exception("Too many adjoining sectors!");
				}
			}

			// Find lowest height in list.
			if (h == 0)
			{
				return currentHeight;
			}

			var min = this.heightList[0];

			// Range checking? 
			for (var i = 1; i < h; i++)
			{
				if (this.heightList[i] < min)
				{
					min = this.heightList[i];
				}
			}

			return min;
		}

		private static readonly int platformWait = 3;
		private static readonly Fixed platformSpeed = Fixed.One;

		public bool DoPlatform(LineDef line, PlatformType type, int amount)
		{
			//	Activate all <type> plats that are in stasis.
			switch (type)
			{
				case PlatformType.PerpetualRaise:
					this.ActivateInStasis(line.Tag);

					break;

				default:
					break;
			}

			var sectors = this.world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = this.FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				if (sector.SpecialData != null)
				{
					continue;
				}

				result = true;

				// Find lowest and highest floors around sector.
				var plat = new Platform(this.world);
				this.world.Thinkers.Add(plat);
				plat.Type = type;
				plat.Sector = sector;
				plat.Sector.SpecialData = plat;
				plat.Crush = false;
				plat.Tag = line.Tag;

				switch (type)
				{
					case PlatformType.RaiseToNearestAndChange:
						plat.Speed = SectorAction.platformSpeed / 2;
						sector.FloorFlat = line.FrontSide.Sector.FloorFlat;
						plat.High = this.FindNextHighestFloor(sector, sector.FloorHeight);
						plat.Wait = 0;
						plat.Status = PlatformState.Up;

						// No more damage, if applicable.
						sector.Special = 0;
						this.world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);

						break;

					case PlatformType.RaiseAndChange:
						plat.Speed = SectorAction.platformSpeed / 2;
						sector.FloorFlat = line.FrontSide.Sector.FloorFlat;
						plat.High = sector.FloorHeight + amount * Fixed.One;
						plat.Wait = 0;
						plat.Status = PlatformState.Up;
						this.world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);

						break;

					case PlatformType.DownWaitUpStay:
						plat.Speed = SectorAction.platformSpeed * 4;
						plat.Low = this.FindLowestFloorSurrounding(sector);

						if (plat.Low > sector.FloorHeight)
						{
							plat.Low = sector.FloorHeight;
						}

						plat.High = sector.FloorHeight;
						plat.Wait = 35 * SectorAction.platformWait;
						plat.Status = PlatformState.Down;
						this.world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);

						break;

					case PlatformType.BlazeDwus:
						plat.Speed = SectorAction.platformSpeed * 8;
						plat.Low = this.FindLowestFloorSurrounding(sector);

						if (plat.Low > sector.FloorHeight)
						{
							plat.Low = sector.FloorHeight;
						}

						plat.High = sector.FloorHeight;
						plat.Wait = 35 * SectorAction.platformWait;
						plat.Status = PlatformState.Down;
						this.world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);

						break;

					case PlatformType.PerpetualRaise:
						plat.Speed = SectorAction.platformSpeed;
						plat.Low = this.FindLowestFloorSurrounding(sector);

						if (plat.Low > sector.FloorHeight)
						{
							plat.Low = sector.FloorHeight;
						}

						plat.High = this.FindHighestFloorSurrounding(sector);

						if (plat.High < sector.FloorHeight)
						{
							plat.High = sector.FloorHeight;
						}

						plat.Wait = 35 * SectorAction.platformWait;
						plat.Status = (PlatformState) (this.world.Random.Next() & 1);
						this.world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);

						break;
				}

				this.AddActivePlatform(plat);
			}

			return result;
		}

		private static readonly int maxPlatformCount = 60;
		private Platform[] activePlatforms = new Platform[SectorAction.maxPlatformCount];

		public void ActivateInStasis(int tag)
		{
			for (var i = 0; i < this.activePlatforms.Length; i++)
			{
				if (this.activePlatforms[i] != null && this.activePlatforms[i].Tag == tag && this.activePlatforms[i].Status == PlatformState.InStasis)
				{
					this.activePlatforms[i].Status = this.activePlatforms[i].OldStatus;
					this.activePlatforms[i].ThinkerState = ThinkerState.Active;
				}
			}
		}

		public void StopPlatform(LineDef line)
		{
			for (var j = 0; j < this.activePlatforms.Length; j++)
			{
				if (this.activePlatforms[j] != null && this.activePlatforms[j].Status != PlatformState.InStasis && this.activePlatforms[j].Tag == line.Tag)
				{
					this.activePlatforms[j].OldStatus = this.activePlatforms[j].Status;
					this.activePlatforms[j].Status = PlatformState.InStasis;
					this.activePlatforms[j].ThinkerState = ThinkerState.InStasis;
				}
			}
		}

		public void AddActivePlatform(Platform platform)
		{
			for (var i = 0; i < this.activePlatforms.Length; i++)
			{
				if (this.activePlatforms[i] == null)
				{
					this.activePlatforms[i] = platform;

					return;
				}
			}

			throw new Exception("Too many active platforms!");
		}

		public void RemoveActivePlatform(Platform platform)
		{
			for (var i = 0; i < this.activePlatforms.Length; i++)
			{
				if (platform == this.activePlatforms[i])
				{
					this.activePlatforms[i].Sector.SpecialData = null;
					this.world.Thinkers.Remove(this.activePlatforms[i]);
					this.activePlatforms[i] = null;

					return;
				}
			}

			throw new Exception("The platform was not found!");
		}

		////////////////////////////////////////////////////////////
		// Floor
		////////////////////////////////////////////////////////////

		private static readonly Fixed floorSpeed = Fixed.One;

		public bool DoFloor(LineDef line, FloorMoveType type)
		{
			var sectors = this.world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = this.FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				// Already moving? If so, keep going...
				if (sector.SpecialData != null)
				{
					continue;
				}

				result = true;

				// New floor thinker.
				var floor = new FloorMove(this.world);
				this.world.Thinkers.Add(floor);
				sector.SpecialData = floor;
				floor.Type = type;
				floor.Crush = false;

				switch (type)
				{
					case FloorMoveType.LowerFloor:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = this.FindHighestFloorSurrounding(sector);

						break;

					case FloorMoveType.LowerFloorToLowest:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = this.FindLowestFloorSurrounding(sector);

						break;

					case FloorMoveType.TurboLower:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed * 4;
						floor.FloorDestHeight = this.FindHighestFloorSurrounding(sector);

						if (floor.FloorDestHeight != sector.FloorHeight)
						{
							floor.FloorDestHeight += Fixed.FromInt(8);
						}

						break;

					case FloorMoveType.RaiseFloorCrush:
					case FloorMoveType.RaiseFloor:
						if (type == FloorMoveType.RaiseFloorCrush)
						{
							floor.Crush = true;
						}

						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = this.FindLowestCeilingSurrounding(sector);

						if (floor.FloorDestHeight > sector.CeilingHeight)
						{
							floor.FloorDestHeight = sector.CeilingHeight;
						}

						floor.FloorDestHeight -= Fixed.FromInt(8) * (type == FloorMoveType.RaiseFloorCrush ? 1 : 0);

						break;

					case FloorMoveType.RaiseFloorTurbo:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed * 4;
						floor.FloorDestHeight = this.FindNextHighestFloor(sector, sector.FloorHeight);

						break;

					case FloorMoveType.RaiseFloorToNearest:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = this.FindNextHighestFloor(sector, sector.FloorHeight);

						break;

					case FloorMoveType.RaiseFloor24:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(24);

						break;

					case FloorMoveType.RaiseFloor512:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(512);

						break;

					case FloorMoveType.RaiseFloor24AndChange:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(24);
						sector.FloorFlat = line.FrontSector.FloorFlat;
						sector.Special = line.FrontSector.Special;

						break;

					case FloorMoveType.RaiseToTexture:
						var min = int.MaxValue;
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						var textures = this.world.Map.Textures;

						for (var i = 0; i < sector.Lines.Length; i++)
						{
							if ((sector.Lines[i].Flags & LineFlags.TwoSided) != 0)
							{
								var frontSide = sector.Lines[i].FrontSide;

								if (frontSide.BottomTexture >= 0)
								{
									if (textures[frontSide.BottomTexture].Height < min)
									{
										min = textures[frontSide.BottomTexture].Height;
									}
								}

								var backSide = sector.Lines[i].BackSide;

								if (backSide.BottomTexture >= 0)
								{
									if (textures[backSide.BottomTexture].Height < min)
									{
										min = textures[backSide.BottomTexture].Height;
									}
								}
							}
						}

						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(min);

						break;

					case FloorMoveType.LowerAndChange:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = SectorAction.floorSpeed;
						floor.FloorDestHeight = this.FindLowestFloorSurrounding(sector);
						floor.Texture = sector.FloorFlat;

						for (var i = 0; i < sector.Lines.Length; i++)
						{
							if ((sector.Lines[i].Flags & LineFlags.TwoSided) != 0)
							{
								if (sector.Lines[i].FrontSide.Sector.Number == sectorNumber)
								{
									sector = sector.Lines[i].BackSide.Sector;

									if (sector.FloorHeight == floor.FloorDestHeight)
									{
										floor.Texture = sector.FloorFlat;
										floor.NewSpecial = sector.Special;

										break;
									}
								}
								else
								{
									sector = sector.Lines[i].FrontSide.Sector;

									if (sector.FloorHeight == floor.FloorDestHeight)
									{
										floor.Texture = sector.FloorFlat;
										floor.NewSpecial = sector.Special;

										break;
									}
								}
							}
						}

						break;
				}
			}

			return result;
		}

		public bool BuildStairs(LineDef line, StairType type)
		{
			var sectors = this.world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = this.FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				// Already moving? If so, keep going...
				if (sector.SpecialData != null)
				{
					continue;
				}

				result = true;

				// New floor thinker.
				var floor = new FloorMove(this.world);
				this.world.Thinkers.Add(floor);
				sector.SpecialData = floor;
				floor.Direction = 1;
				floor.Sector = sector;

				Fixed speed;
				Fixed stairSize;

				switch (type)
				{
					case StairType.Build8:
						speed = SectorAction.floorSpeed / 4;
						stairSize = Fixed.FromInt(8);

						break;

					case StairType.Turbo16:
						speed = SectorAction.floorSpeed * 4;
						stairSize = Fixed.FromInt(16);

						break;

					default:
						throw new Exception("Unknown stair type!");
				}

				floor.Speed = speed;
				var height = sector.FloorHeight + stairSize;
				floor.FloorDestHeight = height;

				var texture = sector.FloorFlat;

				// Find next sector to raise.
				//     1. Find 2-sided line with same sector side[0].
				//     2. Other side is the next sector to raise.
				bool ok;

				do
				{
					ok = false;

					for (var i = 0; i < sector.Lines.Length; i++)
					{
						if (((sector.Lines[i]).Flags & LineFlags.TwoSided) == 0)
						{
							continue;
						}

						var target = (sector.Lines[i]).FrontSector;
						var newSectorNumber = target.Number;

						if (sectorNumber != newSectorNumber)
						{
							continue;
						}

						target = (sector.Lines[i]).BackSector;
						newSectorNumber = target.Number;

						if (target.FloorFlat != texture)
						{
							continue;
						}

						height += stairSize;

						if (target.SpecialData != null)
						{
							continue;
						}

						sector = target;
						sectorNumber = newSectorNumber;
						floor = new FloorMove(this.world);

						this.world.Thinkers.Add(floor);

						sector.SpecialData = floor;
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = speed;
						floor.FloorDestHeight = height;
						ok = true;

						break;
					}
				}
				while (ok);
			}

			return result;
		}

		////////////////////////////////////////////////////////////
		// Ceiling
		////////////////////////////////////////////////////////////

		public bool DoCeiling(LineDef line, CeilingMoveType type)
		{
			// Reactivate in-stasis ceilings...for certain types.
			switch (type)
			{
				case CeilingMoveType.FastCrushAndRaise:
				case CeilingMoveType.SilentCrushAndRaise:
				case CeilingMoveType.CrushAndRaise:
					this.ActivateInStasisCeiling(line);

					break;

				default:
					break;
			}

			var sectors = this.world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = this.FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				if (sector.SpecialData != null)
				{
					continue;
				}

				result = true;

				// New door thinker.
				var ceiling = new CeilingMove(this.world);
				this.world.Thinkers.Add(ceiling);
				sector.SpecialData = ceiling;
				ceiling.Sector = sector;
				ceiling.Crush = false;

				switch (type)
				{
					case CeilingMoveType.FastCrushAndRaise:
						ceiling.Crush = true;
						ceiling.TopHeight = sector.CeilingHeight;
						ceiling.BottomHeight = sector.FloorHeight + Fixed.FromInt(8);
						ceiling.Direction = -1;
						ceiling.Speed = SectorAction.CeilingSpeed * 2;

						break;

					case CeilingMoveType.SilentCrushAndRaise:
					case CeilingMoveType.CrushAndRaise:
					case CeilingMoveType.LowerAndCrush:
					case CeilingMoveType.LowerToFloor:
						if (type == CeilingMoveType.SilentCrushAndRaise || type == CeilingMoveType.CrushAndRaise)
						{
							ceiling.Crush = true;
							ceiling.TopHeight = sector.CeilingHeight;
						}

						ceiling.BottomHeight = sector.FloorHeight;

						if (type != CeilingMoveType.LowerToFloor)
						{
							ceiling.BottomHeight += Fixed.FromInt(8);
						}

						ceiling.Direction = -1;
						ceiling.Speed = SectorAction.CeilingSpeed;

						break;

					case CeilingMoveType.RaiseToHighest:
						ceiling.TopHeight = this.FindHighestCeilingSurrounding(sector);
						ceiling.Direction = 1;
						ceiling.Speed = SectorAction.CeilingSpeed;

						break;
				}

				ceiling.Tag = sector.Tag;
				ceiling.Type = type;
				this.AddActiveCeiling(ceiling);
			}

			return result;
		}

		public static readonly Fixed CeilingSpeed = Fixed.One;
		public static readonly int CeilingWwait = 150;

		private static readonly int maxCeilingCount = 30;

		private CeilingMove[] activeCeilings = new CeilingMove[SectorAction.maxCeilingCount];

		public void AddActiveCeiling(CeilingMove ceiling)
		{
			for (var i = 0; i < this.activeCeilings.Length; i++)
			{
				if (this.activeCeilings[i] == null)
				{
					this.activeCeilings[i] = ceiling;

					return;
				}
			}
		}

		public void RemoveActiveCeiling(CeilingMove ceiling)
		{
			for (var i = 0; i < this.activeCeilings.Length; i++)
			{
				if (this.activeCeilings[i] == ceiling)
				{
					this.activeCeilings[i].Sector.SpecialData = null;
					this.world.Thinkers.Remove(this.activeCeilings[i]);
					this.activeCeilings[i] = null;

					break;
				}
			}
		}

		public bool CheckActiveCeiling(CeilingMove ceiling)
		{
			if (ceiling == null)
			{
				return false;
			}

			for (var i = 0; i < this.activeCeilings.Length; i++)
			{
				if (this.activeCeilings[i] == ceiling)
				{
					return true;
				}
			}

			return false;
		}

		public void ActivateInStasisCeiling(LineDef line)
		{
			for (var i = 0; i < this.activeCeilings.Length; i++)
			{
				if (this.activeCeilings[i] != null && this.activeCeilings[i].Tag == line.Tag && this.activeCeilings[i].Direction == 0)
				{
					this.activeCeilings[i].Direction = this.activeCeilings[i].OldDirection;
					this.activeCeilings[i].ThinkerState = ThinkerState.Active;
				}
			}
		}

		public bool CeilingCrushStop(LineDef line)
		{
			var result = false;

			for (var i = 0; i < this.activeCeilings.Length; i++)
			{
				if (this.activeCeilings[i] != null && this.activeCeilings[i].Tag == line.Tag && this.activeCeilings[i].Direction != 0)
				{
					this.activeCeilings[i].OldDirection = this.activeCeilings[i].Direction;
					this.activeCeilings[i].ThinkerState = ThinkerState.InStasis;
					this.activeCeilings[i].Direction = 0;
					result = true;
				}
			}

			return result;
		}

		////////////////////////////////////////////////////////////
		// Teleport
		////////////////////////////////////////////////////////////

		public bool Teleport(LineDef line, int side, Mobj thing)
		{
			// Don't teleport missiles.
			if ((thing.Flags & MobjFlags.Missile) != 0)
			{
				return false;
			}

			// Don't teleport if hit back of line, so you can get out of teleporter.
			if (side == 1)
			{
				return false;
			}

			var sectors = this.world.Map.Sectors;
			var tag = line.Tag;

			for (var i = 0; i < sectors.Length; i++)
			{
				if (sectors[i].Tag == tag)
				{
					foreach (var thinker in this.world.Thinkers)
					{
						var dest = thinker as Mobj;

						if (dest == null)
						{
							// Not a mobj.
							continue;
						}

						if (dest.Type != MobjType.Teleportman)
						{
							// Not a teleportman.
							continue;
						}

						var sector = dest.Subsector.Sector;

						if (sector.Number != i)
						{
							// Wrong sector.
							continue;
						}

						var oldX = thing.X;
						var oldY = thing.Y;
						var oldZ = thing.Z;

						if (!this.world.ThingMovement.TeleportMove(thing, dest.X, dest.Y))
						{
							return false;
						}

						// This compatibility fix is based on Chocolate Doom's implementation.
						if (DoomApplication.Instance.IWad != "plutonia" && DoomApplication.Instance.IWad != "tnt")
						{
							thing.Z = thing.FloorZ;
						}

						if (thing.Player != null)
						{
							thing.Player.ViewZ = thing.Z + thing.Player.ViewHeight;
						}

						var ta = this.world.ThingAllocation;

						// Spawn teleport fog at source position.
						var fog1 = ta.SpawnMobj(oldX, oldY, oldZ, MobjType.Tfog);
						this.world.StartSound(fog1, Sfx.TELEPT, SfxType.Misc);

						// Destination position.
						var angle = dest.Angle;
						var fog2 = ta.SpawnMobj(dest.X + 20 * Trig.Cos(angle), dest.Y + 20 * Trig.Sin(angle), thing.Z, MobjType.Tfog);
						this.world.StartSound(fog2, Sfx.TELEPT, SfxType.Misc);

						if (thing.Player != null)
						{
							// Don't move for a bit.
							thing.ReactionTime = 18;
						}

						thing.Angle = dest.Angle;
						thing.MomX = thing.MomY = thing.MomZ = Fixed.Zero;

						return true;
					}
				}
			}

			return false;
		}

		////////////////////////////////////////////////////////////
		// Lighting
		////////////////////////////////////////////////////////////

		public void TurnTagLightsOff(LineDef line)
		{
			var sectors = this.world.Map.Sectors;

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];

				if (sector.Tag == line.Tag)
				{
					var min = sector.LightLevel;

					for (var j = 0; j < sector.Lines.Length; j++)
					{
						var target = this.GetNextSector(sector.Lines[j], sector);

						if (target == null)
						{
							continue;
						}

						if (target.LightLevel < min)
						{
							min = target.LightLevel;
						}
					}

					sector.LightLevel = min;
				}
			}
		}

		public void LightTurnOn(LineDef line, int bright)
		{
			var sectors = this.world.Map.Sectors;

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];

				if (sector.Tag == line.Tag)
				{
					// bright = 0 means to search for highest light level surrounding sector.
					if (bright == 0)
					{
						for (var j = 0; j < sector.Lines.Length; j++)
						{
							var target = this.GetNextSector(sector.Lines[j], sector);

							if (target == null)
							{
								continue;
							}

							if (target.LightLevel > bright)
							{
								bright = target.LightLevel;
							}
						}
					}

					sector.LightLevel = bright;
				}
			}
		}

		public void StartLightStrobing(LineDef line)
		{
			var sectors = this.world.Map.Sectors;
			var sectorNumber = -1;

			while ((sectorNumber = this.FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				if (sector.SpecialData != null)
				{
					continue;
				}

				this.world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, false);
			}
		}

		////////////////////////////////////////////////////////////
		// Miscellaneous
		////////////////////////////////////////////////////////////

		public bool DoDonut(LineDef line)
		{
			var sectors = this.world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = this.FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var s1 = sectors[sectorNumber];

				// Already moving? If so, keep going...
				if (s1.SpecialData != null)
				{
					continue;
				}

				result = true;

				var s2 = this.GetNextSector(s1.Lines[0], s1);

				//
				// The code below is based on Chocolate Doom's implementation.
				//

				if (s2 == null)
				{
					break;
				}

				for (var i = 0; i < s2.Lines.Length; i++)
				{
					var s3 = s2.Lines[i].BackSector;

					if (s3 == s1)
					{
						continue;
					}

					if (s3 == null)
					{
						// Undefined behavior in Vanilla Doom.
						return result;
					}

					var thinkers = this.world.Thinkers;

					// Spawn rising slime.
					var floor1 = new FloorMove(this.world);
					thinkers.Add(floor1);
					s2.SpecialData = floor1;
					floor1.Type = FloorMoveType.DonutRaise;
					floor1.Crush = false;
					floor1.Direction = 1;
					floor1.Sector = s2;
					floor1.Speed = SectorAction.floorSpeed / 2;
					floor1.Texture = s3.FloorFlat;
					floor1.NewSpecial = 0;
					floor1.FloorDestHeight = s3.FloorHeight;

					// Spawn lowering donut-hole.
					var floor2 = new FloorMove(this.world);
					thinkers.Add(floor2);
					s1.SpecialData = floor2;
					floor2.Type = FloorMoveType.LowerFloor;
					floor2.Crush = false;
					floor2.Direction = -1;
					floor2.Sector = s1;
					floor2.Speed = SectorAction.floorSpeed / 2;
					floor2.FloorDestHeight = s3.FloorHeight;

					break;
				}
			}

			return result;
		}

		public void SpawnDoorCloseIn30(Sector sector)
		{
			var door = new VerticalDoor(this.world);

			this.world.Thinkers.Add(door);

			sector.SpecialData = door;
			sector.Special = 0;

			door.Sector = sector;
			door.Direction = 0;
			door.Type = VerticalDoorType.Normal;
			door.Speed = SectorAction.doorSpeed;
			door.TopCountDown = 30 * 35;
		}

		public void SpawnDoorRaiseIn5Mins(Sector sector)
		{
			var door = new VerticalDoor(this.world);

			this.world.Thinkers.Add(door);

			sector.SpecialData = door;
			sector.Special = 0;

			door.Sector = sector;
			door.Direction = 2;
			door.Type = VerticalDoorType.RaiseIn5Mins;
			door.Speed = SectorAction.doorSpeed;
			door.TopHeight = this.FindLowestCeilingSurrounding(sector);
			door.TopHeight -= Fixed.FromInt(4);
			door.TopWait = SectorAction.doorWait;
			door.TopCountDown = 5 * 60 * 35;
		}
	}
}
