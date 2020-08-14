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

namespace DoomEngine.Doom.Common
{
	using Graphics;
	using System;
	using System.Runtime.ExceptionServices;

	public sealed class CommonResource
	{
		private Palette palette;
		private ColorMap colorMap;
		private TextureLookup textures;
		private FlatLookup flats;
		private SpriteLookup sprites;
		private TextureAnimation animation;

		public CommonResource()
		{
			try
			{
				this.palette = new Palette();
				this.colorMap = new ColorMap();
				this.textures = new TextureLookup();
				this.flats = new FlatLookup();
				this.sprites = new SpriteLookup();
				this.animation = new TextureAnimation(this.textures, this.flats);
			}
			catch (Exception e)
			{
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public void Dispose()
		{
		}

		public Palette Palette => this.palette;
		public ColorMap ColorMap => this.colorMap;
		public TextureLookup Textures => this.textures;
		public FlatLookup Flats => this.flats;
		public SpriteLookup Sprites => this.sprites;
		public TextureAnimation Animation => this.animation;
	}
}
