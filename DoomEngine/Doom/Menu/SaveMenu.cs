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
	using System.Collections.Generic;
	using System.Linq;
	using UserInput;

	public sealed class SaveMenu : MenuDef
	{
		private string[] name;
		private int[] titleX;
		private int[] titleY;
		private TextBoxMenuItem[] items;

		private int index;
		private TextBoxMenuItem choice;

		private TextInput textInput;

		private int lastSaveSlot;

		public SaveMenu(DoomMenu menu, string name, int titleX, int titleY, int firstChoice, params TextBoxMenuItem[] items)
			: base(menu)
		{
			this.name = new[] {name};
			this.titleX = new[] {titleX};
			this.titleY = new[] {titleY};
			this.items = items;

			this.index = firstChoice;
			this.choice = items[this.index];

			this.lastSaveSlot = -1;
		}

		public override void Open()
		{
			if (this.Menu.Application.State != ApplicationState.Game || this.Menu.Application.Game.State != GameState.Level)
			{
				this.Menu.NotifySaveFailed();

				return;
			}

			for (var i = 0; i < this.items.Length; i++)
			{
				this.items[i].SetText(this.Menu.SaveSlots[i]);
			}
		}

		private void Up()
		{
			this.index--;

			if (this.index < 0)
			{
				this.index = this.items.Length - 1;
			}

			this.choice = this.items[this.index];
		}

		private void Down()
		{
			this.index++;

			if (this.index >= this.items.Length)
			{
				this.index = 0;
			}

			this.choice = this.items[this.index];
		}

		public override bool DoEvent(DoomEvent e)
		{
			if (e.Type != EventType.KeyDown)
			{
				return true;
			}

			if (this.textInput != null)
			{
				var result = this.textInput.DoEvent(e);

				if (this.textInput.State == TextInputState.Canceled)
				{
					this.textInput = null;
				}
				else if (this.textInput.State == TextInputState.Finished)
				{
					this.textInput = null;
				}

				if (result)
				{
					return true;
				}
			}

			if (e.Key == DoomKey.Up)
			{
				this.Up();
				this.Menu.StartSound(Sfx.PSTOP);
			}

			if (e.Key == DoomKey.Down)
			{
				this.Down();
				this.Menu.StartSound(Sfx.PSTOP);
			}

			if (e.Key == DoomKey.Enter)
			{
				this.textInput = this.choice.Edit(() => this.DoSave(this.index));
				this.Menu.StartSound(Sfx.PISTOL);
			}

			if (e.Key == DoomKey.Escape)
			{
				this.Menu.Close();
				this.Menu.StartSound(Sfx.SWTCHX);
			}

			return true;
		}

		public void DoSave(int slotNumber)
		{
			this.Menu.SaveSlots[slotNumber] = new string(this.items[slotNumber].Text.ToArray());

			if (this.Menu.Application.SaveGame(slotNumber, this.Menu.SaveSlots[slotNumber]))
			{
				this.Menu.Close();
				this.lastSaveSlot = slotNumber;
			}
			else
			{
				this.Menu.NotifySaveFailed();
			}

			this.Menu.StartSound(Sfx.PISTOL);
		}

		public IReadOnlyList<string> Name => this.name;
		public IReadOnlyList<int> TitleX => this.titleX;
		public IReadOnlyList<int> TitleY => this.titleY;
		public IReadOnlyList<MenuItem> Items => this.items;
		public MenuItem Choice => this.choice;
		public int LastSaveSlot => this.lastSaveSlot;
	}
}
