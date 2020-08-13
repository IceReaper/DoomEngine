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

namespace DoomEngine.Doom
{
	using Audio;
	using Common;
	using Game;

	public sealed class OpeningSequence
	{
		private CommonResource resource;
		private GameOptions options;

		private OpeningSequenceState state;

		private int currentStage;
		private int nextStage;

		private int count;
		private int timer;

		private TicCmd[] cmds;
		private Demo demo;
		private DoomGame game;

		private bool reset;

		public OpeningSequence(CommonResource resource, GameOptions options)
		{
			this.resource = resource;
			this.options = options;

			this.cmds = new TicCmd[Player.MaxPlayerCount];

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				this.cmds[i] = new TicCmd();
			}

			this.currentStage = 0;
			this.nextStage = 0;

			this.reset = false;

			this.StartTitleScreen();
		}

		public void Reset()
		{
			this.currentStage = 0;
			this.nextStage = 0;

			this.demo = null;
			this.game = null;

			this.reset = true;

			this.StartTitleScreen();
		}

		public UpdateResult Update()
		{
			var updateResult = UpdateResult.None;

			if (this.nextStage != this.currentStage)
			{
				switch (this.nextStage)
				{
					case 0:
						this.StartTitleScreen();

						break;

					case 1:
						this.StartDemo("DEMO1");

						break;

					case 2:
						this.StartCreditScreen();

						break;

					case 3:
						this.StartDemo("DEMO2");

						break;

					case 4:
						this.StartTitleScreen();

						break;

					case 5:
						this.StartDemo("DEMO3");

						break;

					case 6:
						this.StartCreditScreen();

						break;

					case 7:
						this.StartDemo("DEMO4");

						break;
				}

				this.currentStage = this.nextStage;
				updateResult = UpdateResult.NeedWipe;
			}

			switch (this.currentStage)
			{
				case 0:
					this.count++;

					if (this.count == this.timer)
					{
						this.nextStage = 1;
					}

					break;

				case 1:
					if (!this.demo.ReadCmd(this.cmds))
					{
						this.nextStage = 2;
					}
					else
					{
						this.game.Update(this.cmds);
					}

					break;

				case 2:
					this.count++;

					if (this.count == this.timer)
					{
						this.nextStage = 3;
					}

					break;

				case 3:
					if (!this.demo.ReadCmd(this.cmds))
					{
						this.nextStage = 4;
					}
					else
					{
						this.game.Update(this.cmds);
					}

					break;

				case 4:
					this.count++;

					if (this.count == this.timer)
					{
						this.nextStage = 5;
					}

					break;

				case 5:
					if (!this.demo.ReadCmd(this.cmds))
					{
						if (this.resource.Wad.GetLumpNumber("DEMO4") == -1)
						{
							this.nextStage = 0;
						}
						else
						{
							this.nextStage = 6;
						}
					}
					else
					{
						this.game.Update(this.cmds);
					}

					break;

				case 6:
					this.count++;

					if (this.count == this.timer)
					{
						this.nextStage = 7;
					}

					break;

				case 7:
					if (!this.demo.ReadCmd(this.cmds))
					{
						this.nextStage = 0;
					}
					else
					{
						this.game.Update(this.cmds);
					}

					break;
			}

			if (this.state == OpeningSequenceState.Title && this.count == 1)
			{
				if (this.options.GameMode == GameMode.Commercial)
				{
					this.options.Music.StartMusic(Bgm.DM2TTL, false);
				}
				else
				{
					this.options.Music.StartMusic(Bgm.INTRO, false);
				}
			}

			if (this.reset)
			{
				this.reset = false;

				return UpdateResult.NeedWipe;
			}
			else
			{
				return updateResult;
			}
		}

		private void StartTitleScreen()
		{
			this.state = OpeningSequenceState.Title;

			this.count = 0;

			if (this.options.GameMode == GameMode.Commercial)
			{
				this.timer = 35 * 11;
			}
			else
			{
				this.timer = 170;
			}
		}

		private void StartCreditScreen()
		{
			this.state = OpeningSequenceState.Credit;

			this.count = 0;
			this.timer = 200;
		}

		private void StartDemo(string lump)
		{
			this.state = OpeningSequenceState.Demo;

			this.demo = new Demo(this.resource.Wad.ReadLump(lump));
			this.demo.Options.GameVersion = this.options.GameVersion;
			this.demo.Options.GameMode = this.options.GameMode;
			this.demo.Options.MissionPack = this.options.MissionPack;
			this.demo.Options.Renderer = this.options.Renderer;
			this.demo.Options.Sound = this.options.Sound;
			this.demo.Options.Music = this.options.Music;

			this.game = new DoomGame(this.resource, this.demo.Options);
			this.game.DeferedInitNew();
		}

		public OpeningSequenceState State => this.state;
		public DoomGame DemoGame => this.game;
	}
}
