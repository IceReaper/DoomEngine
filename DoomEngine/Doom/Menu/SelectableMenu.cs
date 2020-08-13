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
	using System.Collections.Generic;
	using UserInput;

	public sealed class SelectableMenu : MenuDef
	{
		private string[] name;
		private int[] titleX;
		private int[] titleY;
		private MenuItem[] items;

		private int index;
		private MenuItem choice;

		private TextInput textInput;

		public SelectableMenu(DoomMenu menu, string name, int titleX, int titleY, int firstChoice, params MenuItem[] items)
			: base(menu)
		{
			this.name = new[] {name};
			this.titleX = new[] {titleX};
			this.titleY = new[] {titleY};
			this.items = items;

			this.index = firstChoice;
			this.choice = items[this.index];
		}

		public SelectableMenu(
			DoomMenu menu,
			string name1,
			int titleX1,
			int titleY1,
			string name2,
			int titleX2,
			int titleY2,
			int firstChoice,
			params MenuItem[] items
		)
			: base(menu)
		{
			this.name = new[] {name1, name2};
			this.titleX = new[] {titleX1, titleX2};
			this.titleY = new[] {titleY1, titleY2};
			this.items = items;

			this.index = firstChoice;
			this.choice = items[this.index];
		}

		public override void Open()
		{
			foreach (var item in this.items)
			{
				var toggle = item as ToggleMenuItem;

				if (toggle != null)
				{
					toggle.Reset();
				}

				var slider = item as SliderMenuItem;

				if (slider != null)
				{
					slider.Reset();
				}
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

			if (e.Key == DoomKey.Left)
			{
				var toggle = this.choice as ToggleMenuItem;

				if (toggle != null)
				{
					toggle.Down();
					this.Menu.StartSound(Sfx.PISTOL);
				}

				var slider = this.choice as SliderMenuItem;

				if (slider != null)
				{
					slider.Down();
					this.Menu.StartSound(Sfx.STNMOV);
				}
			}

			if (e.Key == DoomKey.Right)
			{
				var toggle = this.choice as ToggleMenuItem;

				if (toggle != null)
				{
					toggle.Up();
					this.Menu.StartSound(Sfx.PISTOL);
				}

				var slider = this.choice as SliderMenuItem;

				if (slider != null)
				{
					slider.Up();
					this.Menu.StartSound(Sfx.STNMOV);
				}
			}

			if (e.Key == DoomKey.Enter)
			{
				var toggle = this.choice as ToggleMenuItem;

				if (toggle != null)
				{
					toggle.Up();
					this.Menu.StartSound(Sfx.PISTOL);
				}

				var simple = this.choice as SimpleMenuItem;

				if (simple != null)
				{
					if (simple.Selectable)
					{
						if (simple.Action != null)
						{
							simple.Action();
						}

						if (simple.Next != null)
						{
							this.Menu.SetCurrent(simple.Next);
						}
						else
						{
							this.Menu.Close();
						}
					}

					this.Menu.StartSound(Sfx.PISTOL);

					return true;
				}

				if (this.choice.Next != null)
				{
					this.Menu.SetCurrent(this.choice.Next);
					this.Menu.StartSound(Sfx.PISTOL);
				}
			}

			if (e.Key == DoomKey.Escape)
			{
				this.Menu.Close();
				this.Menu.StartSound(Sfx.SWTCHX);
			}

			return true;
		}

		public IReadOnlyList<string> Name => this.name;
		public IReadOnlyList<int> TitleX => this.titleX;
		public IReadOnlyList<int> TitleY => this.titleY;
		public IReadOnlyList<MenuItem> Items => this.items;
		public MenuItem Choice => this.choice;
	}
}
