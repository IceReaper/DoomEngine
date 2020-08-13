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
	using Math;
	using System;
	using Wad;
	using World;

	public sealed class BlockMap
	{
		public static readonly int IntBlockSize = 128;
		public static readonly Fixed BlockSize = Fixed.FromInt(BlockMap.IntBlockSize);
		public static readonly int BlockMask = BlockMap.BlockSize.Data - 1;
		public static readonly int FracToBlockShift = Fixed.FracBits + 7;
		public static readonly int BlockToFracShift = BlockMap.FracToBlockShift - Fixed.FracBits;

		private Fixed originX;
		private Fixed originY;

		private int width;
		private int height;

		private short[] table;

		private LineDef[] lines;

		private Mobj[] thingLists;

		private BlockMap(Fixed originX, Fixed originY, int width, int height, short[] table, LineDef[] lines)
		{
			this.originX = originX;
			this.originY = originY;
			this.width = width;
			this.height = height;
			this.table = table;
			this.lines = lines;

			this.thingLists = new Mobj[width * height];
		}

		public static BlockMap FromWad(Wad wad, int lump, LineDef[] lines)
		{
			var data = wad.ReadLump(lump);

			var table = new short[data.Length / 2];

			for (var i = 0; i < table.Length; i++)
			{
				var offset = 2 * i;
				table[i] = BitConverter.ToInt16(data, offset);
			}

			var originX = Fixed.FromInt(table[0]);
			var originY = Fixed.FromInt(table[1]);
			var width = table[2];
			var height = table[3];

			return new BlockMap(originX, originY, width, height, table, lines);
		}

		public int GetBlockX(Fixed x)
		{
			return (x - this.originX).Data >> BlockMap.FracToBlockShift;
		}

		public int GetBlockY(Fixed y)
		{
			return (y - this.originY).Data >> BlockMap.FracToBlockShift;
		}

		public int GetIndex(int blockX, int blockY)
		{
			if (0 <= blockX && blockX < this.width && 0 <= blockY && blockY < this.height)
			{
				return this.width * blockY + blockX;
			}
			else
			{
				return -1;
			}
		}

		public int GetIndex(Fixed x, Fixed y)
		{
			var blockX = this.GetBlockX(x);
			var blockY = this.GetBlockY(y);

			return this.GetIndex(blockX, blockY);
		}

		public bool IterateLines(int blockX, int blockY, Func<LineDef, bool> func, int validCount)
		{
			var index = this.GetIndex(blockX, blockY);

			if (index == -1)
			{
				return true;
			}

			for (var offset = this.table[4 + index]; this.table[offset] != -1; offset++)
			{
				var line = this.lines[this.table[offset]];

				if (line.ValidCount == validCount)
				{
					continue;
				}

				line.ValidCount = validCount;

				if (!func(line))
				{
					return false;
				}
			}

			return true;
		}

		public bool IterateThings(int blockX, int blockY, Func<Mobj, bool> func)
		{
			var index = this.GetIndex(blockX, blockY);

			if (index == -1)
			{
				return true;
			}

			for (var mobj = this.thingLists[index]; mobj != null; mobj = mobj.BlockNext)
			{
				if (!func(mobj))
				{
					return false;
				}
			}

			return true;
		}

		public Fixed OriginX => this.originX;
		public Fixed OriginY => this.originY;
		public int Width => this.width;
		public int Height => this.height;
		public Mobj[] ThingLists => this.thingLists;
	}
}
