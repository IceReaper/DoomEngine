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
	using Event;
	using Info;
	using Intermission;
	using System;
	using World;

	public sealed class DoomGame
	{
		private CommonResource resource;
		private GameOptions options;

		private GameAction gameAction;
		private GameState gameState;

		private int gameTic;
		private DoomRandom random;

		private World world;
		private Intermission intermission;
		private Finale finale;

		private bool paused;

		private int loadGameSlotNumber;
		private int saveGameSlotNumber;
		private string saveGameDescription;

		public DoomGame(CommonResource resource, GameOptions options)
		{
			this.resource = resource;
			this.options = options;

			this.gameAction = GameAction.Nothing;

			this.gameTic = 0;
			this.random = new DoomRandom();
		}

		////////////////////////////////////////////////////////////
		// Public methods to control the game state
		////////////////////////////////////////////////////////////

		/// <summary>
		/// Start a new game.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void DeferedInitNew()
		{
			this.gameAction = GameAction.NewGame;
		}

		/// <summary>
		/// Start a new game.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void DeferedInitNew(GameSkill skill, int episode, int map)
		{
			this.options.Skill = skill;
			this.options.Episode = episode;
			this.options.Map = map;
			this.gameAction = GameAction.NewGame;
		}

		/// <summary>
		/// Load the saved game at the given slot number.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void LoadGame(int slotNumber)
		{
			this.loadGameSlotNumber = slotNumber;
			this.gameAction = GameAction.LoadGame;
		}

		/// <summary>
		/// Save the game at the given slot number.
		/// Can be called by the startup code or the menu task.
		/// </summary>
		public void SaveGame(int slotNumber, string description)
		{
			this.saveGameSlotNumber = slotNumber;
			this.saveGameDescription = description;
			this.gameAction = GameAction.SaveGame;
		}

		/// <summary>
		/// Advance the game one frame.
		/// </summary>
		public UpdateResult Update(TicCmd[] cmds)
		{
			// Do player reborns if needed.
			var players = this.options.Players;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame && players[i].PlayerState == PlayerState.Reborn)
				{
					this.DoReborn(i);
				}
			}

			// Do things to change the game state.
			while (this.gameAction != GameAction.Nothing)
			{
				switch (this.gameAction)
				{
					case GameAction.LoadLevel:
						this.DoLoadLevel();

						break;

					case GameAction.NewGame:
						this.DoNewGame();

						break;

					case GameAction.LoadGame:
						this.DoLoadGame();

						break;

					case GameAction.SaveGame:
						this.DoSaveGame();

						break;

					case GameAction.Completed:
						this.DoCompleted();

						break;

					case GameAction.Victory:
						this.DoFinale();

						break;

					case GameAction.WorldDone:
						this.DoWorldDone();

						break;

					case GameAction.Nothing:
						break;
				}
			}

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame)
				{
					var cmd = players[i].Cmd;
					cmd.CopyFrom(cmds[i]);

					// Check for turbo cheats.
					if (cmd.ForwardMove > GameConst.TurboThreshold && (this.world.LevelTime & 31) == 0 && ((this.world.LevelTime >> 5) & 3) == i)
					{
						var player = players[this.options.ConsolePlayer];
						player.SendMessage(players[i].Name + " is turbo!");
					}
				}
			}

			// Check for special buttons.
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame)
				{
					if ((players[i].Cmd.Buttons & TicCmdButtons.Special) != 0)
					{
						if ((players[i].Cmd.Buttons & TicCmdButtons.SpecialMask) == TicCmdButtons.Pause)
						{
							this.paused = !this.paused;

							if (this.paused)
							{
								this.options.Sound.Pause();
							}
							else
							{
								this.options.Sound.Resume();
							}
						}
					}
				}
			}

			// Do main actions.
			var result = UpdateResult.None;

			switch (this.gameState)
			{
				case GameState.Level:
					if (!this.paused || this.world.FirstTicIsNotYetDone)
					{
						result = this.world.Update();

						if (result == UpdateResult.Completed)
						{
							this.gameAction = GameAction.Completed;
						}
					}

					break;

				case GameState.Intermission:
					result = this.intermission.Update();

					if (result == UpdateResult.Completed)
					{
						this.gameAction = GameAction.WorldDone;

						if (this.world.SecretExit)
						{
							players[this.options.ConsolePlayer].DidSecret = true;
						}

						if (DoomApplication.Instance.IWad == "doom2"
							|| DoomApplication.Instance.IWad == "freedoom2"
							|| DoomApplication.Instance.IWad == "plutonia"
							|| DoomApplication.Instance.IWad == "tnt")
						{
							switch (this.options.Map)
							{
								case 6:
								case 11:
								case 20:
								case 30:
									this.DoFinale();
									result = UpdateResult.NeedWipe;

									break;

								case 15:
								case 31:
									if (this.world.SecretExit)
									{
										this.DoFinale();
										result = UpdateResult.NeedWipe;
									}

									break;
							}
						}
					}

					break;

				case GameState.Finale:
					result = this.finale.Update();

					if (result == UpdateResult.Completed)
					{
						this.gameAction = GameAction.WorldDone;
					}

					break;
			}

			this.gameTic++;

			if (result == UpdateResult.NeedWipe)
			{
				return UpdateResult.NeedWipe;
			}
			else
			{
				return UpdateResult.None;
			}
		}

		////////////////////////////////////////////////////////////
		// Actual game actions
		////////////////////////////////////////////////////////////

		// It seems that these methods should not be called directly
		// from outside for some reason.
		// So if you want to start a new game or do load / save, use
		// the following public methods.
		//
		//     - DeferedInitNew
		//     - LoadGame
		//     - SaveGame

		private void DoLoadLevel()
		{
			this.gameAction = GameAction.Nothing;

			this.gameState = GameState.Level;

			var players = this.options.Players;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame && players[i].PlayerState == PlayerState.Dead)
				{
					players[i].PlayerState = PlayerState.Reborn;
				}

				Array.Clear(players[i].Frags, 0, players[i].Frags.Length);
			}

			this.intermission = null;

			this.options.Sound.Reset();

			this.world = new World(this.resource, this.options, this);

			this.options.UserInput.Reset();
		}

		private void DoNewGame()
		{
			this.gameAction = GameAction.Nothing;

			this.InitNew(this.options.Skill, this.options.Episode, this.options.Map);
		}

		private void DoLoadGame()
		{
			this.gameAction = GameAction.Nothing;

			var path = "doomsav" + this.loadGameSlotNumber + ".dsg";
			SaveAndLoad.Load(this, path);
		}

		private void DoSaveGame()
		{
			this.gameAction = GameAction.Nothing;

			var path = "doomsav" + this.saveGameSlotNumber + ".dsg";
			SaveAndLoad.Save(this, this.saveGameDescription, path);
			this.world.ConsolePlayer.SendMessage(DoomInfo.Strings.GGSAVED);
		}

		private void DoCompleted()
		{
			this.gameAction = GameAction.Nothing;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (this.options.Players[i].InGame)
				{
					// Take away cards and stuff.
					this.options.Players[i].FinishLevel();
				}
			}

			if (DoomApplication.Instance.IWad != "doom2"
				&& DoomApplication.Instance.IWad != "freedoom2"
				&& DoomApplication.Instance.IWad != "plutonia"
				&& DoomApplication.Instance.IWad != "tnt")
			{
				switch (this.options.Map)
				{
					case 8:
						this.gameAction = GameAction.Victory;

						return;

					case 9:
						for (var i = 0; i < Player.MaxPlayerCount; i++)
						{
							this.options.Players[i].DidSecret = true;
						}

						break;
				}
			}

			if ((this.options.Map == 8)
				&& (DoomApplication.Instance.IWad != "doom2"
					&& DoomApplication.Instance.IWad != "freedoom2"
					&& DoomApplication.Instance.IWad != "plutonia"
					&& DoomApplication.Instance.IWad != "tnt"))
			{
				// Victory.
				this.gameAction = GameAction.Victory;

				return;
			}

			if ((this.options.Map == 9)
				&& (DoomApplication.Instance.IWad != "doom2"
					&& DoomApplication.Instance.IWad != "freedoom2"
					&& DoomApplication.Instance.IWad != "plutonia"
					&& DoomApplication.Instance.IWad != "tnt"))

			{
				// Exit secret level.
				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					this.options.Players[i].DidSecret = true;
				}
			}

			var imInfo = this.options.IntermissionInfo;

			imInfo.DidSecret = this.options.Players[this.options.ConsolePlayer].DidSecret;
			imInfo.Episode = this.options.Episode - 1;
			imInfo.LastLevel = this.options.Map - 1;

			// IntermissionInfo.Next is 0 biased, unlike GameOptions.Map.
			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				if (this.world.SecretExit)
				{
					switch (this.options.Map)
					{
						case 15:
							imInfo.NextLevel = 30;

							break;

						case 31:
							imInfo.NextLevel = 31;

							break;
					}
				}
				else
				{
					switch (this.options.Map)
					{
						case 31:
						case 32:
							imInfo.NextLevel = 15;

							break;

						default:
							imInfo.NextLevel = this.options.Map;

							break;
					}
				}
			}
			else
			{
				if (this.world.SecretExit)
				{
					// Go to secret level.
					imInfo.NextLevel = 8;
				}
				else if (this.options.Map == 9)
				{
					// Returning from secret level.
					switch (this.options.Episode)
					{
						case 1:
							imInfo.NextLevel = 3;

							break;

						case 2:
							imInfo.NextLevel = 5;

							break;

						case 3:
							imInfo.NextLevel = 6;

							break;

						case 4:
							imInfo.NextLevel = 2;

							break;
					}
				}
				else
				{
					// Go to next level.
					imInfo.NextLevel = this.options.Map;
				}
			}

			imInfo.MaxKillCount = this.world.TotalKills;
			imInfo.MaxItemCount = this.world.TotalItems;
			imInfo.MaxSecretCount = this.world.TotalSecrets;
			imInfo.TotalFrags = 0;

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				imInfo.ParTime = 35 * DoomInfo.ParTimes.Doom2[this.options.Map - 1];
			}
			else
			{
				imInfo.ParTime = 35 * DoomInfo.ParTimes.Doom1[this.options.Episode - 1][this.options.Map - 1];
			}

			var players = this.options.Players;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				imInfo.Players[i].InGame = players[i].InGame;
				imInfo.Players[i].KillCount = players[i].KillCount;
				imInfo.Players[i].ItemCount = players[i].ItemCount;
				imInfo.Players[i].SecretCount = players[i].SecretCount;
				imInfo.Players[i].Time = this.world.LevelTime;
				Array.Copy(players[i].Frags, imInfo.Players[i].Frags, Player.MaxPlayerCount);
			}

			this.gameState = GameState.Intermission;
			this.intermission = new Intermission(this.options, imInfo);
		}

		private void DoWorldDone()
		{
			this.gameAction = GameAction.Nothing;

			this.gameState = GameState.Level;
			this.options.Map = this.options.IntermissionInfo.NextLevel + 1;
			this.DoLoadLevel();
		}

		private void DoFinale()
		{
			this.gameAction = GameAction.Nothing;

			this.gameState = GameState.Finale;
			this.finale = new Finale(this.options);
		}

		////////////////////////////////////////////////////////////
		// Miscellaneous things
		////////////////////////////////////////////////////////////

		public void InitNew(GameSkill skill, int episode, int map)
		{
			skill = (GameSkill) Math.Clamp((int) skill, (int) GameSkill.Baby, (int) GameSkill.Nightmare);

			if (DoomApplication.Instance.IWad == "doom" || DoomApplication.Instance.IWad == "freedoom")
			{
				episode = Math.Clamp(episode, 1, 4);
			}
			else if (DoomApplication.Instance.IWad == "doom1")
			{
				episode = 1;
			}
			else
			{
				episode = Math.Clamp(episode, 1, 3);
			}

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				map = Math.Clamp(map, 1, 32);
			}
			else
			{
				map = Math.Clamp(map, 1, 9);
			}

			this.random.Clear();

			// Force players to be initialized upon first level load.
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				this.options.Players[i].PlayerState = PlayerState.Reborn;
			}

			this.DoLoadLevel();
		}

		public bool DoEvent(DoomEvent e)
		{
			if (this.gameState == GameState.Level)
			{
				return this.world.DoEvent(e);
			}
			else if (this.gameState == GameState.Finale)
			{
				return this.finale.DoEvent(e);
			}

			return false;
		}

		private void DoReborn(int playerNumber)
		{
			if (!this.options.NetGame)
			{
				// Reload the level from scratch.
				this.gameAction = GameAction.LoadLevel;
			}
			else
			{
				// Respawn at the start.

				// First dissasociate the corpse.
				this.options.Players[playerNumber].Mobj.Player = null;

				var ta = this.world.ThingAllocation;

				// Spawn at random spot if in death match.
				if (this.options.Deathmatch != 0)
				{
					ta.DeathMatchSpawnPlayer(playerNumber);

					return;
				}

				if (ta.CheckSpot(playerNumber, ta.PlayerStarts[playerNumber]))
				{
					ta.SpawnPlayer(ta.PlayerStarts[playerNumber]);

					return;
				}

				// Try to spawn at one of the other players spots.
				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (ta.CheckSpot(playerNumber, ta.PlayerStarts[i]))
					{
						// Fake as other player.
						ta.PlayerStarts[i].Type = playerNumber + 1;

						this.world.ThingAllocation.SpawnPlayer(ta.PlayerStarts[i]);

						// Restore.
						ta.PlayerStarts[i].Type = i + 1;

						return;
					}
				}

				// He's going to be inside something.
				// Too bad.
				this.world.ThingAllocation.SpawnPlayer(ta.PlayerStarts[playerNumber]);
			}
		}

		public GameOptions Options => this.options;
		public Player[] Players => this.options.Players;
		public GameState State => this.gameState;
		public int GameTic => this.gameTic;
		public DoomRandom Random => this.random;
		public World World => this.world;
		public Intermission Intermission => this.intermission;
		public Finale Finale => this.finale;
		public bool Paused => this.paused;

		private enum GameAction
		{
			Nothing,
			LoadLevel,
			NewGame,
			LoadGame,
			SaveGame,
			Completed,
			Victory,
			WorldDone
		}
	}
}
