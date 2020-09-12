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

namespace DoomEngine.Doom.Menu
{
	using Audio;
	using Event;
	using Game;
	using Info;
	using UserInput;

	public sealed class DoomMenu
	{
		private DoomApplication app;
		private GameOptions options;

		private SelectableMenu main;
		private SelectableMenu episodeMenu;
		private SelectableMenu skillMenu;
		private SelectableMenu optionMenu;
		private SelectableMenu volume;
		private LoadMenu load;
		private SaveMenu save;
		private HelpScreen help;

		private PressAnyKey thisIsShareware;
		private PressAnyKey saveFailed;
		private YesNoConfirm nightmareConfirm;
		private YesNoConfirm endGameConfirm;
		private QuitConfirm quitConfirm;

		private MenuDef current;

		private bool active;

		private int tics;

		private int selectedEpisode;

		private SaveSlots saveSlots;

		public DoomMenu(DoomApplication app)
		{
			this.app = app;
			this.options = app.Options;

			this.thisIsShareware = new PressAnyKey(this, DoomInfo.Strings.SWSTRING, null);

			this.saveFailed = new PressAnyKey(this, DoomInfo.Strings.SAVEDEAD, null);

			this.nightmareConfirm = new YesNoConfirm(this, DoomInfo.Strings.NIGHTMARE, () => app.NewGame(GameSkill.Nightmare, this.selectedEpisode, 1));

			this.endGameConfirm = new YesNoConfirm(this, DoomInfo.Strings.ENDGAME, () => app.EndGame());

			this.quitConfirm = new QuitConfirm(this, app);

			this.skillMenu = new SelectableMenu(
				this,
				"M_NEWG",
				96,
				14,
				"M_SKILL",
				54,
				38,
				2,
				new SimpleMenuItem("M_JKILL", 16, 58, 48, 63, () => app.NewGame(GameSkill.Baby, this.selectedEpisode, 1), null),
				new SimpleMenuItem("M_ROUGH", 16, 74, 48, 79, () => app.NewGame(GameSkill.Easy, this.selectedEpisode, 1), null),
				new SimpleMenuItem("M_HURT", 16, 90, 48, 95, () => app.NewGame(GameSkill.Medium, this.selectedEpisode, 1), null),
				new SimpleMenuItem("M_ULTRA", 16, 106, 48, 111, () => app.NewGame(GameSkill.Hard, this.selectedEpisode, 1), null),
				new SimpleMenuItem("M_NMARE", 16, 122, 48, 127, null, this.nightmareConfirm)
			);

			if (DoomApplication.Instance.IWad == "doom" || DoomApplication.Instance.IWad == "freedoom")
			{
				this.episodeMenu = new SelectableMenu(
					this,
					"M_EPISOD",
					54,
					38,
					0,
					new SimpleMenuItem("M_EPI1", 16, 58, 48, 63, () => this.selectedEpisode = 1, this.skillMenu),
					new SimpleMenuItem("M_EPI2", 16, 74, 48, 79, () => this.selectedEpisode = 2, this.skillMenu),
					new SimpleMenuItem("M_EPI3", 16, 90, 48, 95, () => this.selectedEpisode = 3, this.skillMenu),
					new SimpleMenuItem("M_EPI4", 16, 106, 48, 111, () => this.selectedEpisode = 4, this.skillMenu)
				);
			}
			else
			{
				if (DoomApplication.Instance.IWad == "doom1")
				{
					this.episodeMenu = new SelectableMenu(
						this,
						"M_EPISOD",
						54,
						38,
						0,
						new SimpleMenuItem("M_EPI1", 16, 58, 48, 63, () => this.selectedEpisode = 1, this.skillMenu),
						new SimpleMenuItem("M_EPI2", 16, 74, 48, 79, null, this.thisIsShareware),
						new SimpleMenuItem("M_EPI3", 16, 90, 48, 95, null, this.thisIsShareware)
					);
				}
				else
				{
					this.episodeMenu = new SelectableMenu(
						this,
						"M_EPISOD",
						54,
						38,
						0,
						new SimpleMenuItem("M_EPI1", 16, 58, 48, 63, () => this.selectedEpisode = 1, this.skillMenu),
						new SimpleMenuItem("M_EPI2", 16, 74, 48, 79, () => this.selectedEpisode = 2, this.skillMenu),
						new SimpleMenuItem("M_EPI3", 16, 90, 48, 95, () => this.selectedEpisode = 3, this.skillMenu)
					);
				}
			}

			var sound = this.options.Sound;
			var music = this.options.Music;

			this.volume = new SelectableMenu(
				this,
				"M_SVOL",
				60,
				38,
				0,
				new SliderMenuItem("M_SFXVOL", 48, 59, 80, 64, sound.MaxVolume + 1, () => sound.Volume, vol => sound.Volume = vol),
				new SliderMenuItem("M_MUSVOL", 48, 91, 80, 96, music.MaxVolume + 1, () => music.Volume, vol => music.Volume = vol)
			);

			var renderer = this.options.Renderer;
			var userInput = this.options.UserInput;

			this.optionMenu = new SelectableMenu(
				this,
				"M_OPTTTL",
				108,
				15,
				0,
				new SimpleMenuItem("M_ENDGAM", 28, 32, 60, 37, null, this.endGameConfirm, () => app.State == ApplicationState.Game),
				new ToggleMenuItem(
					"M_MESSG",
					28,
					48,
					60,
					53,
					"M_MSGON",
					"M_MSGOFF",
					180,
					() => renderer.DisplayMessage ? 0 : 1,
					value => renderer.DisplayMessage = value == 0
				),
				new SliderMenuItem(
					"M_SCRNSZ",
					28,
					80 - 16,
					60,
					85 - 16,
					renderer.MaxWindowSize + 1,
					() => renderer.WindowSize,
					size => renderer.WindowSize = size
				),
				new SliderMenuItem(
					"M_MSENS",
					28,
					112 - 16,
					60,
					117 - 16,
					userInput.MaxMouseSensitivity + 1,
					() => userInput.MouseSensitivity,
					ms => userInput.MouseSensitivity = ms
				),
				new SimpleMenuItem("M_SVOL", 28, 144 - 16, 60, 149 - 16, null, this.volume)
			);

			this.load = new LoadMenu(
				this,
				"M_LOADG",
				72,
				28,
				0,
				new TextBoxMenuItem(48, 49, 72, 61),
				new TextBoxMenuItem(48, 65, 72, 77),
				new TextBoxMenuItem(48, 81, 72, 93),
				new TextBoxMenuItem(48, 97, 72, 109),
				new TextBoxMenuItem(48, 113, 72, 125),
				new TextBoxMenuItem(48, 129, 72, 141)
			);

			this.save = new SaveMenu(
				this,
				"M_SAVEG",
				72,
				28,
				0,
				new TextBoxMenuItem(48, 49, 72, 61),
				new TextBoxMenuItem(48, 65, 72, 77),
				new TextBoxMenuItem(48, 81, 72, 93),
				new TextBoxMenuItem(48, 97, 72, 109),
				new TextBoxMenuItem(48, 113, 72, 125),
				new TextBoxMenuItem(48, 129, 72, 141)
			);

			this.help = new HelpScreen(this);

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				this.main = new SelectableMenu(
					this,
					"M_DOOM",
					94,
					2,
					0,
					new SimpleMenuItem("M_NGAME", 65, 67, 97, 72, null, this.skillMenu),
					new SimpleMenuItem("M_OPTION", 65, 83, 97, 88, null, this.optionMenu),
					new SimpleMenuItem("M_LOADG", 65, 99, 97, 104, null, this.load),
					new SimpleMenuItem(
						"M_SAVEG",
						65,
						115,
						97,
						120,
						null,
						this.save,
						() => !(app.State == ApplicationState.Game && app.Game.State != GameState.Level)
					),
					new SimpleMenuItem("M_QUITG", 65, 131, 97, 136, null, this.quitConfirm)
				);
			}
			else
			{
				this.main = new SelectableMenu(
					this,
					"M_DOOM",
					94,
					2,
					0,
					new SimpleMenuItem("M_NGAME", 65, 59, 97, 64, null, this.episodeMenu),
					new SimpleMenuItem("M_OPTION", 65, 75, 97, 80, null, this.optionMenu),
					new SimpleMenuItem("M_LOADG", 65, 91, 97, 96, null, this.load),
					new SimpleMenuItem(
						"M_SAVEG",
						65,
						107,
						97,
						112,
						null,
						this.save,
						() => !(app.State == ApplicationState.Game && app.Game.State != GameState.Level)
					),
					new SimpleMenuItem("M_RDTHIS", 65, 123, 97, 128, null, this.help),
					new SimpleMenuItem("M_QUITG", 65, 139, 97, 144, null, this.quitConfirm)
				);
			}

