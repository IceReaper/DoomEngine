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
	using Common;
	using Event;
	using Info;
	using System;
	using System.Collections.Generic;
	using UserInput;

	public sealed class QuitConfirm : MenuDef
	{
		private static readonly Sfx[] doomQuitSoundList = new Sfx[]
		{
			Sfx.PLDETH, Sfx.DMPAIN, Sfx.POPAIN, Sfx.SLOP, Sfx.TELEPT, Sfx.POSIT1, Sfx.POSIT3, Sfx.SGTATK
		};

		private static readonly Sfx[] doom2QuitSoundList = new Sfx[]
		{
			Sfx.VILACT, Sfx.GETPOW, Sfx.BOSCUB, Sfx.SLOP, Sfx.SKESWG, Sfx.KNTDTH, Sfx.BSPACT, Sfx.SGTATK
		};

		private DoomApplication app;
		private DoomRandom random;
		private string[] text;

		private int endCount;

		public QuitConfirm(DoomMenu menu, DoomApplication app)
			: base(menu)
		{
			this.app = app;
			this.random = new DoomRandom(DateTime.Now.Millisecond);
			this.endCount = -1;
		}

		public override void Open()
		{
			IReadOnlyList<DoomString> list;

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				if (DoomApplication.Instance.IWad == "doom2" || DoomApplication.Instance.IWad == "freedoom2")
				{
					list = DoomInfo.QuitMessages.Doom2;
				}
				else
				{
					list = DoomInfo.QuitMessages.FinalDoom;
				}
			}
			else
			{
				list = DoomInfo.QuitMessages.Doom;
			}

			this.text = (list[this.random.Next() % list.Count] + "\n\n" + DoomInfo.Strings.PRESSYN).Split('\n');
		}

		public override bool DoEvent(DoomEvent e)
		{
			if (this.endCount != -1)
			{
				return true;
			}

			if (e.Type != EventType.KeyDown)
			{
				return true;
			}

			if (e.Key == DoomKey.Y || e.Key == DoomKey.Enter || e.Key == DoomKey.Space)
			{
				this.endCount = 0;

				Sfx sfx;

				if (DoomApplication.Instance.IWad == "doom2"
					|| DoomApplication.Instance.IWad == "freedoom2"
					|| DoomApplication.Instance.IWad == "plutonia"
					|| DoomApplication.Instance.IWad == "tnt")
				{
					sfx = QuitConfirm.doom2QuitSoundList[this.random.Next() % QuitConfirm.doom2QuitSoundList.Length];
				}
				else
				{
					sfx = QuitConfirm.doomQuitSoundList[this.random.Next() % QuitConfirm.doomQuitSoundList.Length];
				}

				this.Menu.StartSound(sfx);
			}

			if (e.Key == DoomKey.N || e.Key == DoomKey.Escape)
			{
				this.Menu.Close();
				this.Menu.StartSound(Sfx.SWTCHX);
			}

			return true;
		}

		public override void Update()
		{
			if (this.endCount != -1)
			{
				this.endCount++;
			}

			if (this.endCount == 50)
			{
				this.app.Quit();
			}
		}

		public IReadOnlyList<string> Text => this.text;
	}
}
