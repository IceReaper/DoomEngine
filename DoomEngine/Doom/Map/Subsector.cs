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
	using System;
	using System.IO;

	public sealed class Subsector
	{
		private static readonly int dataSize = 4;

		private Sector sector;
		private int segCount;
		private int firstSeg;

		public Subsector(Sector sector, int segCount, int firstSeg)
		{
			this.sector = sector;
			this.segCount = segCount;
			this.firstSeg = firstSeg;
		}

		public static Subsector FromData(byte[] data, int offset, Seg[] segs)
		{
			var segCount = BitConverter.ToInt16(data, offset);
			var firstSegNumber = BitConverter.ToInt16(data, offset + 2);

			return new Subsector(segs[firstSegNumber].SideDef.Sector, segCount, firstSegNumber);
		}

		public static Subsector[] FromWad(string fileName, Seg[] segs)
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(fileName));
			var length = reader.BaseStream.Length;

			if (length % Subsector.dataSize != 0)
			{
				throw new Exception();
			}

			var data = reader.ReadBytes((int) reader.BaseStream.Length);
			var count = length / Subsector.dataSize;
			var subsectors = new Subsector[count];

			for (var i = 0; i < count; i++)
			{
				var offset = Subsector.dataSize * i;
				subsectors[i] = Subsector.FromData(data, offset, segs);
			}

			return subsectors;
		}

		public Sector Sector => this.sector;
		public int SegCount => this.segCount;
		public int FirstSeg => this.firstSeg;
	}
}
