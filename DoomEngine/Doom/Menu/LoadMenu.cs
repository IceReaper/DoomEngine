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

	public sealed class LoadMenu : MenuDef
    {
        private string[] name;
        private int[] titleX;
        private int[] titleY;
        private TextBoxMenuItem[] items;

        private int index;
        private TextBoxMenuItem choice;

        public LoadMenu(
            DoomMenu menu,
            string name, int titleX, int titleY,
            int firstChoice,
            params TextBoxMenuItem[] items) : base(menu)
        {
            this.name = new[] { name };
            this.titleX = new[] { titleX };
            this.titleY = new[] { titleY };
            this.items = items;

            this.index = firstChoice;
            this.choice = items[this.index];
        }

        public override void Open()
        {
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
                if (this.DoLoad(this.index))
                {
                    this.Menu.Close();
                }
                this.Menu.StartSound(Sfx.PISTOL);
            }

            if (e.Key == DoomKey.Escape)
            {
                this.Menu.Close();
                this.Menu.StartSound(Sfx.SWTCHX);
            }

            return true;
        }

        public bool DoLoad(int slotNumber)
        {
            if (this.Menu.SaveSlots[slotNumber] != null)
            {
                this.Menu.Application.LoadGame(slotNumber);
                return true;
            }
            else
            {
                return false;
            }
        }

        public IReadOnlyList<string> Name => this.name;
        public IReadOnlyList<int> TitleX => this.titleX;
        public IReadOnlyList<int> TitleY => this.titleY;
        public IReadOnlyList<MenuItem> Items => this.items;
        public MenuItem Choice => this.choice;
    }
}
