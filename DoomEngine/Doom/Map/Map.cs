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

namespace DoomEngine.Doom.Map
{
	using Audio;
	using Common;
	using Game;
	using Graphics;
	using Info;
	using Math;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using World;

	public sealed class Map
	{
		private TextureLookup textures;
		private FlatLookup flats;
		private TextureAnimation animation;

		private World world;

		private Vertex[] vertices;
		private Sector[] sectors;
		private SideDef[] sides;
		private LineDef[] lines;
		private Seg[] segs;
		private Subsector[] subsectors;
		private Node[] nodes;
		private MapThing[] things;
		private BlockMap blockMap;
		private Reject reject;

		private Texture skyTexture;

		private string title;

		public Map(CommonResource resorces, World world)
			: this(resorces.Textures, resorces.Flats, resorces.Animation, world)
		{
		}

		public Map(TextureLookup textures, FlatLookup flats, TextureAnimation animation, World world)
		{
			try
			{
				this.textures = textures;
				this.flats = flats;
				this.animation = animation;
				this.world = world;

				var options = world.Options;

				string name;

				if (DoomApplication.Instance.Resource.Wad.Names.Contains("doom2")
					|| DoomApplication.Instance.Resource.Wad.Names.Contains("plutonia")
					|| DoomApplication.Instance.Resource.Wad.Names.Contains("tnt"))
				{
					name = "MAP" + options.Map.ToString("00");
				}
				else
				{
					name = "E" + options.Episode + "M" + options.Map;
				}

				Console.Write("Load map '" + name + "': ");

				var map = $"MAPS/{name}/";

				if (!DoomApplication.Instance.FileSystem.Files().Any(file => file.StartsWith(map)))
				{
					throw new Exception("Map '" + name + "' was not found!");
				}

				this.vertices = Vertex.FromWad($"{map}VERTEXES");
				this.sectors = Sector.FromWad($"{map}SECTORS", flats);
				this.sides = SideDef.FromWad($"{map}SIDEDEFS", textures, this.sectors);
				this.lines = LineDef.FromWad($"{map}LINEDEFS", this.vertices, this.sides);
				this.segs = Seg.FromWad($"{map}SEGS", this.vertices, this.lines);
				this.subsectors = Subsector.FromWad($"{map}SSECTORS", this.segs);
				this.nodes = Node.FromWad($"{map}NODES", this.subsectors);
				this.things = MapThing.FromWad($"{map}THINGS");
				this.blockMap = BlockMap.FromWad($"{map}BLOCKMAP", this.lines);
				this.reject = Reject.FromWad($"{map}REJECT", this.sectors);

				this.GroupLines();

				this.skyTexture = this.GetSkyTextureByMapName(name);

				if (DoomApplication.Instance.Resource.Wad.Names.Contains("doom2")
					|| DoomApplication.Instance.Resource.Wad.Names.Contains("plutonia")
					|| DoomApplication.Instance.Resource.Wad.Names.Contains("tnt"))
				{
					if (DoomApplication.Instance.Resource.Wad.Names.Contains("plutonia"))
					{
						this.title = DoomInfo.MapTitles.Plutonia[options.Map - 1];
					}
					else if (DoomApplication.Instance.Resource.Wad.Names.Contains("tnt"))
					{
						this.title = DoomInfo.MapTitles.Tnt[options.Map - 1];
					}
					else
					{
						this.title = DoomInfo.MapTitles.Doom2[options.Map - 1];
					}
				}
				else
				{
					this.title = DoomInfo.MapTitles.Doom[options.Episode - 1][options.Map - 1];
				}

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private void GroupLines()
		{
			var sectorLines = new List<LineDef>();
			var boundingBox = new Fixed[4];

			foreach (var line in this.lines)
			{
				if (line.Special != 0)
				{
					var so = new Mobj(this.world);
					so.X = (line.Vertex1.X + line.Vertex2.X) / 2;
					so.Y = (line.Vertex1.Y + line.Vertex2.Y) / 2;
					line.SoundOrigin = so;
				}
			}

			foreach (var sector in this.sectors)
			{
				sectorLines.Clear();
				Box.Clear(boundingBox);

				foreach (var line in this.lines)
				{
					if (line.FrontSector == sector || line.BackSector == sector)
					{
						sectorLines.Add(line);
						Box.AddPoint(boundingBox, line.Vertex1.X, line.Vertex1.Y);
						Box.AddPoint(boundingBox, line.Vertex2.X, line.Vertex2.Y);
					}
				}

				sector.Lines = sectorLines.ToArray();

				// Set the degenmobj_t to the middle of the bounding box.
				sector.SoundOrigin = new Mobj(this.world);
				sector.SoundOrigin.X = (boundingBox[Box.Right] + boundingBox[Box.Left]) / 2;
				sector.SoundOrigin.Y = (boundingBox[Box.Top] + boundingBox[Box.Bottom]) / 2;

				sector.BlockBox = new int[4];
				int block;

				// Adjust bounding box to map blocks.
				block = (boundingBox[Box.Top] - this.blockMap.OriginY + GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
				block = block >= this.blockMap.Height ? this.blockMap.Height - 1 : block;
				sector.BlockBox[Box.Top] = block;

				block = (boundingBox[Box.Bottom] - this.blockMap.OriginY - GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
				block = block < 0 ? 0 : block;
				sector.BlockBox[Box.Bottom] = block;

				block = (boundingBox[Box.Right] - this.blockMap.OriginX + GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
				block = block >= this.blockMap.Width ? this.blockMap.Width - 1 : block;
				sector.BlockBox[Box.Right] = block;

				block = (boundingBox[Box.Left] - this.blockMap.OriginX - GameConst.MaxThingRadius).Data >> BlockMap.FracToBlockShift;
				block = block < 0 ? 0 : block;
				sector.BlockBox[Box.Left] = block;
			}
		}

		private Texture GetSkyTextureByMapName(string name)
		{
			if (name.Length == 4)
			{
				switch (name[1])
				{
					case '1':
						return this.textures["SKY1"];

					case '2':
						return this.textures["SKY2"];

					case '3':
						return this.textures["SKY3"];

					default:
						return this.textures["SKY4"];
				}
			}
			else
			{
				var number = int.Parse(name.Substring(3));

				if (number <= 11)
				{
					return this.textures["SKY1"];
				}
				else if (number <= 21)
				{
					return this.textures["SKY2"];
				}
				else
				{
					return this.textures["SKY3"];
				}
			}
		}

		public TextureLookup Textures => this.textures;
		public FlatLookup Flats => this.flats;
		public TextureAnimation Animation => this.animation;

		public Vertex[] Vertices => this.vertices;
		public Sector[] Sectors => this.sectors;
		public SideDef[] Sides => this.sides;
		public LineDef[] Lines => this.lines;
		public Seg[] Segs => this.segs;
		public Subsector[] Subsectors => this.subsectors;
		public Node[] Nodes => this.nodes;
		public MapThing[] Things => this.things;
		public BlockMap BlockMap => this.blockMap;
		public Reject Reject => this.reject;
		public Texture SkyTexture => this.skyTexture;
		public int SkyFlatNumber => this.flats.SkyFlatNumber;
		public string Title => this.title;

		private static readonly Bgm[] e4BgmList = new Bgm[]
		{
			Bgm.E3M4, // American   e4m1
			Bgm.E3M2, // Romero     e4m2
			Bgm.E3M3, // Shawn      e4m3
			Bgm.E1M5, // American   e4m4
			Bgm.E2M7, // Tim        e4m5
			Bgm.E2M4, // Romero     e4m6
			Bgm.E2M6, // J.Anderson e4m7 CHIRON.WAD
			Bgm.E2M5, // Shawn      e4m8
			Bgm.E1M9 // Tim        e4m9
		};

		public static Bgm GetMapBgm(GameOptions options)
		{
			Bgm bgm;

			if (DoomApplication.Instance.Resource.Wad.Names.Contains("doom2")
				|| DoomApplication.Instance.Resource.Wad.Names.Contains("plutonia")
				|| DoomApplication.Instance.Resource.Wad.Names.Contains("tnt"))
			{
				bgm = Bgm.RUNNIN + options.Map - 1;
			}
			else
			{
				if (options.Episode < 4)
				{
					bgm = Bgm.E1M1 + (options.Episode - 1) * 9 + options.Map - 1;
				}
				else
				{
					bgm = Map.e4BgmList[options.Map - 1];
				}
			}

			return bgm;
		}
	}
}
