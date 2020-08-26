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
	using System;

	public class IntermissionInfo
	{
		// Episode number (0-2).
		private int episode;

		// If true, splash the secret level.
		private bool didSecret;

		// Previous and next levels, origin 0.
		private int lastLevel;
		private int nextLevel;

		private int maxKillCount;
		private int maxItemCount;
		private int maxSecretCount;

		// The par time.
		private int parTime;

		private PlayerScores player;

		public IntermissionInfo()
		{
			this.player = new PlayerScores();
		}

		public int Episode
		{
			get => this.episode;
			set => this.episode = value;
		}

		public bool DidSecret
		{
			get => this.didSecret;
			set => this.didSecret = value;
		}

		public int LastLevel
		{
			get => this.lastLevel;
			set => this.lastLevel = value;
		}

		public int NextLevel
		{
			get => this.nextLevel;
			set => this.nextLevel = value;
		}

		public int MaxKillCount
		{
			get => Math.Max(this.maxKillCount, 1);
			set => this.maxKillCount = value;
		}

		public int MaxItemCount
		{
			get => Math.Max(this.maxItemCount, 1);
			set => this.maxItemCount = value;
		}

		public int MaxSecretCount
		{
			get => Math.Max(this.maxSecretCount, 1);
			set => this.maxSecretCount = value;
		}

		public int ParTime
		{
			get => this.parTime;
			set => this.parTime = value;
		}

		public PlayerScores Player
		{
			get => this.player;
		}
	}
}
