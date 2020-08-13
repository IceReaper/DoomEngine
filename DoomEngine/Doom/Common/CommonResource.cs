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
	using Wad;

	public sealed class CommonResource : IDisposable
	{
		private Wad wad;
		private Palette palette;
		private ColorMap colorMap;
		private TextureLookup textures;
		private FlatLookup flats;
		private SpriteLookup sprites;
		private TextureAnimation animation;

		private CommonResource()
		{
		}

		public CommonResource(params string[] wadPaths)
		{
			try
			{
				this.wad = new Wad(wadPaths);
				this.palette = new Palette(this.wad);
				this.colorMap = new ColorMap(this.wad);
				this.textures = new TextureLookup(this.wad);
				this.flats = new FlatLookup(this.wad);
				this.sprites = new SpriteLookup(this.wad);
				this.animation = new TextureAnimation(this.textures, this.flats);
			}
			catch (Exception e)
			{
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public static CommonResource CreateDummy(params string[] wadPaths)
		{
			var resource = new CommonResource();
			resource.wad = new Wad(wadPaths);
			resource.palette = new Palette(resource.wad);
			resource.colorMap = new ColorMap(resource.wad);
			resource.textures = new TextureLookup(resource.wad, true);
			resource.flats = new FlatLookup(resource.wad, true);
			resource.sprites = new SpriteLookup(resource.wad, true);
			resource.animation = new TextureAnimation(resource.textures, resource.flats);

			return resource;
		}

		public void Dispose()
		{
			if (this.wad != null)
			{
				this.wad.Dispose();
				this.wad = null;
			}
		}

		public Wad Wad => this.wad;
		public Palette Palette => this.palette;
		public ColorMap ColorMap => this.colorMap;
		public TextureLookup Textures => this.textures;
		public FlatLookup Flats => this.flats;
		public SpriteLookup Sprites => this.sprites;
		public TextureAnimation Animation => this.animation;
	}
}
