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
	using Intermission;
	using Platform;
	using Platform.Null;

	public sealed class GameOptions
	{
		private Player player;

		private int episode;
		private int map;
		private GameSkill skill;

		private bool fastMonsters;
		private bool respawnMonsters;
		private bool noMonsters;

		private IntermissionInfo intermissionInfo;

		private IRenderer renderer;
		private ISound sound;
		private IMusic music;
		private IUserInput userInput;

		public GameOptions()
		{
			this.player = new Player();

			this.episode = 1;
			this.map = 1;
			this.skill = GameSkill.Medium;

			this.fastMonsters = false;
			this.respawnMonsters = false;
			this.noMonsters = false;

			this.intermissionInfo = new IntermissionInfo();

			this.renderer = null;
			this.sound = NullSound.GetInstance();
			this.music = NullMusic.GetInstance();
			this.userInput = NullUserInput.GetInstance();
		}

		public Player Player
		{
			get => this.player;
		}

		public int Episode
		{
			get => this.episode;
			set => this.episode = value;
		}

		public int Map
		{
			get => this.map;
			set => this.map = value;
		}

		public GameSkill Skill
		{
			get => this.skill;
			set => this.skill = value;
		}

		public bool FastMonsters
		{
			get => this.fastMonsters;
			set => this.fastMonsters = value;
		}

		public bool RespawnMonsters
		{
			get => this.respawnMonsters;
			set => this.respawnMonsters = value;
		}

		public bool NoMonsters
		{
			get => this.noMonsters;
			set => this.noMonsters = value;
		}

		public IntermissionInfo IntermissionInfo
		{
			get => this.intermissionInfo;
		}

		public IRenderer Renderer
		{
			get => this.renderer;
			set => this.renderer = value;
		}

		public ISound Sound
		{
			get => this.sound;

			set
			{
				if (value != null)
				{
					this.sound = value;
				}
				else
				{
					this.sound = NullSound.GetInstance();
				}
			}
		}

		public IMusic Music
		{
			get => this.music;

			set
			{
				if (value != null)
				{
					this.music = value;
				}
				else
				{
					this.music = NullMusic.GetInstance();
				}
			}
		}

		public IUserInput UserInput
		{
			get => this.userInput;

			set
			{
				if (value != null)
				{
					this.userInput = value;
				}
				else
				{
					this.userInput = NullUserInput.GetInstance();
				}
			}
		}
	}
}
