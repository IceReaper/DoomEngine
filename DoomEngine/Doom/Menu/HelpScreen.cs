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
	using UserInput;

	public sealed class HelpScreen : MenuDef
	{
		private int pageCount;

		private int page;

		public HelpScreen(DoomMenu menu)
			: base(menu)
		{
			if (DoomApplication.Instance.IWad == "doom1")
			{
				this.pageCount = 2;
			}
			else
			{
				this.pageCount = 1;
			}
		}

		public override void Open()
		{
			this.page = this.pageCount - 1;
		}

		public override bool DoEvent(DoomEvent e)
		{
			if (e.Type != EventType.KeyDown)
			{
				return true;
			}

			if (e.Key == DoomKey.Enter || e.Key == DoomKey.Space || e.Key == DoomKey.LControl || e.Key == DoomKey.RControl)
			{
				this.page--;

				if (this.page == -1)
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

		public int Page => this.page;
	}
}
