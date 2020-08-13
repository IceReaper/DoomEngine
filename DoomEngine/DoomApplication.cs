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

namespace DoomEngine
{
	using Audio;
	using Doom;
	using Doom.Common;
	using Doom.Event;
	using Doom.Game;
	using Doom.Info;
	using Doom.Menu;
	using SFML.Graphics;
	using SFML.Window;
	using SoftwareRendering;
	using System;
	using System.Collections.Generic;
	using System.Runtime.ExceptionServices;
	using UserInput;
	using EventType = Doom.Event.EventType;

	public sealed class DoomApplication : IDisposable
	{
		private Config config;

		private RenderWindow window;

		private CommonResource resource;
		private SfmlRenderer renderer;
		private SfmlSound sound;
		private SfmlMusic music;
		private SfmlUserInput userInput;

		private List<DoomEvent> events;

		private GameOptions options;

		private DoomMenu menu;

		private OpeningSequence opening;

		private DemoPlayback demoPlayback;

		private TicCmd[] cmds;
		private DoomGame game;

		private WipeEffect wipe;
		private bool wiping;

		private ApplicationState currentState;
		private ApplicationState nextState;
		private bool needWipe;

		private bool sendPause;

		private bool quit;
		private string quitMessage;

		public DoomApplication(CommandLineArgs args)
		{
			this.config = new Config(ConfigUtilities.GetConfigPath());

			try
			{
				this.config.video_screenwidth = Math.Clamp(this.config.video_screenwidth, 320, 3200);
				this.config.video_screenheight = Math.Clamp(this.config.video_screenheight, 200, 2000);
				var videoMode = new VideoMode((uint) this.config.video_screenwidth, (uint) this.config.video_screenheight);
				var style = Styles.Close | Styles.Titlebar;

				if (this.config.video_fullscreen)
				{
					style = Styles.Fullscreen;
				}

				this.window = new RenderWindow(videoMode, ApplicationInfo.Title, style);
				this.window.Clear(new Color(64, 64, 64));
				this.window.Display();

				if (args.deh.Present)
				{
					DeHackEd.ReadFiles(args.deh.Value);
				}

				this.resource = new CommonResource(this.GetWadPaths(args));

				this.renderer = new SfmlRenderer(this.config, this.window, this.resource);

				if (!args.nosound.Present && !args.nosfx.Present)
				{
					this.sound = new SfmlSound(this.config, this.resource.Wad);
				}

				if (!args.nosound.Present && !args.nomusic.Present)
				{
					this.music = ConfigUtilities.GetSfmlMusicInstance(this.config, this.resource.Wad);
				}

				this.userInput = new SfmlUserInput(this.config, this.window, !args.nomouse.Present);

				this.events = new List<DoomEvent>();

				this.options = new GameOptions();
				this.options.GameVersion = this.resource.Wad.GameVersion;
				this.options.GameMode = this.resource.Wad.GameMode;
				this.options.MissionPack = this.resource.Wad.MissionPack;
				this.options.Renderer = this.renderer;
				this.options.Sound = this.sound;
				this.options.Music = this.music;
				this.options.UserInput = this.userInput;

				this.menu = new DoomMenu(this);

				this.opening = new OpeningSequence(this.resource, this.options);

				this.cmds = new TicCmd[Player.MaxPlayerCount];

				for (var i = 0; i < Player.MaxPlayerCount; i++)
				{
					this.cmds[i] = new TicCmd();
				}

				this.game = new DoomGame(this.resource, this.options);

				this.wipe = new WipeEffect(this.renderer.WipeBandCount, this.renderer.WipeHeight);
				this.wiping = false;

				this.currentState = ApplicationState.None;
				this.nextState = ApplicationState.Opening;
				this.needWipe = false;

				this.sendPause = false;

				this.quit = false;
				this.quitMessage = null;

				this.CheckGameArgs(args);

				this.window.Closed += (sender, e) => this.window.Close();
				this.window.KeyPressed += this.KeyPressed;
				this.window.KeyReleased += this.KeyReleased;

				if (!args.timedemo.Present)
				{
					this.window.SetFramerateLimit(35);
				}
			}
			catch (Exception e)
			{
				this.Dispose();
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private string[] GetWadPaths(CommandLineArgs args)
		{
			var wadPaths = new List<string>();

			if (args.iwad.Present)
			{
				wadPaths.Add(args.iwad.Value);
			}
			else
			{
				wadPaths.Add(ConfigUtilities.GetDefaultIwadPath());
			}

			if (args.file.Present)
			{
				foreach (var path in args.file.Value)
				{
					wadPaths.Add(path);
				}
			}

			return wadPaths.ToArray();
		}

		private void CheckGameArgs(CommandLineArgs args)
		{
			if (args.warp.Present)
			{
				this.nextState = ApplicationState.Game;
				this.options.Episode = args.warp.Value.Item1;
				this.options.Map = args.warp.Value.Item2;
				this.game.DeferedInitNew();
			}

			if (args.skill.Present)
			{
				this.options.Skill = (GameSkill) (args.skill.Value - 1);
			}

			if (args.deathmatch.Present)
			{
				this.options.Deathmatch = 1;
			}

			if (args.altdeath.Present)
			{
				this.options.Deathmatch = 2;
			}

			if (args.fast.Present)
			{
				this.options.FastMonsters = true;
			}

			if (args.respawn.Present)
			{
				this.options.RespawnMonsters = true;
			}

			if (args.nomonsters.Present)
			{
				this.options.NoMonsters = true;
			}

			if (args.loadgame.Present)
			{
				this.nextState = ApplicationState.Game;
				this.game.LoadGame(args.loadgame.Value);
			}

			if (args.playdemo.Present)
			{
				this.nextState = ApplicationState.DemoPlayback;
				this.demoPlayback = new DemoPlayback(this.resource, this.options, args.playdemo.Value);
			}

			if (args.timedemo.Present)
			{
				this.nextState = ApplicationState.DemoPlayback;
				this.demoPlayback = new DemoPlayback(this.resource, this.options, args.timedemo.Value);
			}
		}

		public void Run()
		{
			while (this.window.IsOpen)
			{
				this.window.DispatchEvents();
				this.DoEvents();

				if (this.Update() == UpdateResult.Completed)
				{
					this.config.Save(ConfigUtilities.GetConfigPath());

					return;
				}
			}
		}

		public void NewGame(GameSkill skill, int episode, int map)
		{
			this.game.DeferedInitNew(skill, episode, map);
			this.nextState = ApplicationState.Game;
		}

		public void EndGame()
		{
			this.nextState = ApplicationState.Opening;
		}

		private void DoEvents()
		{
			if (this.wiping)
			{
				return;
			}

			foreach (var e in this.events)
			{
				if (this.menu.DoEvent(e))
				{
					continue;
				}

				if (e.Type == EventType.KeyDown)
				{
					if (this.CheckFunctionKey(e.Key))
					{
						continue;
					}
				}

				if (this.currentState == ApplicationState.Game)
				{
					if (e.Key == DoomKey.Pause && e.Type == EventType.KeyDown)
					{
						this.sendPause = true;

						continue;
					}

					if (this.game.DoEvent(e))
					{
						continue;
					}
				}
				else if (this.currentState == ApplicationState.DemoPlayback)
				{
					this.demoPlayback.DoEvent(e);
				}
			}

			this.events.Clear();
		}

		private bool CheckFunctionKey(DoomKey key)
		{
			switch (key)
			{
				case DoomKey.F1:
					this.menu.ShowHelpScreen();

					return true;

				case DoomKey.F2:
					this.menu.ShowSaveScreen();

					return true;

				case DoomKey.F3:
					this.menu.ShowLoadScreen();

					return true;

				case DoomKey.F4:
					this.menu.ShowVolumeControl();

					return true;

				case DoomKey.F6:
					this.menu.QuickSave();

					return true;

				case DoomKey.F7:
					if (this.currentState == ApplicationState.Game)
					{
						this.menu.EndGame();
					}
					else
					{
						this.options.Sound.StartSound(Sfx.OOF);
					}

					return true;

				case DoomKey.F8:
					this.renderer.DisplayMessage = !this.renderer.DisplayMessage;

					if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level)
					{
						string msg;

						if (this.renderer.DisplayMessage)
						{
							msg = DoomInfo.Strings.MSGON;
						}
						else
						{
							msg = DoomInfo.Strings.MSGOFF;
						}

						this.game.World.ConsolePlayer.SendMessage(msg);
					}

					this.menu.StartSound(Sfx.SWTCHN);

					return true;

				case DoomKey.F9:
					this.menu.QuickLoad();

					return true;

				case DoomKey.F10:
					this.menu.Quit();

					return true;

				case DoomKey.F11:
					var gcl = this.renderer.GammaCorrectionLevel;
					gcl++;

					if (gcl > this.renderer.MaxGammaCorrectionLevel)
					{
						gcl = 0;
					}

					this.renderer.GammaCorrectionLevel = gcl;

					if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level)
					{
						string msg;

						if (gcl == 0)
						{
							msg = DoomInfo.Strings.GAMMALVL0;
						}
						else
						{
							msg = "Gamma correction level " + gcl;
						}

						this.game.World.ConsolePlayer.SendMessage(msg);
					}

					return true;

				case DoomKey.Add:
				case DoomKey.Quote:
					if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level && this.game.World.AutoMap.Visible)
					{
						return false;
					}
					else
					{
						this.renderer.WindowSize = Math.Min(this.renderer.WindowSize + 1, this.renderer.MaxWindowSize);
						this.options.Sound.StartSound(Sfx.STNMOV);

						return true;
					}

				case DoomKey.Subtract:
				case DoomKey.Hyphen:
					if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level && this.game.World.AutoMap.Visible)
					{
						return false;
					}
					else
					{
						this.renderer.WindowSize = Math.Max(this.renderer.WindowSize - 1, 0);
						this.options.Sound.StartSound(Sfx.STNMOV);

						return true;
					}

