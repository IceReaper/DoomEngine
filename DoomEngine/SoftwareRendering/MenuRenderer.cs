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

namespace DoomEngine.SoftwareRendering
{
	using Doom.Graphics;
	using Doom.Menu;
	using System.Collections.Generic;

	public sealed class MenuRenderer
	{
		private static readonly char[] cursor = {'_'};

		private DrawScreen screen;

		private PatchCache cache;

		public MenuRenderer(DrawScreen screen)
		{
			this.screen = screen;

			this.cache = new PatchCache();
		}

		public void Render(DoomMenu menu)
		{
			var selectable = menu.Current as SelectableMenu;

			if (selectable != null)
			{
				this.DrawSelectableMenu(selectable);
			}

			var save = menu.Current as SaveMenu;

			if (save != null)
			{
				this.DrawSaveMenu(save);
			}

			var load = menu.Current as LoadMenu;

			if (load != null)
			{
				this.DrawLoadMenu(load);
			}

			var yesNo = menu.Current as YesNoConfirm;

			if (yesNo != null)
			{
				this.DrawText(yesNo.Text);
			}

			var pressAnyKey = menu.Current as PressAnyKey;

			if (pressAnyKey != null)
			{
				this.DrawText(pressAnyKey.Text);
			}

			var quit = menu.Current as QuitConfirm;

			if (quit != null)
			{
				this.DrawText(quit.Text);
			}

			var help = menu.Current as HelpScreen;

			if (help != null)
			{
				this.DrawHelp(help);
			}
		}

		private void DrawSelectableMenu(SelectableMenu selectable)
		{
			for (var i = 0; i < selectable.Name.Count; i++)
			{
				this.DrawMenuPatch(selectable.Name[i], selectable.TitleX[i], selectable.TitleY[i]);
			}

			foreach (var item in selectable.Items)
			{
				this.DrawMenuItem(selectable.Menu, item);
			}

			var choice = selectable.Choice;
			var skull = selectable.Menu.Tics / 8 % 2 == 0 ? "M_SKULL1" : "M_SKULL2";
			this.DrawMenuPatch(skull, choice.SkullX, choice.SkullY);
		}

		private void DrawSaveMenu(SaveMenu save)
		{
			for (var i = 0; i < save.Name.Count; i++)
			{
				this.DrawMenuPatch(save.Name[i], save.TitleX[i], save.TitleY[i]);
			}

			foreach (var item in save.Items)
			{
				this.DrawMenuItem(save.Menu, item);
			}

			var choice = save.Choice;
			var skull = save.Menu.Tics / 8 % 2 == 0 ? "M_SKULL1" : "M_SKULL2";
			this.DrawMenuPatch(skull, choice.SkullX, choice.SkullY);
		}

		private void DrawLoadMenu(LoadMenu load)
		{
			for (var i = 0; i < load.Name.Count; i++)
			{
				this.DrawMenuPatch(load.Name[i], load.TitleX[i], load.TitleY[i]);
			}

			foreach (var item in load.Items)
			{
				this.DrawMenuItem(load.Menu, item);
			}

			var choice = load.Choice;
			var skull = load.Menu.Tics / 8 % 2 == 0 ? "M_SKULL1" : "M_SKULL2";
			this.DrawMenuPatch(skull, choice.SkullX, choice.SkullY);
		}

		private void DrawMenuItem(DoomMenu menu, MenuItem item)
		{
			var simple = item as SimpleMenuItem;

			if (simple != null)
			{
				this.DrawSimpleMenuItem(simple);
			}

			var toggle = item as ToggleMenuItem;

			if (toggle != null)
			{
				this.DrawToggleMenuItem(toggle);
			}

			var slider = item as SliderMenuItem;

			if (slider != null)
			{
				this.DrawSliderMenuItem(slider);
			}

			var textBox = item as TextBoxMenuItem;

			if (textBox != null)
			{
				this.DrawTextBoxMenuItem(textBox, menu.Tics);
			}
		}

