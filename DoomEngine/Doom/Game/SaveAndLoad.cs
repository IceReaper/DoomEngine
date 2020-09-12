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

namespace DoomEngine.Doom.Game
{
	using Common;
	using DoomEngine.Game;
	using DoomEngine.Game.Components.Items;
	using Graphics;
	using Info;
	using Map;
	using Math;
	using System;
	using System.IO;
	using System.Linq;
	using World;

	/// <summary>
	/// Vanilla-compatible save and load, full of messy binary handling code.
	/// </summary>
	public static class SaveAndLoad
	{
		public static readonly int DescriptionSize = 24;

		private static readonly int versionSize = 16;

		private enum ThinkerClass
		{
			End,
			Mobj
		}

		private enum SpecialClass
		{
			Ceiling,
			Door,
			Floor,
			Plat,
			Flash,
			Strobe,
			Glow,
			EndSpecials
		}

		public static void Save(DoomGame game, string description, string path)
		{
			var sg = new SaveGame(description);
			sg.Save(game, path);
		}

		public static void Load(DoomGame game, string path)
		{
			var options = game.Options;
			game.InitNew(options.Skill, options.Episode, options.Map);

			var lg = new LoadGame(DoomApplication.Instance.FileSystem.Read(path));
			lg.Load(game);
		}

		////////////////////////////////////////////////////////////
		// Save game
		////////////////////////////////////////////////////////////

		private class SaveGame
		{
			private BinaryWriter writer;

			public SaveGame(string description)
			{
				this.writer = new BinaryWriter(new MemoryStream());
				this.WriteDescription(description);
				this.WriteVersion();
			}

			private void WriteDescription(string description)
			{
				for (var i = 0; i < description.Length; i++)
				{
					this.writer.Write((byte) description[i]);
				}

				this.writer.BaseStream.Position += SaveAndLoad.DescriptionSize - description.Length;
			}

			private void WriteVersion()
			{
				var version = "version 109";

				for (var i = 0; i < version.Length; i++)
				{
					this.writer.Write((byte) version[i]);
				}

				this.writer.BaseStream.Position += SaveAndLoad.versionSize - version.Length;
			}

			public void Save(DoomGame game, string path)
			{
				var options = game.World.Options;
				this.writer.Write((byte) options.Skill);
				this.writer.Write((byte) options.Episode);
				this.writer.Write((byte) options.Map);

				this.writer.Write(game.World.LevelTime);

				game.World.WorldEntity.Serialize(this.writer);
				this.writer.Write(game.World.Entities.Count);
				game.World.Entities.ForEach(entity => entity.Serialize(this.writer));

				this.ArchivePlayers(game.World);
				this.ArchiveWorld(game.World);
				this.ArchiveThinkers(game.World);
				this.ArchiveSpecials(game.World);

				this.writer.Write((byte) 0x1d);

				using (var writer = DoomApplication.Instance.FileSystem.Write(path))
				{
					writer.Write(((MemoryStream) this.writer.BaseStream).GetBuffer(), 0, (int) this.writer.BaseStream.Length);
				}
			}

			private void PadPointer()
			{
				this.writer.BaseStream.Position += (4 - (this.writer.BaseStream.Position & 3)) & 3;
			}

			private void ArchivePlayers(World world)
			{
				var player = world.Options.Player;
				this.PadPointer();
				this.ArchivePlayer(player);
			}

			private void ArchiveWorld(World world)
			{
				// Do sectors.
				var sectors = world.Map.Sectors;

				for (var i = 0; i < sectors.Length; i++)
				{
					this.ArchiveSector(sectors[i]);
				}

				// Do lines.
				var lines = world.Map.Lines;

				for (var i = 0; i < lines.Length; i++)
				{
					this.ArchiveLine(lines[i]);
				}
			}

