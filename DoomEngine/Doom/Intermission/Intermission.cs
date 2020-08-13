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

namespace DoomEngine.Doom.Intermission
{
	using Audio;
	using Common;
	using Game;
	using System.Collections.Generic;

	public sealed class Intermission
	{
		private GameOptions options;

		// Contains information passed into intermission.
		private IntermissionInfo info;
		private PlayerScores[] scores;

		// Used to accelerate or skip a stage.
		private bool accelerateStage;

		// Specifies current state.
		private IntermissionState state;

		private int[] killCount;
		private int[] itemCount;
		private int[] secretCount;
		private int[] fragCount;
		private int timeCount;
		private int parCount;
		private int pauseCount;

		private int spState;

		private int ngState;
		private bool doFrags;

		private int dmState;
		private int[][] dmFragCount;
		private int[] dmTotalCount;

		private DoomRandom random;
		private Animation[] animations;
		private bool showYouAreHere;

		// Used for general timing.
		private int count;

		// Used for timing of background animation.
		private int bgCount;

		private bool completed;

		public Intermission(GameOptions options, IntermissionInfo info)
		{
			this.options = options;
			this.info = info;

			this.scores = info.Players;

			this.killCount = new int[Player.MaxPlayerCount];
			this.itemCount = new int[Player.MaxPlayerCount];
			this.secretCount = new int[Player.MaxPlayerCount];
			this.fragCount = new int[Player.MaxPlayerCount];

			this.dmFragCount = new int[Player.MaxPlayerCount][];

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				this.dmFragCount[i] = new int[Player.MaxPlayerCount];
			}

			this.dmTotalCount = new int[Player.MaxPlayerCount];

			if (options.Deathmatch != 0)
			{
				this.InitDeathmatchStats();
			}
			else if (options.NetGame)
			{
				this.InitNetGameStats();
			}
			else
			{
				this.InitSinglePLayerStats();
			}

			this.completed = false;
		}

		////////////////////////////////////////////////////////////
		// Initialization
		////////////////////////////////////////////////////////////

		private void InitSinglePLayerStats()
		{
			this.state = IntermissionState.StatCount;
			this.accelerateStage = false;
			this.spState = 1;
			this.killCount[0] = this.itemCount[0] = this.secretCount[0] = -1;
			this.timeCount = this.parCount = -1;
			this.pauseCount = GameConst.TicRate;

			this.InitAnimatedBack();
		}

		private void InitNetGameStats()
		{
			this.state = IntermissionState.StatCount;
			this.accelerateStage = false;
			this.ngState = 1;
			this.pauseCount = GameConst.TicRate;

			var frags = 0;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (!this.options.Players[i].InGame)
				{
					continue;
				}

				this.killCount[i] = this.itemCount[i] = this.secretCount[i] = this.fragCount[i] = 0;

				frags += this.GetFragSum(i);
			}

			this.doFrags = frags > 0;

