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
	using System;

	public sealed class Demo
	{
		private int p;
		private byte[] data;

		private GameOptions options;

		private int playerCount;

		public Demo(byte[] data)
		{
			this.p = 0;

			if (data[this.p++] != 109)
			{
				throw new Exception("Demo is from a different game version!");
			}

			this.data = data;

			this.options = new GameOptions();
			this.options.Skill = (GameSkill) data[this.p++];
			this.options.Episode = data[this.p++];
			this.options.Map = data[this.p++];
			this.options.Deathmatch = data[this.p++];
			this.options.RespawnMonsters = data[this.p++] != 0;
			this.options.FastMonsters = data[this.p++] != 0;
			this.options.NoMonsters = data[this.p++] != 0;
			this.options.ConsolePlayer = data[this.p++];

			this.options.Players[0].InGame = data[this.p++] != 0;
			this.options.Players[1].InGame = data[this.p++] != 0;
			this.options.Players[2].InGame = data[this.p++] != 0;
			this.options.Players[3].InGame = data[this.p++] != 0;

			this.options.DemoPlayback = true;

			this.playerCount = 0;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (this.options.Players[i].InGame)
				{
					this.playerCount++;
				}
			}

			if (this.playerCount >= 2)
			{
				this.options.NetGame = true;
			}
		}

		public bool ReadCmd(TicCmd[] cmds)
		{
			if (this.p == this.data.Length)
			{
				return false;
			}

			if (this.data[this.p] == 0x80)
			{
				return false;
			}

			if (this.p + 4 * this.playerCount > this.data.Length)
			{
				return false;
			}

			var players = this.options.Players;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame)
				{
					var cmd = cmds[i];
					cmd.ForwardMove = (sbyte) this.data[this.p++];
					cmd.SideMove = (sbyte) this.data[this.p++];
					cmd.AngleTurn = (short) (this.data[this.p++] << 8);
					cmd.Buttons = this.data[this.p++];
				}
			}

			return true;
		}

		public GameOptions Options => this.options;
	}
}