			private void ArchiveThinkers(World world)
			{
				var thinkers = world.Thinkers;

				// Read in saved thinkers.
				foreach (var thinker in thinkers)
				{
					var mobj = thinker as Mobj;

					if (mobj != null)
					{
						this.writer.Write((byte) ThinkerClass.Mobj);
						this.PadPointer();

						this.writer.BaseStream.Position += 8;
						this.writer.Write(SaveGame.GetThinkerState(mobj.ThinkerState));
						this.writer.Write(mobj.X.Data);
						this.writer.Write(mobj.Y.Data);
						this.writer.Write(mobj.Z.Data);
						this.writer.BaseStream.Position += 8;
						this.writer.Write(mobj.Angle.Data);
						this.writer.Write((int) mobj.Sprite);
						this.writer.Write(mobj.Frame);
						this.writer.BaseStream.Position += 12;
						this.writer.Write(mobj.FloorZ.Data);
						this.writer.Write(mobj.CeilingZ.Data);
						this.writer.Write(mobj.Radius.Data);
						this.writer.Write(mobj.Height.Data);
						this.writer.Write(mobj.MomX.Data);
						this.writer.Write(mobj.MomY.Data);
						this.writer.Write(mobj.MomZ.Data);
						this.writer.BaseStream.Position += 4;
						this.writer.Write((int) mobj.Type);
						this.writer.BaseStream.Position += 4;
						this.writer.Write(mobj.Tics);
						this.writer.Write(mobj.State.Number);
						this.writer.Write((int) mobj.Flags);
						this.writer.Write(mobj.Health);
						this.writer.Write((int) mobj.MoveDir);
						this.writer.Write(mobj.MoveCount);
						this.writer.BaseStream.Position += 4;
						this.writer.Write(mobj.ReactionTime);
						this.writer.Write(mobj.Threshold);

						if (mobj.Player == null)
						{
							this.writer.Write(0);
						}
						else
						{
							this.writer.Write(1);
						}

						this.writer.Write(mobj.LastLook);

						if (mobj.SpawnPoint == null)
						{
							this.writer.Write((short) 0);
							this.writer.Write((short) 0);
							this.writer.Write((short) 0);
							this.writer.Write((short) 0);
							this.writer.Write((short) 0);
						}
						else
						{
							this.writer.Write((short) mobj.SpawnPoint.X.ToIntFloor());
							this.writer.Write((short) mobj.SpawnPoint.Y.ToIntFloor());
							this.writer.Write((short) Math.Round(mobj.SpawnPoint.Angle.ToDegree()));
							this.writer.Write((short) mobj.SpawnPoint.Type);
							this.writer.Write((short) mobj.SpawnPoint.Flags);
						}

						this.writer.BaseStream.Position += 4;
					}
				}

				this.writer.Write((byte) ThinkerClass.End);
			}

			private void ArchiveSpecials(World world)
			{
				var thinkers = world.Thinkers;
				var sa = world.SectorAction;

				// Read in saved thinkers.
				foreach (var thinker in thinkers)
				{
					if (thinker.ThinkerState == ThinkerState.InStasis)
					{
						var ceiling = thinker as CeilingMove;

						if (sa.CheckActiveCeiling(ceiling))
						{
							this.writer.Write((byte) SpecialClass.Ceiling);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(ceiling.ThinkerState));
							this.writer.Write((int) ceiling.Type);
							this.writer.Write(ceiling.Sector.Number);
							this.writer.Write(ceiling.BottomHeight.Data);
							this.writer.Write(ceiling.TopHeight.Data);
							this.writer.Write(ceiling.Speed.Data);
							this.writer.Write(ceiling.Crush ? 1 : 0);
							this.writer.Write(ceiling.Direction);
							this.writer.Write(ceiling.Tag);
							this.writer.Write(ceiling.OldDirection);
						}

						continue;
					}

					{
						var ceiling = thinker as CeilingMove;

						if (ceiling != null)
						{
							this.writer.Write((byte) SpecialClass.Ceiling);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(ceiling.ThinkerState));
							this.writer.Write((int) ceiling.Type);
							this.writer.Write(ceiling.Sector.Number);
							this.writer.Write(ceiling.BottomHeight.Data);
							this.writer.Write(ceiling.TopHeight.Data);
							this.writer.Write(ceiling.Speed.Data);
							this.writer.Write(ceiling.Crush ? 1 : 0);
							this.writer.Write(ceiling.Direction);
							this.writer.Write(ceiling.Tag);
							this.writer.Write(ceiling.OldDirection);

							continue;
						}
					}