			this.current = this.main;
			this.active = false;

			this.tics = 0;

			this.selectedEpisode = 1;

			this.saveSlots = new SaveSlots();
		}

		public bool DoEvent(DoomEvent e)
		{
			if (this.active)
			{
				if (this.current.DoEvent(e))
				{
					return true;
				}

				if (e.Key == DoomKey.Escape && e.Type == EventType.KeyDown)
				{
					this.Close();
				}

				return true;
			}
			else
			{
				if (e.Key == DoomKey.Escape && e.Type == EventType.KeyDown)
				{
					this.SetCurrent(this.main);
					this.Open();
					this.StartSound(Sfx.SWTCHN);

					return true;
				}

				if (e.Type == EventType.KeyDown && this.app.State == ApplicationState.Opening)
				{
					if (e.Key == DoomKey.Enter || e.Key == DoomKey.Space || e.Key == DoomKey.LControl || e.Key == DoomKey.RControl || e.Key == DoomKey.Escape)
					{
						this.SetCurrent(this.main);
						this.Open();
						this.StartSound(Sfx.SWTCHN);

						return true;
					}
				}

				return false;
			}
		}

		public void Update()
		{
			this.tics++;

			if (this.current != null)
			{
				this.current.Update();
			}

			if (this.active)
			{
				this.app.PauseGame();
			}
		}