				default:
					return false;
			}
		}

		private UpdateResult Update()
		{
			if (!this.wiping)
			{
				this.menu.Update();

				if (this.nextState != this.currentState)
				{
					if (this.nextState != ApplicationState.Opening)
					{
						this.opening.Reset();
					}

					if (this.nextState != ApplicationState.DemoPlayback)
					{
						this.demoPlayback = null;
					}

					this.currentState = this.nextState;
				}

				if (this.quit)
				{
					return UpdateResult.Completed;
				}

				if (this.needWipe)
				{
					this.needWipe = false;
					this.StartWipe();
				}
			}

			if (!this.wiping)
			{
				switch (this.currentState)
				{
					case ApplicationState.Opening:
						if (this.opening.Update() == UpdateResult.NeedWipe)
						{
							this.StartWipe();
						}

						break;

					case ApplicationState.DemoPlayback:
						var result = this.demoPlayback.Update();

						if (result == UpdateResult.NeedWipe)
						{
							this.StartWipe();
						}
						else if (result == UpdateResult.Completed)
						{
							this.Quit("FPS: " + this.demoPlayback.Fps.ToString("0.0"));
						}

						break;

					case ApplicationState.Game:
						this.userInput.BuildTicCmd(this.cmds[this.options.ConsolePlayer]);

						if (this.sendPause)
						{
							this.sendPause = false;
							this.cmds[this.options.ConsolePlayer].Buttons |= (byte) (TicCmdButtons.Special | TicCmdButtons.Pause);
						}

						if (this.game.Update(this.cmds) == UpdateResult.NeedWipe)
						{
							this.StartWipe();
						}

						break;

					default:
						throw new Exception("Invalid application state!");
				}
			}

			if (this.wiping)
			{
				var result = this.wipe.Update();
				this.renderer.RenderWipe(this, this.wipe);

				if (result == UpdateResult.Completed)
				{
					this.wiping = false;
				}
			}
			else
			{
				this.renderer.Render(this);
			}

			this.options.Sound.Update();

			return UpdateResult.None;
		}

		private void KeyPressed(object sender, KeyEventArgs e)
		{
			if (this.events.Count < 64)
			{
				this.events.Add(new DoomEvent(EventType.KeyDown, (DoomKey) e.Code));
			}
		}

		private void KeyReleased(object sender, KeyEventArgs e)
		{
			if (this.events.Count < 64)
			{
				this.events.Add(new DoomEvent(EventType.KeyUp, (DoomKey) e.Code));
			}
		}

		private void StartWipe()
		{
			this.wipe.Start();
			this.renderer.InitializeWipe();
			this.wiping = true;
		}

		public void PauseGame()
		{
			if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level && !this.game.Paused && !this.sendPause)
			{
				this.sendPause = true;
			}
		}

		public void ResumeGame()
		{
			if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level && this.game.Paused && !this.sendPause)
			{
				this.sendPause = true;
			}
		}

		public bool SaveGame(int slotNumber, string description)
		{
			if (this.currentState == ApplicationState.Game && this.game.State == GameState.Level)
			{
				this.game.SaveGame(slotNumber, description);

				return true;
			}
			else
			{
				return false;
			}
		}

		public void LoadGame(int slotNumber)
		{
			this.game.LoadGame(slotNumber);
			this.nextState = ApplicationState.Game;
		}

		public void Quit()
		{
			this.quit = true;
		}

		public void Quit(string message)
		{
			this.quit = true;
			this.quitMessage = message;
		}

		public void Dispose()
		{
			if (this.userInput != null)
			{
				this.userInput.Dispose();
				this.userInput = null;
			}

			if (this.music != null)
			{
				this.music.Dispose();
				this.music = null;
			}

			if (this.sound != null)
			{
				this.sound.Dispose();
				this.sound = null;
			}

			if (this.renderer != null)
			{
				this.renderer.Dispose();
				this.renderer = null;
			}

			if (this.resource != null)
			{
				this.resource.Dispose();
				this.resource = null;
			}

			if (this.window != null)
			{
				this.window.Dispose();
				this.window = null;
			}
		}

		public ApplicationState State => this.currentState;
		public OpeningSequence Opening => this.opening;
		public DemoPlayback DemoPlayback => this.demoPlayback;
		public GameOptions Options => this.options;
		public DoomGame Game => this.game;
		public DoomMenu Menu => this.menu;
		public string QuitMessage => this.quitMessage;
	}
}
