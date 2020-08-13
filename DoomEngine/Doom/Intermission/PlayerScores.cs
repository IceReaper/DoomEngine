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
	using Game;

	public class PlayerScores
	{
		// Whether the player is in game.
		private bool inGame;

		// Player stats, kills, collected items etc.
		private int killCount;
		private int itemCount;
		private int secretCount;
		private int time;
		private int[] frags;

		public PlayerScores()
		{
			this.frags = new int[Player.MaxPlayerCount];
		}

		public bool InGame
		{
			get => this.inGame;
			set => this.inGame = value;
		}

		public int KillCount
		{
			get => this.killCount;
			set => this.killCount = value;
		}

		public int ItemCount
		{
			get => this.itemCount;
			set => this.itemCount = value;
		}

		public int SecretCount
		{
			get => this.secretCount;
			set => this.secretCount = value;
		}

		public int Time
		{
			get => this.time;
			set => this.time = value;
		}

		public int[] Frags
		{
			get => this.frags;
		}
	}
}
