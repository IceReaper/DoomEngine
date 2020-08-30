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

	public sealed class Intermission
	{
		private GameOptions options;

		// Contains information passed into intermission.
		private IntermissionInfo info;
		private PlayerScores scores;

		// Used to accelerate or skip a stage.
		private bool accelerateStage;

		// Specifies current state.
		private IntermissionState state;

		private int killCount;
		private int itemCount;
		private int secretCount;
		private int timeCount;
		private int parCount;
		private int pauseCount;

		private int spState;

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

			this.scores = info.Player;

			this.killCount = 0;
			this.itemCount = 0;
			this.secretCount = 0;

			this.InitSinglePLayerStats();

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
			this.killCount = this.itemCount = this.secretCount = -1;
			this.timeCount = this.parCount = -1;
			this.pauseCount = GameConst.TicRate;

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
			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
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
				if (DoomApplication.Instance.IWad == "doom2"
					|| DoomApplication.Instance.IWad == "freedoom2"
					|| DoomApplication.Instance.IWad == "plutonia"
					|| DoomApplication.Instance.IWad == "tnt")
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
					this.UpdateSinglePlayerStats();

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
				this.killCount = (this.scores.KillCount * 100) / this.info.MaxKillCount;
				this.itemCount = (this.scores.ItemCount * 100) / this.info.MaxItemCount;
				this.secretCount = (this.scores.SecretCount * 100) / this.info.MaxSecretCount;
				this.timeCount = this.scores.Time / GameConst.TicRate;
				this.parCount = this.info.ParTime / GameConst.TicRate;
				this.StartSound(Sfx.BAREXP);
				this.spState = 10;
			}

			if (this.spState == 2)
			{
				this.killCount += 2;

				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				if (this.killCount >= (this.scores.KillCount * 100) / this.info.MaxKillCount)
				{
					this.killCount = (this.scores.KillCount * 100) / this.info.MaxKillCount;
					this.StartSound(Sfx.BAREXP);
					this.spState++;
				}
			}
			else if (this.spState == 4)
			{
				this.itemCount += 2;

				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				if (this.itemCount >= (this.scores.ItemCount * 100) / this.info.MaxItemCount)
				{
					this.itemCount = (this.scores.ItemCount * 100) / this.info.MaxItemCount;
					this.StartSound(Sfx.BAREXP);
					this.spState++;
				}
			}
			else if (this.spState == 6)
			{
				this.secretCount += 2;

				if ((this.bgCount & 3) == 0)
				{
					this.StartSound(Sfx.PISTOL);
				}

				if (this.secretCount >= (this.scores.SecretCount * 100) / this.info.MaxSecretCount)
				{
					this.secretCount = (this.scores.SecretCount * 100) / this.info.MaxSecretCount;
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

				if (this.timeCount >= this.scores.Time / GameConst.TicRate)
				{
					this.timeCount = this.scores.Time / GameConst.TicRate;
				}

				this.parCount += 3;

				if (this.parCount >= this.info.ParTime / GameConst.TicRate)
				{
					this.parCount = this.info.ParTime / GameConst.TicRate;

					if (this.timeCount >= this.scores.Time / GameConst.TicRate)
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

					if (DoomApplication.Instance.IWad == "doom2"
						|| DoomApplication.Instance.IWad == "freedoom2"
						|| DoomApplication.Instance.IWad == "plutonia"
						|| DoomApplication.Instance.IWad == "tnt")
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
			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
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
			var player = this.options.Player;

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

		////////////////////////////////////////////////////////////
		// Miscellaneous functions
		////////////////////////////////////////////////////////////

		private void StartSound(Sfx sfx)
		{
			this.options.Sound.StartSound(sfx);
		}

		public GameOptions Options => this.options;
		public IntermissionInfo Info => this.info;
		public IntermissionState State => this.state;
		public int KillCount => this.killCount;
		public int ItemCount => this.itemCount;
		public int SecretCount => this.secretCount;
		public int TimeCount => this.timeCount;
		public int ParCount => this.parCount;
		public DoomRandom Random => this.random;
		public Animation[] Animations => this.animations;
		public bool ShowYouAreHere => this.showYouAreHere;
	}
}
