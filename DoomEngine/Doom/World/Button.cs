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

namespace DoomEngine.Doom.World
{
	using Map;

	public sealed class Button
	{
		private LineDef line;
		private ButtonPosition position;
		private int texture;
		private int timer;
		private Mobj soundOrigin;

		public void Clear()
		{
			this.line = null;
			this.position = 0;
			this.texture = 0;
			this.timer = 0;
			this.soundOrigin = null;
		}

		public LineDef Line
		{
			get => this.line;
			set => this.line = value;
		}

		public ButtonPosition Position
		{
			get => this.position;
			set => this.position = value;
		}

		public int Texture
		{
			get => this.texture;
			set => this.texture = value;
		}

		public int Timer
		{
			get => this.timer;
			set => this.timer = value;
		}

		public Mobj SoundOrigin
		{
			get => this.soundOrigin;
			set => this.soundOrigin = value;
		}
	}
}
