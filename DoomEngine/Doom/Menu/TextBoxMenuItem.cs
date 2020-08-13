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
	using System.Collections.Generic;

	public class TextBoxMenuItem : MenuItem
	{
		private int itemX;
		private int itemY;

		private IReadOnlyList<char> text;
		private TextInput edit;

		public TextBoxMenuItem(int skullX, int skullY, int itemX, int itemY)
			: base(skullX, skullY, null)
		{
			this.itemX = itemX;
			this.itemY = itemY;
		}

		public TextInput Edit(Action finished)
		{
			this.edit = new TextInput(
				this.text != null ? this.text : new char[0],
				cs =>
				{
				},
				cs =>
				{
					this.text = cs;
					this.edit = null;
					finished();
				},
				() =>
				{
					this.edit = null;
				}
			);

			return this.edit;
		}

		public void SetText(string text)
		{
			if (text != null)
			{
				this.text = text.ToCharArray();
			}
		}

		public IReadOnlyList<char> Text
		{
			get
			{
				if (this.edit == null)
				{
					return this.text;
				}
				else
				{
					return this.edit.Text;
				}
			}
		}

		public int ItemX => this.itemX;
		public int ItemY => this.itemY;
		public bool Editing => this.edit != null;
	}
}