			this.InitAnimatedBack();
		}

		private void InitDeathmatchStats()
		{
			this.state = IntermissionState.StatCount;
			this.accelerateStage = false;
			this.dmState = 1;
			this.pauseCount = GameConst.TicRate;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (this.options.Players[i].InGame)
				{
					for (var j = 0; j < Player.MaxPlayerCount; j++)
					{
						if (this.options.Players[j].InGame)
						{
							this.dmFragCount[i][j] = 0;
						}
					}

					this.dmTotalCount[i] = 0;
				}
			}

			this.InitAnimatedBack();
		}

		private void InitNoState()
		{
			this.state = IntermissionState.NoState;
			this.accelerateStage = false;
			this.count = 10;
		}

		private static readonly int showNextLocDelay = 4;

		private void InitShowNextLoc()
		{
			this.state = IntermissionState.ShowNextLoc;
			this.accelerateStage = false;
			this.count = Intermission.showNextLocDelay * GameConst.TicRate;

			this.InitAnimatedBack();
		}

		private void InitAnimatedBack()
		{
			if (this.options.GameMode == GameMode.Commercial)
			{
				return;
			}

			if (this.info.Episode > 2)
			{
				return;
			}

			if (this.animations == null)
			{
				this.animations = new Animation[AnimationInfo.Episodes[this.info.Episode].Count];

				for (var i = 0; i < this.animations.Length; i++)
				{
					this.animations[i] = new Animation(this, AnimationInfo.Episodes[this.info.Episode][i], i);
				}

				this.random = new DoomRandom();
			}

			foreach (var animation in this.animations)
			{
				animation.Reset(this.bgCount);
			}
		}

		////////////////////////////////////////////////////////////
		// Update
		////////////////////////////////////////////////////////////

		public UpdateResult Update()
		{
			// Counter for general background animation.
			this.bgCount++;

			this.CheckForAccelerate();

			if (this.bgCount == 1)
			{
				// intermission music
				if (this.options.GameMode == GameMode.Commercial)
				{
					this.options.Music.StartMusic(Bgm.DM2INT, true);
				}
				else
				{
					this.options.Music.StartMusic(Bgm.INTER, true);
				}
			}

			switch (this.state)
			{
				case IntermissionState.StatCount:
					if (this.options.Deathmatch != 0)
					{
						this.UpdateDeathmatchStats();
					}
					else if (this.options.NetGame)
					{
						this.UpdateNetGameStats();
					}
					else
					{
						this.UpdateSinglePlayerStats();
					}

					break;

				case IntermissionState.ShowNextLoc:
					this.UpdateShowNextLoc();

					break;

				case IntermissionState.NoState:
					this.UpdateNoState();

					break;
			}

			if (this.completed)
			{
				return UpdateResult.Completed;
			}
			else
			{
				if (this.bgCount == 1)
				{
					return UpdateResult.NeedWipe;
				}
				else
				{
					return UpdateResult.None;
				}
			}
		}

		private void UpdateSinglePlayerStats()
		{
			this.UpdateAnimatedBack();

			if (this.accelerateStage && this.spState != 10)
			{
				this.accelerateStage = false;
				this.killCount[0] = (this.scores[0].KillCount * 100) / this.info.MaxKillCount;
				this.itemCount[0] = (this.scores[0].ItemCount * 100) / this.info.MaxItemCount;
				this.secretCount[0] = (this.scores[0].SecretCount * 100) / this.info.MaxSecretCount;
				this.timeCount = this.scores[0].Time / GameConst.TicRate;
				this.parCount = this.info.ParTime / GameConst.TicRate;
				this.StartSound(Sfx.BAREXP);
				this.spState = 10;
			}

			if (this.spState == 2)
			{
				this.killCount[0] += 2;

				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				if (this.killCount[0] >= (this.scores[0].KillCount * 100) / this.info.MaxKillCount)
				{
					this.killCount[0] = (this.scores[0].KillCount * 100) / this.info.MaxKillCount;
					this.StartSound(Sfx.BAREXP);
					this.spState++;
				}
			}
			else if (this.spState == 4)
			{
				this.itemCount[0] += 2;

				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				if (this.itemCount[0] >= (this.scores[0].ItemCount * 100) / this.info.MaxItemCount)
				{
					this.itemCount[0] = (this.scores[0].ItemCount * 100) / this.info.MaxItemCount;
					this.StartSound(Sfx.BAREXP);
					this.spState++;
				}
			}
			else if (this.spState == 6)
			{
				this.secretCount[0] += 2;

				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				if (this.secretCount[0] >= (this.scores[0].SecretCount * 100) / this.info.MaxSecretCount)
				{
					this.secretCount[0] = (this.scores[0].SecretCount * 100) / this.info.MaxSecretCount;
					this.StartSound(Sfx.BAREXP);
					this.spState++;
				}
			}

			else if (this.spState == 8)
			{
				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				this.timeCount += 3;

				if (this.timeCount >= this.scores[0].Time / GameConst.TicRate)
				{
					this.timeCount = this.scores[0].Time / GameConst.TicRate;
				}

				this.parCount += 3;

				if (this.parCount >= this.info.ParTime / GameConst.TicRate)
				{
					this.parCount = this.info.ParTime / GameConst.TicRate;

					if (this.timeCount >= this.scores[0].Time / GameConst.TicRate)
					{
						this.StartSound(Sfx.BAREXP);
						this.spState++;
					}
				}
			}
			else if (this.spState == 10)
			{
				if (this.accelerateStage)
				{
					this.StartSound(Sfx.SGCOCK);

					if (this.options.GameMode == GameMode.Commercial)
					{
						this.InitNoState();
					}
					else
					{
						this.InitShowNextLoc();
					}
				}
			}
			else if ((this.spState & 1) != 0)
			{
				if (--this.pauseCount == 0)
				{
					this.spState++;
					this.pauseCount = GameConst.TicRate;
				}
			}
		}

		private void UpdateNetGameStats()
		{
			this.UpdateAnimatedBack();

			bool stillTicking;

			if (this.accelerateStage && this.ngState != 10)
			{
				this.accelerateStage = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!this.options.Players[i].InGame)
					{
						continue;
					}

					this.killCount[i] = (this.scores[i].KillCount * 100) / this.info.MaxKillCount;
					this.itemCount[i] = (this.scores[i].ItemCount * 100) / this.info.MaxItemCount;
					this.secretCount[i] = (this.scores[i].SecretCount * 100) / this.info.MaxSecretCount;
				}

				this.StartSound(Sfx.BAREXP);

				this.ngState = 10;
			}

			if (this.ngState == 2)
			{
				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				stillTicking = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!this.options.Players[i].InGame)
					{
						continue;
					}

					this.killCount[i] += 2;

					if (this.killCount[i] >= (this.scores[i].KillCount * 100) / this.info.MaxKillCount)
					{
						this.killCount[i] = (this.scores[i].KillCount * 100) / this.info.MaxKillCount;
					}
					else
					{
						stillTicking = true;
					}
				}

				if (!stillTicking)
				{
					this.StartSound(Sfx.BAREXP);
					this.ngState++;
				}
			}
			else if (this.ngState == 4)
			{
				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				stillTicking = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!this.options.Players[i].InGame)
					{
						continue;
					}

					this.itemCount[i] += 2;

					if (this.itemCount[i] >= (this.scores[i].ItemCount * 100) / this.info.MaxItemCount)
					{
						this.itemCount[i] = (this.scores[i].ItemCount * 100) / this.info.MaxItemCount;
					}
					else
					{
						stillTicking = true;
					}
				}

				if (!stillTicking)
				{
					this.StartSound(Sfx.BAREXP);
					this.ngState++;
				}
			}
			else if (this.ngState == 6)
			{
				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				stillTicking = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!this.options.Players[i].InGame)
					{
						continue;
					}

					this.secretCount[i] += 2;

					if (this.secretCount[i] >= (this.scores[i].SecretCount * 100) / this.info.MaxSecretCount)
					{
						this.secretCount[i] = (this.scores[i].SecretCount * 100) / this.info.MaxSecretCount;
					}
					else
					{
						stillTicking = true;
					}
				}

				if (!stillTicking)
				{
					this.StartSound(Sfx.BAREXP);

					if (this.doFrags)
					{
						this.ngState++;
					}
					else
					{
						this.ngState += 3;
					}
				}
			}
			else if (this.ngState == 8)
			{
				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				stillTicking = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (!this.options.Players[i].InGame)
					{
						continue;
					}

					this.fragCount[i] += 1;
					var sum = this.GetFragSum(i);

					if (this.fragCount[i] >= sum)
					{
						this.fragCount[i] = sum;
					}
					else
					{
						stillTicking = true;
					}
				}

				if (!stillTicking)
				{
					this.StartSound(Sfx.PLDETH);
					this.ngState++;
				}
			}
			else if (this.ngState == 10)
			{
				if (this.accelerateStage)
				{
					this.StartSound(Sfx.SGCOCK);

					if (this.options.GameMode == GameMode.Commercial)
					{
						this.InitNoState();
					}
					else
					{
						this.InitShowNextLoc();
					}
				}
			}
			else if ((this.ngState & 1) != 0)
			{
				if (--this.pauseCount == 0)
				{
					this.ngState++;
					this.pauseCount = GameConst.TicRate;
				}
			}
		}

		private void UpdateDeathmatchStats()
		{
			this.UpdateAnimatedBack();

			bool stillticking;

			if (this.accelerateStage && this.dmState != 4)
			{
				this.accelerateStage = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (this.options.Players[i].InGame)
					{
						for (var j = 0; j < Player.MaxPlayerCount; j++)
						{
							if (this.options.Players[j].InGame)
							{
								this.dmFragCount[i][j] = this.scores[i].Frags[j];
							}
						}

						this.dmTotalCount[i] = this.GetFragSum(i);
					}
				}

				this.StartSound(Sfx.BAREXP);

				this.dmState = 4;
			}

			if (this.dmState == 2)
			{
				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				stillticking = false;

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (this.options.Players[i].InGame)
					{
						for (var j = 0; j < Player.MaxPlayerCount; j++)
						{
							if (this.options.Players[j].InGame && this.dmFragCount[i][j] != this.scores[i].Frags[j])
							{
								if (this.scores[i].Frags[j] < 0)
								{
									this.dmFragCount[i][j]--;
								}
								else
								{
									this.dmFragCount[i][j]++;
								}

								if (this.dmFragCount[i][j] > 99)
								{
									this.dmFragCount[i][j] = 99;
								}

								if (this.dmFragCount[i][j] < -99)
								{
									this.dmFragCount[i][j] = -99;
								}

								stillticking = true;
							}
						}

						this.dmTotalCount[i] = this.GetFragSum(i);

						if (this.dmTotalCount[i] > 99)
						{
							this.dmTotalCount[i] = 99;
						}

						if (this.dmTotalCount[i] < -99)
						{
							this.dmTotalCount[i] = -99;
						}
					}
				}

				if (!stillticking)
				{
					this.StartSound(Sfx.BAREXP);
					this.dmState++;
				}
			}
			else if (this.dmState == 4)
			{
				if (this.accelerateStage)
				{
					this.StartSound(Sfx.SLOP);

					if (this.options.GameMode == GameMode.Commercial)
					{
						this.InitNoState();
					}
					else
					{
						this.InitShowNextLoc();
					}
				}
			}
			else if ((this.dmState & 1) != 0)
			{
				if (--this.pauseCount == 0)
				{
					this.dmState++;
					this.pauseCount = GameConst.TicRate;
				}
			}
		}

		private void UpdateShowNextLoc()
		{
			this.UpdateAnimatedBack();

			if (--this.count == 0 || this.accelerateStage)
			{
				this.InitNoState();
			}
			else
			{
				this.showYouAreHere = (this.count & 31) < 20;
			}
		}

		private void UpdateNoState()
		{
			this.UpdateAnimatedBack();

			if (--this.count == 0)
			{
				this.completed = true;
			}
		}

		private void UpdateAnimatedBack()
		{
			if (this.options.GameMode == GameMode.Commercial)
			{
				return;
			}

			if (this.info.Episode > 2)
			{
				return;
			}

			foreach (var a in this.animations)
			{
				a.Update(this.bgCount);
			}
		}

		////////////////////////////////////////////////////////////
		// Check for button press
		////////////////////////////////////////////////////////////

		private void CheckForAccelerate()
		{
			// Check for button presses to skip delays.
			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				var player = this.options.Players[i];

				if (player.InGame)
				{
					if ((player.Cmd.Buttons & TicCmdButtons.Attack) != 0)
					{
						if (!player.AttackDown)
						{
							this.accelerateStage = true;
						}

						player.AttackDown = true;
					}
					else
					{
						player.AttackDown = false;
					}

					if ((player.Cmd.Buttons & TicCmdButtons.Use) != 0)
					{
						if (!player.UseDown)
						{
							this.accelerateStage = true;
						}

						player.UseDown = true;
					}
					else
					{
						player.UseDown = false;
					}
				}
			}
		}

		////////////////////////////////////////////////////////////
		// Miscellaneous functions
		////////////////////////////////////////////////////////////

		private int GetFragSum(int playerNumber)
		{
			var frags = 0;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (this.options.Players[i].InGame && i != playerNumber)
				{
					frags += this.scores[playerNumber].Frags[i];
				}
			}

			frags -= this.scores[playerNumber].Frags[playerNumber];

			return frags;
		}

		private void StartSound(Sfx sfx)
		{
			this.options.Sound.StartSound(sfx);
		}

		public GameOptions Options => this.options;
		public IntermissionInfo Info => this.info;
		public IntermissionState State => this.state;
		public IReadOnlyList<int> KillCount => this.killCount;
		public IReadOnlyList<int> ItemCount => this.itemCount;
		public IReadOnlyList<int> SecretCount => this.secretCount;
		public IReadOnlyList<int> FragCount => this.fragCount;
		public int TimeCount => this.timeCount;
		public int ParCount => this.parCount;
		public int[][] DeathmatchFrags => this.dmFragCount;
		public int[] DeathmatchTotals => this.dmTotalCount;
		public bool DoFrags => this.doFrags;
		public DoomRandom Random => this.random;
		public Animation[] Animations => this.animations;
		public bool ShowYouAreHere => this.showYouAreHere;
	}
}
