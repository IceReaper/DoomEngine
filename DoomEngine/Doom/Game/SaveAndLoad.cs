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
	using Graphics;
	using Info;
	using Map;
	using Math;
	using System;
	using System.IO;
	using World;

	/// <summary>
	/// Vanilla-compatible save and load, full of messy binary handling code.
	/// </summary>
	public static class SaveAndLoad
	{
		public static readonly int DescriptionSize = 24;

		private static readonly int versionSize = 16;
		private static readonly int saveBufferSize = 360 * 1024;

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
			private byte[] data;
			private int ptr;

			public SaveGame(string description)
			{
				this.data = new byte[SaveAndLoad.saveBufferSize];
				this.ptr = 0;

				this.WriteDescription(description);
				this.WriteVersion();
			}

			private void WriteDescription(string description)
			{
				for (var i = 0; i < description.Length; i++)
				{
					this.data[i] = (byte) description[i];
				}

				this.ptr += SaveAndLoad.DescriptionSize;
			}

			private void WriteVersion()
			{
				var version = "version 109";

				for (var i = 0; i < version.Length; i++)
				{
					this.data[this.ptr + i] = (byte) version[i];
				}

				this.ptr += SaveAndLoad.versionSize;
			}

			public void Save(DoomGame game, string path)
			{
				var options = game.World.Options;
				this.data[this.ptr++] = (byte) options.Skill;
				this.data[this.ptr++] = (byte) options.Episode;
				this.data[this.ptr++] = (byte) options.Map;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					this.data[this.ptr++] = options.Players[i].InGame ? (byte) 1 : (byte) 0;
				}

				this.data[this.ptr++] = (byte) (game.World.LevelTime >> 16);
				this.data[this.ptr++] = (byte) (game.World.LevelTime >> 8);
				this.data[this.ptr++] = (byte) (game.World.LevelTime);

				this.ArchivePlayers(game.World);
				this.ArchiveWorld(game.World);
				this.ArchiveThinkers(game.World);
				this.ArchiveSpecials(game.World);

				this.data[this.ptr++] = 0x1d;

				using (var writer = DoomApplication.Instance.FileSystem.Write(path))
				{
					writer.Write(this.data, 0, this.ptr);
				}
			}

			private void PadPointer()
			{
				this.ptr += (4 - (this.ptr & 3)) & 3;
			}

			private void ArchivePlayers(World world)
			{
				var players = world.Options.Players;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!players[i].InGame)
					{
						continue;
					}

					this.PadPointer();

					this.ptr = SaveGame.ArchivePlayer(players[i], this.data, this.ptr);
				}
			}

			private void ArchiveWorld(World world)
			{
				// Do sectors.
				var sectors = world.Map.Sectors;

				for (var i = 0; i < sectors.Length; i++)
				{
					this.ptr = SaveGame.ArchiveSector(sectors[i], this.data, this.ptr);
				}

				// Do lines.
				var lines = world.Map.Lines;

				for (var i = 0; i < lines.Length; i++)
				{
					this.ptr = SaveGame.ArchiveLine(lines[i], this.data, this.ptr);
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
						this.data[this.ptr++] = (byte) ThinkerClass.Mobj;
						this.PadPointer();

						SaveGame.WriteThinkerState(this.data, this.ptr + 8, mobj.ThinkerState);
						SaveGame.Write(this.data, this.ptr + 12, mobj.X.Data);
						SaveGame.Write(this.data, this.ptr + 16, mobj.Y.Data);
						SaveGame.Write(this.data, this.ptr + 20, mobj.Z.Data);
						SaveGame.Write(this.data, this.ptr + 32, mobj.Angle.Data);
						SaveGame.Write(this.data, this.ptr + 36, (int) mobj.Sprite);
						SaveGame.Write(this.data, this.ptr + 40, mobj.Frame);
						SaveGame.Write(this.data, this.ptr + 56, mobj.FloorZ.Data);
						SaveGame.Write(this.data, this.ptr + 60, mobj.CeilingZ.Data);
						SaveGame.Write(this.data, this.ptr + 64, mobj.Radius.Data);
						SaveGame.Write(this.data, this.ptr + 68, mobj.Height.Data);
						SaveGame.Write(this.data, this.ptr + 72, mobj.MomX.Data);
						SaveGame.Write(this.data, this.ptr + 76, mobj.MomY.Data);
						SaveGame.Write(this.data, this.ptr + 80, mobj.MomZ.Data);
						SaveGame.Write(this.data, this.ptr + 88, (int) mobj.Type);
						SaveGame.Write(this.data, this.ptr + 96, mobj.Tics);
						SaveGame.Write(this.data, this.ptr + 100, mobj.State.Number);
						SaveGame.Write(this.data, this.ptr + 104, (int) mobj.Flags);
						SaveGame.Write(this.data, this.ptr + 108, mobj.Health);
						SaveGame.Write(this.data, this.ptr + 112, (int) mobj.MoveDir);
						SaveGame.Write(this.data, this.ptr + 116, mobj.MoveCount);
						SaveGame.Write(this.data, this.ptr + 124, mobj.ReactionTime);
						SaveGame.Write(this.data, this.ptr + 128, mobj.Threshold);

						if (mobj.Player == null)
						{
							SaveGame.Write(this.data, this.ptr + 132, 0);
						}
						else
						{
							SaveGame.Write(this.data, this.ptr + 132, mobj.Player.Number + 1);
						}

						SaveGame.Write(this.data, this.ptr + 136, mobj.LastLook);

						if (mobj.SpawnPoint == null)
						{
							SaveGame.Write(this.data, this.ptr + 140, (short) 0);
							SaveGame.Write(this.data, this.ptr + 142, (short) 0);
							SaveGame.Write(this.data, this.ptr + 144, (short) 0);
							SaveGame.Write(this.data, this.ptr + 146, (short) 0);
							SaveGame.Write(this.data, this.ptr + 148, (short) 0);
						}
						else
						{
							SaveGame.Write(this.data, this.ptr + 140, (short) mobj.SpawnPoint.X.ToIntFloor());
							SaveGame.Write(this.data, this.ptr + 142, (short) mobj.SpawnPoint.Y.ToIntFloor());
							SaveGame.Write(this.data, this.ptr + 144, (short) Math.Round(mobj.SpawnPoint.Angle.ToDegree()));
							SaveGame.Write(this.data, this.ptr + 146, (short) mobj.SpawnPoint.Type);
							SaveGame.Write(this.data, this.ptr + 148, (short) mobj.SpawnPoint.Flags);
						}

						this.ptr += 154;
					}
				}

				this.data[this.ptr++] = (byte) ThinkerClass.End;
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
							this.data[this.ptr++] = (byte) SpecialClass.Ceiling;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, ceiling.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, (int) ceiling.Type);
							SaveGame.Write(this.data, this.ptr + 16, ceiling.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 20, ceiling.BottomHeight.Data);
							SaveGame.Write(this.data, this.ptr + 24, ceiling.TopHeight.Data);
							SaveGame.Write(this.data, this.ptr + 28, ceiling.Speed.Data);
							SaveGame.Write(this.data, this.ptr + 32, ceiling.Crush ? 1 : 0);
							SaveGame.Write(this.data, this.ptr + 36, ceiling.Direction);
							SaveGame.Write(this.data, this.ptr + 40, ceiling.Tag);
							SaveGame.Write(this.data, this.ptr + 44, ceiling.OldDirection);
							this.ptr += 48;
						}

						continue;
					}

					{
						var ceiling = thinker as CeilingMove;

						if (ceiling != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Ceiling;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, ceiling.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, (int) ceiling.Type);
							SaveGame.Write(this.data, this.ptr + 16, ceiling.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 20, ceiling.BottomHeight.Data);
							SaveGame.Write(this.data, this.ptr + 24, ceiling.TopHeight.Data);
							SaveGame.Write(this.data, this.ptr + 28, ceiling.Speed.Data);
							SaveGame.Write(this.data, this.ptr + 32, ceiling.Crush ? 1 : 0);
							SaveGame.Write(this.data, this.ptr + 36, ceiling.Direction);
							SaveGame.Write(this.data, this.ptr + 40, ceiling.Tag);
							SaveGame.Write(this.data, this.ptr + 44, ceiling.OldDirection);
							this.ptr += 48;

							continue;
						}
					}

					{
						var door = thinker as VerticalDoor;

						if (door != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Door;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, door.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, (int) door.Type);
							SaveGame.Write(this.data, this.ptr + 16, door.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 20, door.TopHeight.Data);
							SaveGame.Write(this.data, this.ptr + 24, door.Speed.Data);
							SaveGame.Write(this.data, this.ptr + 28, door.Direction);
							SaveGame.Write(this.data, this.ptr + 32, door.TopWait);
							SaveGame.Write(this.data, this.ptr + 36, door.TopCountDown);
							this.ptr += 40;

							continue;
						}
					}

					{
						var floor = thinker as FloorMove;

						if (floor != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Floor;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, floor.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, (int) floor.Type);
							SaveGame.Write(this.data, this.ptr + 16, floor.Crush ? 1 : 0);
							SaveGame.Write(this.data, this.ptr + 20, floor.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 24, floor.Direction);
							SaveGame.Write(this.data, this.ptr + 28, (int) floor.NewSpecial);
							SaveGame.Write(this.data, this.ptr + 32, floor.Texture);
							SaveGame.Write(this.data, this.ptr + 36, floor.FloorDestHeight.Data);
							SaveGame.Write(this.data, this.ptr + 40, floor.Speed.Data);
							this.ptr += 44;

							continue;
						}
					}

					{
						var plat = thinker as Platform;

						if (plat != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Plat;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, plat.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, plat.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 16, plat.Speed.Data);
							SaveGame.Write(this.data, this.ptr + 20, plat.Low.Data);
							SaveGame.Write(this.data, this.ptr + 24, plat.High.Data);
							SaveGame.Write(this.data, this.ptr + 28, plat.Wait);
							SaveGame.Write(this.data, this.ptr + 32, plat.Count);
							SaveGame.Write(this.data, this.ptr + 36, (int) plat.Status);
							SaveGame.Write(this.data, this.ptr + 40, (int) plat.OldStatus);
							SaveGame.Write(this.data, this.ptr + 44, plat.Crush ? 1 : 0);
							SaveGame.Write(this.data, this.ptr + 48, plat.Tag);
							SaveGame.Write(this.data, this.ptr + 52, (int) plat.Type);
							this.ptr += 56;

							continue;
						}
					}

					{
						var flash = thinker as LightFlash;

						if (flash != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Flash;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, flash.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, flash.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 16, flash.Count);
							SaveGame.Write(this.data, this.ptr + 20, flash.MaxLight);
							SaveGame.Write(this.data, this.ptr + 24, flash.MinLight);
							SaveGame.Write(this.data, this.ptr + 28, flash.MaxTime);
							SaveGame.Write(this.data, this.ptr + 32, flash.MinTime);
							this.ptr += 36;

							continue;
						}
					}

					{
						var strobe = thinker as StrobeFlash;

						if (strobe != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Strobe;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, strobe.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, strobe.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 16, strobe.Count);
							SaveGame.Write(this.data, this.ptr + 20, strobe.MinLight);
							SaveGame.Write(this.data, this.ptr + 24, strobe.MaxLight);
							SaveGame.Write(this.data, this.ptr + 28, strobe.DarkTime);
							SaveGame.Write(this.data, this.ptr + 32, strobe.BrightTime);
							this.ptr += 36;

							continue;
						}
					}

					{
						var glow = thinker as GlowingLight;

						if (glow != null)
						{
							this.data[this.ptr++] = (byte) SpecialClass.Glow;
							this.PadPointer();
							SaveGame.WriteThinkerState(this.data, this.ptr + 8, glow.ThinkerState);
							SaveGame.Write(this.data, this.ptr + 12, glow.Sector.Number);
							SaveGame.Write(this.data, this.ptr + 16, glow.MinLight);
							SaveGame.Write(this.data, this.ptr + 20, glow.MaxLight);
							SaveGame.Write(this.data, this.ptr + 24, glow.Direction);
							this.ptr += 28;

							continue;
						}
					}
				}

				this.data[this.ptr++] = (byte) SpecialClass.EndSpecials;
			}

			private static int ArchivePlayer(Player player, byte[] data, int p)
			{
				SaveGame.Write(data, p + 4, (int) player.PlayerState);
				SaveGame.Write(data, p + 16, player.ViewZ.Data);
				SaveGame.Write(data, p + 20, player.ViewHeight.Data);
				SaveGame.Write(data, p + 24, player.DeltaViewHeight.Data);
				SaveGame.Write(data, p + 28, player.Bob.Data);
				SaveGame.Write(data, p + 32, player.Health);
				SaveGame.Write(data, p + 36, player.ArmorPoints);
				SaveGame.Write(data, p + 40, player.ArmorType);

				for (var i = 0; i < (int) PowerType.Count; i++)
				{
					SaveGame.Write(data, p + 44 + 4 * i, player.Powers[i]);
				}

				for (var i = 0; i < (int) PowerType.Count; i++)
				{
					SaveGame.Write(data, p + 68 + 4 * i, player.Cards[i] ? 1 : 0);
				}

				SaveGame.Write(data, p + 92, player.Backpack ? 1 : 0);

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					SaveGame.Write(data, p + 96 + 4 * i, player.Frags[i]);
				}

				SaveGame.Write(data, p + 112, (int) player.ReadyWeapon);
				SaveGame.Write(data, p + 116, (int) player.PendingWeapon);

				for (var i = 0; i < (int) WeaponType.Count; i++)
				{
					SaveGame.Write(data, p + 120 + 4 * i, player.WeaponOwned[i] ? 1 : 0);
				}

				for (var i = 0; i < (int) AmmoType.Count; i++)
				{
					SaveGame.Write(data, p + 156 + 4 * i, player.Ammo[i]);
				}

				for (var i = 0; i < (int) AmmoType.Count; i++)
				{
					SaveGame.Write(data, p + 172 + 4 * i, player.MaxAmmo[i]);
				}

				SaveGame.Write(data, p + 188, player.AttackDown ? 1 : 0);
				SaveGame.Write(data, p + 192, player.UseDown ? 1 : 0);
				SaveGame.Write(data, p + 196, (int) player.Cheats);
				SaveGame.Write(data, p + 200, player.Refire);
				SaveGame.Write(data, p + 204, player.KillCount);
				SaveGame.Write(data, p + 208, player.ItemCount);
				SaveGame.Write(data, p + 212, player.SecretCount);
				SaveGame.Write(data, p + 220, player.DamageCount);
				SaveGame.Write(data, p + 224, player.BonusCount);
				SaveGame.Write(data, p + 232, player.ExtraLight);
				SaveGame.Write(data, p + 236, player.FixedColorMap);
				SaveGame.Write(data, p + 240, player.ColorMap);

				for (var i = 0; i < (int) PlayerSprite.Count; i++)
				{
					if (player.PlayerSprites[i].State == null)
					{
						SaveGame.Write(data, p + 244 + 16 * i, 0);
					}
					else
					{
						SaveGame.Write(data, p + 244 + 16 * i, player.PlayerSprites[i].State.Number);
					}

					SaveGame.Write(data, p + 244 + 16 * i + 4, player.PlayerSprites[i].Tics);
					SaveGame.Write(data, p + 244 + 16 * i + 8, player.PlayerSprites[i].Sx.Data);
					SaveGame.Write(data, p + 244 + 16 * i + 12, player.PlayerSprites[i].Sy.Data);
				}

				SaveGame.Write(data, p + 276, player.DidSecret ? 1 : 0);

				return p + 280;
			}

			private static int ArchiveSector(Sector sector, byte[] data, int p)
			{
				SaveGame.Write(data, p, (short) (sector.FloorHeight.ToIntFloor()));
				SaveGame.Write(data, p + 2, (short) (sector.CeilingHeight.ToIntFloor()));
				SaveGame.Write(data, p + 4, (short) sector.FloorFlat);
				SaveGame.Write(data, p + 6, (short) sector.CeilingFlat);
				SaveGame.Write(data, p + 8, (short) sector.LightLevel);
				SaveGame.Write(data, p + 10, (short) sector.Special);
				SaveGame.Write(data, p + 12, (short) sector.Tag);

				return p + 14;
			}

			private static int ArchiveLine(LineDef line, byte[] data, int p)
			{
				SaveGame.Write(data, p, (short) line.Flags);
				SaveGame.Write(data, p + 2, (short) line.Special);
				SaveGame.Write(data, p + 4, (short) line.Tag);
				p += 6;

				if (line.FrontSide != null)
				{
					var side = line.FrontSide;
					SaveGame.Write(data, p, (short) side.TextureOffset.ToIntFloor());
					SaveGame.Write(data, p + 2, (short) side.RowOffset.ToIntFloor());
					SaveGame.Write(data, p + 4, (short) side.TopTexture);
					SaveGame.Write(data, p + 6, (short) side.BottomTexture);
					SaveGame.Write(data, p + 8, (short) side.MiddleTexture);
					p += 10;
				}

				if (line.BackSide != null)
				{
					var side = line.BackSide;
					SaveGame.Write(data, p, (short) side.TextureOffset.ToIntFloor());
					SaveGame.Write(data, p + 2, (short) side.RowOffset.ToIntFloor());
					SaveGame.Write(data, p + 4, (short) side.TopTexture);
					SaveGame.Write(data, p + 6, (short) side.BottomTexture);
					SaveGame.Write(data, p + 8, (short) side.MiddleTexture);
					p += 10;
				}

				return p;
			}

			private static void Write(byte[] data, int p, int value)
			{
				data[p] = (byte) value;
				data[p + 1] = (byte) (value >> 8);
				data[p + 2] = (byte) (value >> 16);
				data[p + 3] = (byte) (value >> 24);
			}

			private static void Write(byte[] data, int p, uint value)
			{
				data[p] = (byte) value;
				data[p + 1] = (byte) (value >> 8);
				data[p + 2] = (byte) (value >> 16);
				data[p + 3] = (byte) (value >> 24);
			}

			private static void Write(byte[] data, int p, short value)
			{
				data[p] = (byte) value;
				data[p + 1] = (byte) (value >> 8);
			}

			private static void WriteThinkerState(byte[] data, int p, ThinkerState state)
			{
				switch (state)
				{
					case ThinkerState.InStasis:
						SaveGame.Write(data, p, 0);

						break;

					default:
						SaveGame.Write(data, p, 1);

						break;
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

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					options.Players[i].InGame = this.reader.ReadByte() != 0;
				}

				game.InitNew(options.Skill, options.Episode, options.Map);

				var a = this.reader.ReadByte();
				var b = this.reader.ReadByte();
				var c = this.reader.ReadByte();
				var levelTime = (a << 16) + (b << 8) + c;

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

				options.Sound.SetListener(game.World.ConsolePlayer.Mobj);
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
				var players = world.Options.Players;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!players[i].InGame)
					{
						continue;
					}

					this.PadPointer();

					this.UnArchivePlayer(players[i]);
				}
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
								mobj.Player = world.Options.Players[playerNumber - 1];
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

			private void UnArchivePlayer(Player player)
			{
				player.Clear();

				this.reader.BaseStream.Position += 4;
				player.PlayerState = (PlayerState) this.reader.ReadInt32();
				this.reader.BaseStream.Position += 8;
				player.ViewZ = new Fixed(this.reader.ReadInt32());
				player.ViewHeight = new Fixed(this.reader.ReadInt32());
				player.DeltaViewHeight = new Fixed(this.reader.ReadInt32());
				player.Bob = new Fixed(this.reader.ReadInt32());
				player.Health = this.reader.ReadInt32();
				player.ArmorPoints = this.reader.ReadInt32();
				player.ArmorType = this.reader.ReadInt32();

				for (var i = 0; i < (int) PowerType.Count; i++)
				{
					player.Powers[i] = this.reader.ReadInt32();
				}

				for (var i = 0; i < (int) PowerType.Count; i++)
				{
					player.Cards[i] = this.reader.ReadInt32() != 0;
				}

				player.Backpack = this.reader.ReadInt32() != 0;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					player.Frags[i] = this.reader.ReadInt32();
				}

				player.ReadyWeapon = (WeaponType) this.reader.ReadInt32();
				player.PendingWeapon = (WeaponType) this.reader.ReadInt32();

				for (var i = 0; i < (int) WeaponType.Count; i++)
				{
					player.WeaponOwned[i] = this.reader.ReadInt32() != 0;
				}

				for (var i = 0; i < (int) AmmoType.Count; i++)
				{
					player.Ammo[i] = this.reader.ReadInt32();
				}

				for (var i = 0; i < (int) AmmoType.Count; i++)
				{
					player.MaxAmmo[i] = this.reader.ReadInt32();
				}

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