		private void DrawMenuPatch(string name, int x, int y)
		{
			var scale = this.screen.Width / 320;
			this.screen.DrawPatch(this.cache[name], scale * x, scale * y, scale);
		}

		private void DrawMenuText(IReadOnlyList<char> text, int x, int y)
		{
			var scale = this.screen.Width / 320;
			this.screen.DrawText(text, scale * x, scale * y, scale);
		}

		private void DrawSimpleMenuItem(SimpleMenuItem item)
		{
			this.DrawMenuPatch(item.Name, item.ItemX, item.ItemY);
		}

		private void DrawToggleMenuItem(ToggleMenuItem item)
		{
			this.DrawMenuPatch(item.Name, item.ItemX, item.ItemY);
			this.DrawMenuPatch(item.State, item.StateX, item.ItemY);
		}

		private void DrawSliderMenuItem(SliderMenuItem item)
		{
			this.DrawMenuPatch(item.Name, item.ItemX, item.ItemY);

			this.DrawMenuPatch("M_THERML", item.SliderX, item.SliderY);

			for (var i = 0; i < item.SliderLength; i++)
			{
				var x = item.SliderX + 8 * (1 + i);
				this.DrawMenuPatch("M_THERMM", x, item.SliderY);
			}

			var end = item.SliderX + 8 * (1 + item.SliderLength);
			this.DrawMenuPatch("M_THERMR", end, item.SliderY);

			var pos = item.SliderX + 8 * (1 + item.SliderPosition);
			this.DrawMenuPatch("M_THERMO", pos, item.SliderY);
		}

		private char[] emptyText = "EMPTY SLOT".ToCharArray();

		private void DrawTextBoxMenuItem(TextBoxMenuItem item, int tics)
		{
			var length = 24;
			this.DrawMenuPatch("M_LSLEFT", item.ItemX, item.ItemY);

			for (var i = 0; i < length; i++)
			{
				var x = item.ItemX + 8 * (1 + i);
				this.DrawMenuPatch("M_LSCNTR", x, item.ItemY);
			}

			this.DrawMenuPatch("M_LSRGHT", item.ItemX + 8 * (1 + length), item.ItemY);

			if (!item.Editing)
			{
				var text = item.Text != null ? item.Text : this.emptyText;
				this.DrawMenuText(text, item.ItemX + 8, item.ItemY);
			}
			else
			{
				this.DrawMenuText(item.Text, item.ItemX + 8, item.ItemY);

				if (tics / 3 % 2 == 0)
				{
					var textWidth = this.screen.MeasureText(item.Text, 1);
					this.DrawMenuText(MenuRenderer.cursor, item.ItemX + 8 + textWidth, item.ItemY);
				}
			}
		}

		private void DrawText(IReadOnlyList<string> text)
		{
			var scale = this.screen.Width / 320;
			var height = 7 * scale * text.Count;

			for (var i = 0; i < text.Count; i++)
			{
				var x = (this.screen.Width - this.screen.MeasureText(text[i], scale)) / 2;
				var y = (this.screen.Height - height) / 2 + 7 * scale * (i + 1);
				this.screen.DrawText(text[i], x, y, scale);
			}
		}

		private void DrawHelp(HelpScreen help)
		{
			var skull = help.Menu.Tics / 8 % 2 == 0 ? "M_SKULL1" : "M_SKULL2";

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				this.DrawMenuPatch("HELP", 0, 0);
				this.DrawMenuPatch(skull, 298, 160);
			}
			else
			{
				if (help.Page == 0)
				{
					this.DrawMenuPatch("HELP1", 0, 0);
					this.DrawMenuPatch(skull, 298, 170);
				}
				else
				{
					this.DrawMenuPatch("HELP2", 0, 0);
					this.DrawMenuPatch(skull, 248, 180);
				}
			}
		}
	}
}