		public void SetCurrent(MenuDef next)
		{
			this.current = next;
			this.current.Open();
		}

		public void Open()
		{
			this.active = true;
		}

		public void Close()
		{
			this.active = false;
			this.app.ResumeGame();
		}

		public void StartSound(Sfx sfx)
		{
			this.options.Sound.StartSound(sfx);
		}

		public void NotifySaveFailed()
		{
			this.SetCurrent(this.saveFailed);
		}

		public void ShowHelpScreen()
		{
			this.SetCurrent(this.help);
			this.Open();
			this.StartSound(Sfx.SWTCHN);
		}

		public void ShowSaveScreen()
		{
			this.SetCurrent(this.save);
			this.Open();
			this.StartSound(Sfx.SWTCHN);
		}

		public void ShowLoadScreen()
		{
			this.SetCurrent(this.load);
			this.Open();
			this.StartSound(Sfx.SWTCHN);
		}

		public void ShowVolumeControl()
		{
			this.SetCurrent(this.volume);
			this.Open();
			this.StartSound(Sfx.SWTCHN);
		}

		public void QuickSave()
		{
			if (this.save.LastSaveSlot == -1)
			{
				this.ShowSaveScreen();
			}
			else
			{
				var desc = this.saveSlots[this.save.LastSaveSlot];
				var confirm = new YesNoConfirm(this, ((string) DoomInfo.Strings.QSPROMPT).Replace("%s", desc), () => this.save.DoSave(this.save.LastSaveSlot));
				this.SetCurrent(confirm);
				this.Open();
				this.StartSound(Sfx.SWTCHN);
			}
		}

		public void QuickLoad()
		{
			if (this.save.LastSaveSlot == -1)
			{
				var pak = new PressAnyKey(this, DoomInfo.Strings.QSAVESPOT, null);
				this.SetCurrent(pak);
				this.Open();
				this.StartSound(Sfx.SWTCHN);
			}
			else
			{
				var desc = this.saveSlots[this.save.LastSaveSlot];
				var confirm = new YesNoConfirm(this, ((string) DoomInfo.Strings.QLPROMPT).Replace("%s", desc), () => this.load.DoLoad(this.save.LastSaveSlot));
				this.SetCurrent(confirm);
				this.Open();
				this.StartSound(Sfx.SWTCHN);
			}
		}

		public void EndGame()
		{
			this.SetCurrent(this.endGameConfirm);
			this.Open();
			this.StartSound(Sfx.SWTCHN);
		}

		public void Quit()
		{
			this.SetCurrent(this.quitConfirm);
			this.Open();
			this.StartSound(Sfx.SWTCHN);
		}

		public DoomApplication Application => this.app;
		public GameOptions Options => this.app.Options;
		public MenuDef Current => this.current;
		public bool Active => this.active;
		public int Tics => this.tics;
		public SaveSlots SaveSlots => this.saveSlots;
	}
}
