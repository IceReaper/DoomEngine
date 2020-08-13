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
	using System;

	public class ToggleMenuItem : MenuItem
	{
		private string name;
		private int itemX;
		private int itemY;

		private string[] states;
		private int stateX;

		private int stateNumber;

		private Func<int> reset;
		private Action<int> action;

		public ToggleMenuItem(
			string name,
			int skullX,
			int skullY,
			int itemX,
			int itemY,
			string state1,
			string state2,
			int stateX,
			Func<int> reset,
			Action<int> action
		)
			: base(skullX, skullY, null)
		{
			this.name = name;
			this.itemX = itemX;
			this.itemY = itemY;

			this.states = new[] {state1, state2};
			this.stateX = stateX;

			this.stateNumber = 0;

			this.action = action;
			this.reset = reset;
		}

		public void Reset()
		{
			if (this.reset != null)
			{
				this.stateNumber = this.reset();
			}
		}

		public void Up()
		{
			this.stateNumber++;

			if (this.stateNumber == this.states.Length)
			{
				this.stateNumber = 0;
			}

			if (this.action != null)
			{
				this.action(this.stateNumber);
			}
		}

		public void Down()
		{
			this.stateNumber--;

			if (this.stateNumber == -1)
			{
				this.stateNumber = this.states.Length - 1;
			}

			if (this.action != null)
			{
				this.action(this.stateNumber);
			}
		}

		public string Name => this.name;
		public int ItemX => this.itemX;
		public int ItemY => this.itemY;

		public string State => this.states[this.stateNumber];
		public int StateX => this.stateX;
	}
}