					{
						var door = thinker as VerticalDoor;

						if (door != null)
						{
							this.writer.Write((byte) SpecialClass.Door);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(door.ThinkerState));
							this.writer.Write((int) door.Type);
							this.writer.Write(door.Sector.Number);
							this.writer.Write(door.TopHeight.Data);
							this.writer.Write(door.Speed.Data);
							this.writer.Write(door.Direction);
							this.writer.Write(door.TopWait);
							this.writer.Write(door.TopCountDown);

							continue;
						}
					}

					{
						var floor = thinker as FloorMove;

						if (floor != null)
						{
							this.writer.Write((byte) SpecialClass.Floor);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(floor.ThinkerState));
							this.writer.Write((int) floor.Type);
							this.writer.Write(floor.Crush ? 1 : 0);
							this.writer.Write(floor.Sector.Number);
							this.writer.Write(floor.Direction);
							this.writer.Write((int) floor.NewSpecial);
							this.writer.Write(floor.Texture);
							this.writer.Write(floor.FloorDestHeight.Data);
							this.writer.Write(floor.Speed.Data);

							continue;
						}
					}

					{
						var plat = thinker as Platform;

						if (plat != null)
						{
							this.writer.Write((byte) SpecialClass.Plat);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(plat.ThinkerState));
							this.writer.Write(plat.Sector.Number);
							this.writer.Write(plat.Speed.Data);
							this.writer.Write(plat.Low.Data);
							this.writer.Write(plat.High.Data);
							this.writer.Write(plat.Wait);
							this.writer.Write(plat.Count);
							this.writer.Write((int) plat.Status);
							this.writer.Write((int) plat.OldStatus);
							this.writer.Write(plat.Crush ? 1 : 0);
							this.writer.Write(plat.Tag);
							this.writer.Write((int) plat.Type);

							continue;
						}
					}

					{
						var flash = thinker as LightFlash;

						if (flash != null)
						{
							this.writer.Write((byte) SpecialClass.Flash);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(flash.ThinkerState));
							this.writer.Write(flash.Sector.Number);
							this.writer.Write(flash.Count);
							this.writer.Write(flash.MaxLight);
							this.writer.Write(flash.MinLight);
							this.writer.Write(flash.MaxTime);
							this.writer.Write(flash.MinTime);

							continue;
						}
					}

					{
						var strobe = thinker as StrobeFlash;

						if (strobe != null)
						{
							this.writer.Write((byte) SpecialClass.Strobe);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(strobe.ThinkerState));
							this.writer.Write(strobe.Sector.Number);
							this.writer.Write(strobe.Count);
							this.writer.Write(strobe.MinLight);
							this.writer.Write(strobe.MaxLight);
							this.writer.Write(strobe.DarkTime);
							this.writer.Write(strobe.BrightTime);

							continue;
						}
					}

					{
						var glow = thinker as GlowingLight;

						if (glow != null)
						{
							this.writer.Write((byte) SpecialClass.Glow);
							this.PadPointer();
							this.writer.BaseStream.Position += 8;
							this.writer.Write(SaveGame.GetThinkerState(glow.ThinkerState));
							this.writer.Write(glow.Sector.Number);
							this.writer.Write(glow.MinLight);
							this.writer.Write(glow.MaxLight);
							this.writer.Write(glow.Direction);

							continue;
						}
					}
				}

				this.writer.Write((byte) SpecialClass.EndSpecials);
			}

			private void ArchivePlayer(Player player)
			{
				this.writer.BaseStream.Position += 4;
				this.writer.Write((int) player.PlayerState);
				this.writer.BaseStream.Position += 8;
				this.writer.Write(player.ViewZ.Data);
				this.writer.Write(player.ViewHeight.Data);
				this.writer.Write(player.DeltaViewHeight.Data);
				this.writer.Write(player.Bob.Data);
				this.writer.Write(player.ArmorPoints);
				this.writer.Write(player.ArmorType);

				for (var i = 0; i < (int) PowerType.Count; i++)
				{
					this.writer.Write(player.Powers[i]);
				}

				this.writer.Write(player.Backpack ? 1 : 0);

				this.writer.Write(player.ReadyWeapon.Info.Name);
				this.writer.Write(player.PendingWeapon?.Info.Name ?? "");

				this.writer.Write(player.AttackDown ? 1 : 0);
				this.writer.Write(player.UseDown ? 1 : 0);
				this.writer.Write((int) player.Cheats);
				this.writer.Write(player.Refire);
				this.writer.Write(player.KillCount);
				this.writer.Write(player.ItemCount);
				this.writer.Write(player.SecretCount);
				this.writer.BaseStream.Position += 4;
				this.writer.Write(player.DamageCount);
				this.writer.Write(player.BonusCount);
				this.writer.BaseStream.Position += 4;
				this.writer.Write(player.ExtraLight);
				this.writer.Write(player.FixedColorMap);
				this.writer.Write(player.ColorMap);

				for (var i = 0; i < (int) PlayerSprite.Count; i++)
				{
					if (player.PlayerSprites[i].State == null)
					{
						this.writer.Write(0);
					}
					else
					{
						this.writer.Write(player.PlayerSprites[i].State.Number);
					}

					this.writer.Write(player.PlayerSprites[i].Tics);
					this.writer.Write(player.PlayerSprites[i].Sx.Data);
					this.writer.Write(player.PlayerSprites[i].Sy.Data);
				}

				this.writer.Write(player.DidSecret ? 1 : 0);
			}

			private void ArchiveSector(Sector sector)
			{
				this.writer.Write((short) (sector.FloorHeight.ToIntFloor()));
				this.writer.Write((short) (sector.CeilingHeight.ToIntFloor()));
				this.writer.Write((short) sector.FloorFlat);
				this.writer.Write((short) sector.CeilingFlat);
				this.writer.Write((short) sector.LightLevel);
				this.writer.Write((short) sector.Special);
				this.writer.Write((short) sector.Tag);
			}

			private void ArchiveLine(LineDef line)
			{
				this.writer.Write((short) line.Flags);
				this.writer.Write((short) line.Special);
				this.writer.Write((short) line.Tag);

				if (line.FrontSide != null)
				{
					var side = line.FrontSide;
					this.writer.Write((short) side.TextureOffset.ToIntFloor());
					this.writer.Write((short) side.RowOffset.ToIntFloor());
					this.writer.Write((short) side.TopTexture);
					this.writer.Write((short) side.BottomTexture);
					this.writer.Write((short) side.MiddleTexture);
				}

				if (line.BackSide != null)
				{
					var side = line.BackSide;
					this.writer.Write((short) side.TextureOffset.ToIntFloor());
					this.writer.Write((short) side.RowOffset.ToIntFloor());
					this.writer.Write((short) side.TopTexture);
					this.writer.Write((short) side.BottomTexture);
					this.writer.Write((short) side.MiddleTexture);
				}
			}

			private static int GetThinkerState(ThinkerState state)
			{
				switch (state)
				{
					case ThinkerState.InStasis:
						return 0;

					default:
						return 1;
				}
			}
		}

		////////////////////////////////////////////////////////////
		// Load game
		////////////////////////////////////////////////////////////

		private class LoadGame
		{
			private BinaryReader reader;

			public LoadGame(Stream stream)
			{
				this.reader = new BinaryReader(stream);

				this.ReadDescription();

				var version = this.ReadVersion();

				if (version != "VERSION 109")
				{
					throw new Exception("Unsupported version!");
				}
			}

			public void Load(DoomGame game)
			{
				var options = game.World.Options;
				options.Skill = (GameSkill) this.reader.ReadByte();
				options.Episode = this.reader.ReadByte();
				options.Map = this.reader.ReadByte();

				game.InitNew(options.Skill, options.Episode, options.Map);

				var levelTime = this.reader.ReadInt32();

				game.World.WorldEntity = Entity.Deserialize(game.World, this.reader);

				var numEntities = this.reader.ReadInt32();

				for (var i = 0; i < numEntities; i++)
					game.World.Entities.Add(Entity.Deserialize(game.World, this.reader));

				this.UnArchivePlayers(game.World);
				this.UnArchiveWorld(game.World);
				this.UnArchiveThinkers(game.World);
				this.UnArchiveSpecials(game.World);

				var test = this.reader.ReadByte();
				this.reader.BaseStream.Position--;

				if (test != 0x1d)
				{
					throw new Exception("Bad savegame!");
				}

				game.World.LevelTime = levelTime;

				options.Sound.SetListener(game.World.Options.Player.Mobj);
			}

			private void PadPointer()
			{
				this.reader.BaseStream.Position += (4 - (this.reader.BaseStream.Position & 3)) & 3;
			}

			private string ReadDescription()
			{
				return DoomInterop.ToString(this.reader.ReadBytes(SaveAndLoad.DescriptionSize), 0, SaveAndLoad.DescriptionSize);
			}

			private string ReadVersion()
			{
				return DoomInterop.ToString(this.reader.ReadBytes(SaveAndLoad.versionSize), 0, SaveAndLoad.versionSize);
			}

			private void UnArchivePlayers(World world)
			{
				var player = world.Options.Player;

				this.PadPointer();
				this.UnArchivePlayer(world, player);
			}

			private void UnArchiveWorld(World world)
			{
				// Do sectors.
				var sectors = world.Map.Sectors;

				for (var i = 0; i < sectors.Length; i++)
				{
					this.UnArchiveSector(sectors[i]);
				}

				// Do lines.
				var lines = world.Map.Lines;

				for (var i = 0; i < lines.Length; i++)
				{
					this.UnArchiveLine(lines[i]);
				}
			}

			private void UnArchiveThinkers(World world)
			{
				var thinkers = world.Thinkers;
				var ta = world.ThingAllocation;

				// Remove all the current thinkers.
				foreach (var thinker in thinkers)
				{
					var mobj = thinker as Mobj;

					if (mobj != null)
					{
						ta.RemoveMobj(mobj);
					}
				}

				thinkers.Reset();

				// Read in saved thinkers.
				while (true)
				{
					var tclass = (ThinkerClass) this.reader.ReadByte();

					switch (tclass)
					{
						case ThinkerClass.End:
							// End of list.
							return;

						case ThinkerClass.Mobj:
							this.PadPointer();
							var mobj = new Mobj(world);
							this.reader.BaseStream.Position += 8;
							mobj.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							mobj.X = new Fixed(this.reader.ReadInt32());
							mobj.Y = new Fixed(this.reader.ReadInt32());
							mobj.Z = new Fixed(this.reader.ReadInt32());
							this.reader.BaseStream.Position += 8;
							mobj.Angle = new Angle(this.reader.ReadInt32());
							mobj.Sprite = (Sprite) this.reader.ReadInt32();
							mobj.Frame = this.reader.ReadInt32();
							this.reader.BaseStream.Position += 12;
							mobj.FloorZ = new Fixed(this.reader.ReadInt32());
							mobj.CeilingZ = new Fixed(this.reader.ReadInt32());
							mobj.Radius = new Fixed(this.reader.ReadInt32());
							mobj.Height = new Fixed(this.reader.ReadInt32());
							mobj.MomX = new Fixed(this.reader.ReadInt32());
							mobj.MomY = new Fixed(this.reader.ReadInt32());
							mobj.MomZ = new Fixed(this.reader.ReadInt32());
							this.reader.BaseStream.Position += 4;
							mobj.Type = (MobjType) this.reader.ReadInt32();
							mobj.Info = DoomInfo.MobjInfos[(int) mobj.Type];
							this.reader.BaseStream.Position += 4;
							mobj.Tics = this.reader.ReadInt32();
							mobj.State = DoomInfo.States[this.reader.ReadInt32()];
							mobj.Flags = (MobjFlags) this.reader.ReadInt32();
							mobj.Health = this.reader.ReadInt32();
							mobj.MoveDir = (Direction) this.reader.ReadInt32();
							mobj.MoveCount = this.reader.ReadInt32();
							this.reader.BaseStream.Position += 4;
							mobj.ReactionTime = this.reader.ReadInt32();
							mobj.Threshold = this.reader.ReadInt32();
							var playerNumber = this.reader.ReadInt32();

							if (playerNumber != 0)
							{
								mobj.Player = world.Options.Player;
								mobj.Player.Mobj = mobj;
							}

							mobj.LastLook = this.reader.ReadInt32();

							mobj.SpawnPoint = new MapThing(
								Fixed.FromInt(this.reader.ReadInt16()),
								Fixed.FromInt(this.reader.ReadInt16()),
								new Angle(Angle.Ang45.Data * (uint) (this.reader.ReadInt16() / 45)),
								this.reader.ReadInt16(),
								(ThingFlags) this.reader.ReadInt16()
							);

							this.reader.BaseStream.Position += 4;

							world.ThingMovement.SetThingPosition(mobj);

							// mobj.FloorZ = mobj.Subsector.Sector.FloorHeight;
							// mobj.CeilingZ = mobj.Subsector.Sector.CeilingHeight;
							thinkers.Add(mobj);

							break;

						default:
							throw new Exception("Unknown thinker class in savegame!");
					}
				}
			}

			private void UnArchiveSpecials(World world)
			{
				var thinkers = world.Thinkers;
				var sa = world.SectorAction;

				// Read in saved thinkers.
				while (true)
				{
					var tclass = (SpecialClass) this.reader.ReadByte();

					switch (tclass)
					{
						case SpecialClass.EndSpecials:
							// End of list.
							return;

						case SpecialClass.Ceiling:
							this.PadPointer();
							var ceiling = new CeilingMove(world);
							this.reader.BaseStream.Position += 8;
							ceiling.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							ceiling.Type = (CeilingMoveType) this.reader.ReadInt32();
							ceiling.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							ceiling.Sector.SpecialData = ceiling;
							ceiling.BottomHeight = new Fixed(this.reader.ReadInt32());
							ceiling.TopHeight = new Fixed(this.reader.ReadInt32());
							ceiling.Speed = new Fixed(this.reader.ReadInt32());
							ceiling.Crush = this.reader.ReadInt32() != 0;
							ceiling.Direction = this.reader.ReadInt32();
							ceiling.Tag = this.reader.ReadInt32();
							ceiling.OldDirection = this.reader.ReadInt32();

							thinkers.Add(ceiling);
							sa.AddActiveCeiling(ceiling);

							break;

						case SpecialClass.Door:
							this.PadPointer();
							var door = new VerticalDoor(world);
							this.reader.BaseStream.Position += 8;
							door.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							door.Type = (VerticalDoorType) this.reader.ReadInt32();
							door.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							door.Sector.SpecialData = door;
							door.TopHeight = new Fixed(this.reader.ReadInt32());
							door.Speed = new Fixed(this.reader.ReadInt32());
							door.Direction = this.reader.ReadInt32();
							door.TopWait = this.reader.ReadInt32();
							door.TopCountDown = this.reader.ReadInt32();

							thinkers.Add(door);

							break;

						case SpecialClass.Floor:
							this.PadPointer();
							var floor = new FloorMove(world);
							this.reader.BaseStream.Position += 8;
							floor.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							floor.Type = (FloorMoveType) this.reader.ReadInt32();
							floor.Crush = this.reader.ReadInt32() != 0;
							floor.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							floor.Sector.SpecialData = floor;
							floor.Direction = this.reader.ReadInt32();
							floor.NewSpecial = (SectorSpecial) this.reader.ReadInt32();
							floor.Texture = this.reader.ReadInt32();
							floor.FloorDestHeight = new Fixed(this.reader.ReadInt32());
							floor.Speed = new Fixed(this.reader.ReadInt32());

							thinkers.Add(floor);

							break;

						case SpecialClass.Plat:
							this.PadPointer();
							var plat = new Platform(world);
							this.reader.BaseStream.Position += 8;
							plat.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							plat.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							plat.Sector.SpecialData = plat;
							plat.Speed = new Fixed(this.reader.ReadInt32());
							plat.Low = new Fixed(this.reader.ReadInt32());
							plat.High = new Fixed(this.reader.ReadInt32());
							plat.Wait = this.reader.ReadInt32();
							plat.Count = this.reader.ReadInt32();
							plat.Status = (PlatformState) this.reader.ReadInt32();
							plat.OldStatus = (PlatformState) this.reader.ReadInt32();
							plat.Crush = this.reader.ReadInt32() != 0;
							plat.Tag = this.reader.ReadInt32();
							plat.Type = (PlatformType) this.reader.ReadInt32();

							thinkers.Add(plat);
							sa.AddActivePlatform(plat);

							break;

						case SpecialClass.Flash:
							this.PadPointer();
							var flash = new LightFlash(world);
							this.reader.BaseStream.Position += 8;
							flash.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							flash.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							flash.Count = this.reader.ReadInt32();
							flash.MaxLight = this.reader.ReadInt32();
							flash.MinLight = this.reader.ReadInt32();
							flash.MaxTime = this.reader.ReadInt32();
							flash.MinTime = this.reader.ReadInt32();

							thinkers.Add(flash);

							break;

						case SpecialClass.Strobe:
							this.PadPointer();
							var strobe = new StrobeFlash(world);
							this.reader.BaseStream.Position += 8;
							strobe.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							strobe.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							strobe.Count = this.reader.ReadInt32();
							strobe.MinLight = this.reader.ReadInt32();
							strobe.MaxLight = this.reader.ReadInt32();
							strobe.DarkTime = this.reader.ReadInt32();
							strobe.BrightTime = this.reader.ReadInt32();

							thinkers.Add(strobe);

							break;

						case SpecialClass.Glow:
							this.PadPointer();
							var glow = new GlowingLight(world);
							this.reader.BaseStream.Position += 8;
							glow.ThinkerState = LoadGame.GetThinkerState(this.reader.ReadInt32());
							glow.Sector = world.Map.Sectors[this.reader.ReadInt32()];
							glow.MinLight = this.reader.ReadInt32();
							glow.MaxLight = this.reader.ReadInt32();
							glow.Direction = this.reader.ReadInt32();

							thinkers.Add(glow);

							break;

						default:
							throw new Exception("Unknown special in savegame!");
					}
				}
			}

			private static ThinkerState GetThinkerState(int value)
			{
				switch (value)
				{
					case 0:
						return ThinkerState.InStasis;

					default:
						return ThinkerState.Active;
				}
			}

			private void UnArchivePlayer(World world, Player player)
			{
				player.Clear(world);
				player.Reborn(world);

				this.reader.BaseStream.Position += 4;
				player.PlayerState = (PlayerState) this.reader.ReadInt32();
				this.reader.BaseStream.Position += 8;
				player.ViewZ = new Fixed(this.reader.ReadInt32());
				player.ViewHeight = new Fixed(this.reader.ReadInt32());
				player.DeltaViewHeight = new Fixed(this.reader.ReadInt32());
				player.Bob = new Fixed(this.reader.ReadInt32());
				player.ArmorPoints = this.reader.ReadInt32();
				player.ArmorType = this.reader.ReadInt32();

				for (var i = 0; i < (int) PowerType.Count; i++)
				{
					player.Powers[i] = this.reader.ReadInt32();
				}

				player.Backpack = this.reader.ReadInt32() != 0;

				player.Entity = world.Entities.First(entity => entity.Info is DoomEngine.Game.Entities.Player);

				var inventory = player.Entity.GetComponent<InventoryComponent>();

				var readyWeapon = this.reader.ReadString();
				player.ReadyWeapon = inventory.Items.First(weapon => weapon.Info.Name == readyWeapon);

				var pendingWeapon = this.reader.ReadString();

				if (pendingWeapon != "")
					player.PendingWeapon = inventory.Items.First(weapon => weapon.Info.Name == pendingWeapon);

				player.AttackDown = this.reader.ReadInt32() != 0;
				player.UseDown = this.reader.ReadInt32() != 0;
				player.Cheats = (CheatFlags) this.reader.ReadInt32();
				player.Refire = this.reader.ReadInt32();
				player.KillCount = this.reader.ReadInt32();
				player.ItemCount = this.reader.ReadInt32();
				player.SecretCount = this.reader.ReadInt32();
				this.reader.BaseStream.Position += 4;
				player.DamageCount = this.reader.ReadInt32();
				player.BonusCount = this.reader.ReadInt32();
				this.reader.BaseStream.Position += 4;
				player.ExtraLight = this.reader.ReadInt32();
				player.FixedColorMap = this.reader.ReadInt32();
				player.ColorMap = this.reader.ReadInt32();

				for (var i = 0; i < (int) PlayerSprite.Count; i++)
				{
					player.PlayerSprites[i].State = DoomInfo.States[this.reader.ReadInt32()];

					if (player.PlayerSprites[i].State.Number == (int) MobjState.Null)
					{
						player.PlayerSprites[i].State = null;
					}

					player.PlayerSprites[i].Tics = this.reader.ReadInt32();
					player.PlayerSprites[i].Sx = new Fixed(this.reader.ReadInt32());
					player.PlayerSprites[i].Sy = new Fixed(this.reader.ReadInt32());
				}

				player.DidSecret = this.reader.ReadInt32() != 0;
			}

			private void UnArchiveSector(Sector sector)
			{
				sector.FloorHeight = Fixed.FromInt(this.reader.ReadInt16());
				sector.CeilingHeight = Fixed.FromInt(this.reader.ReadInt16());
				sector.FloorFlat = this.reader.ReadInt16();
				sector.CeilingFlat = this.reader.ReadInt16();
				sector.LightLevel = this.reader.ReadInt16();
				sector.Special = (SectorSpecial) this.reader.ReadInt16();
				sector.Tag = this.reader.ReadInt16();
				sector.SpecialData = null;
				sector.SoundTarget = null;
			}

			private void UnArchiveLine(LineDef line)
			{
				line.Flags = (LineFlags) this.reader.ReadInt16();
				line.Special = (LineSpecial) this.reader.ReadInt16();
				line.Tag = this.reader.ReadInt16();

				if (line.FrontSide != null)
				{
					var side = line.FrontSide;
					side.TextureOffset = Fixed.FromInt(this.reader.ReadInt16());
					side.RowOffset = Fixed.FromInt(this.reader.ReadInt16());
					side.TopTexture = this.reader.ReadInt16();
					side.BottomTexture = this.reader.ReadInt16();
					side.MiddleTexture = this.reader.ReadInt16();
				}

				if (line.BackSide != null)
				{
					var side = line.BackSide;
					side.TextureOffset = Fixed.FromInt(this.reader.ReadInt16());
					side.RowOffset = Fixed.FromInt(this.reader.ReadInt16());
					side.TopTexture = this.reader.ReadInt16();
					side.BottomTexture = this.reader.ReadInt16();
					side.MiddleTexture = this.reader.ReadInt16();
				}
			}
		}
	}
}
