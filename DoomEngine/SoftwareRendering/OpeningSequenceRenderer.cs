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
	using Doom;
	using Doom.Graphics;
	using Doom.Wad;
	using Platform;

	public class OpeningSequenceRenderer
	{
		private DrawScreen screen;
		private IRenderer parent;

		private PatchCache cache;

		public OpeningSequenceRenderer(Wad wad, DrawScreen screen, IRenderer parent)
		{
			this.screen = screen;
			this.parent = parent;

			this.cache = new PatchCache(wad);
		}

		public void Render(OpeningSequence sequence)
		{
			var scale = this.screen.Width / 320;

			switch (sequence.State)
			{
				case OpeningSequenceState.Title:
					this.screen.DrawPatch(this.cache["TITLEPIC"], 0, 0, scale);

					break;

				case OpeningSequenceState.Demo:
					this.parent.RenderGame(sequence.DemoGame);

					break;

				case OpeningSequenceState.Credit:
					this.screen.DrawPatch(this.cache["CREDIT"], 0, 0, scale);

					break;
			}
		}
	}
}
