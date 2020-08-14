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
	using System.IO;

	public sealed class Demo
	{
		private BinaryReader reader;

		private GameOptions options;

		private int playerCount;

		public Demo(Stream stream)
		{
			var reader = new BinaryReader(stream);

			if (reader.ReadByte() != 109)
			{
				throw new Exception("Demo is from a different game version!");
			}

			this.reader = reader;

			this.options = new GameOptions();
			this.options.Skill = (GameSkill) reader.ReadByte();
			this.options.Episode = reader.ReadByte();
			this.options.Map = reader.ReadByte();
			this.options.Deathmatch = reader.ReadByte();
			this.options.RespawnMonsters = reader.ReadByte() != 0;
			this.options.FastMonsters = reader.ReadByte() != 0;
			this.options.NoMonsters = reader.ReadByte() != 0;
			this.options.ConsolePlayer = reader.ReadByte();

			this.options.Players[0].InGame = reader.ReadByte() != 0;
			this.options.Players[1].InGame = reader.ReadByte() != 0;
			this.options.Players[2].InGame = reader.ReadByte() != 0;
			this.options.Players[3].InGame = reader.ReadByte() != 0;

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
			if (this.reader.BaseStream.Position == this.reader.BaseStream.Length)
			{
				return false;
			}

			var test = this.reader.ReadByte();
			this.reader.BaseStream.Position--;

			if (test == 0x80)
			{
				return false;
			}

			if (this.reader.BaseStream.Position + 4 * this.playerCount > this.reader.BaseStream.Length)
			{
				return false;
			}

			var players = this.options.Players;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (players[i].InGame)
				{
					var cmd = cmds[i];
					cmd.ForwardMove = (sbyte) this.reader.ReadByte();
					cmd.SideMove = (sbyte) this.reader.ReadByte();
					cmd.AngleTurn = (short) (this.reader.ReadByte() << 8);
					cmd.Buttons = this.reader.ReadByte();
				}
			}

			return true;
		}

		public GameOptions Options => this.options;
	}
}
