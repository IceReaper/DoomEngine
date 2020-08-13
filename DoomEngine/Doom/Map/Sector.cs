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
	using Common;
	using Graphics;
	using Math;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Wad;
	using World;

	public sealed class Sector
	{
		private static readonly int dataSize = 26;

		private int number;
		private Fixed floorHeight;
		private Fixed ceilingHeight;
		private int floorFlat;
		private int ceilingFlat;
		private int lightLevel;
		private SectorSpecial special;
		private int tag;

		// 0 = untraversed, 1, 2 = sndlines - 1.
		private int soundTraversed;

		// Thing that made a sound (or null).
		private Mobj soundTarget;

		// Mapblock bounding box for height changes.
		private int[] blockBox;

		// Origin for any sounds played by the sector.
		private Mobj soundOrigin;

		// If == validcount, already checked.
		private int validCount;

		// List of mobjs in sector.
		private Mobj thingList;

		// Thinker for reversable actions.
		private Thinker specialData;

		private LineDef[] lines;

		public Sector(int number, Fixed floorHeight, Fixed ceilingHeight, int floorFlat, int ceilingFlat, int lightLevel, SectorSpecial special, int tag)
		{
			this.number = number;
			this.floorHeight = floorHeight;
			this.ceilingHeight = ceilingHeight;
			this.floorFlat = floorFlat;
			this.ceilingFlat = ceilingFlat;
			this.lightLevel = lightLevel;
			this.special = special;
			this.tag = tag;
		}

		public static Sector FromData(byte[] data, int offset, int number, FlatLookup flats)
		{
			var floorHeight = BitConverter.ToInt16(data, offset);
			var ceilingHeight = BitConverter.ToInt16(data, offset + 2);
			var floorFlatName = DoomInterop.ToString(data, offset + 4, 8);
			var ceilingFlatName = DoomInterop.ToString(data, offset + 12, 8);
			var lightLevel = BitConverter.ToInt16(data, offset + 20);
			var special = BitConverter.ToInt16(data, offset + 22);
			var tag = BitConverter.ToInt16(data, offset + 24);

			return new Sector(
				number,
				Fixed.FromInt(floorHeight),
				Fixed.FromInt(ceilingHeight),
				flats.GetNumber(floorFlatName),
				flats.GetNumber(ceilingFlatName),
				lightLevel,
				(SectorSpecial) special,
				tag
			);
		}

		public static Sector[] FromWad(Wad wad, int lump, FlatLookup flats)
		{
			var length = wad.GetLumpSize(lump);

			if (length % Sector.dataSize != 0)
			{
				throw new Exception();
			}

			var data = wad.ReadLump(lump);
			var count = length / Sector.dataSize;
			var sectors = new Sector[count];
			;

			for (var i = 0; i < count; i++)
			{
				var offset = Sector.dataSize * i;
				sectors[i] = Sector.FromData(data, offset, i, flats);
			}

			return sectors;
		}

		public ThingEnumerator GetEnumerator()
		{
			return new ThingEnumerator(this);
		}

		public struct ThingEnumerator : IEnumerator<Mobj>
		{
			private Sector sector;
			private Mobj thing;
			private Mobj current;

			public ThingEnumerator(Sector sector)
			{
				this.sector = sector;
				this.thing = sector.thingList;
				this.current = null;
			}

			public bool MoveNext()
			{
				if (this.thing != null)
				{
					this.current = this.thing;
					this.thing = this.thing.SectorNext;

					return true;
				}
				else
				{
					this.current = null;

					return false;
				}
			}

			public void Reset()
			{
				this.thing = this.sector.thingList;
				this.current = null;
			}

			public void Dispose()
			{
			}

			public Mobj Current => this.current;

			object IEnumerator.Current => throw new NotImplementedException();
		}

		public int Number => this.number;

		public Fixed FloorHeight
		{
			get => this.floorHeight;
			set => this.floorHeight = value;
		}

		public Fixed CeilingHeight
		{
			get => this.ceilingHeight;
			set => this.ceilingHeight = value;
		}

		public int FloorFlat
		{
			get => this.floorFlat;
			set => this.floorFlat = value;
		}

		public int CeilingFlat
		{
			get => this.ceilingFlat;
			set => this.ceilingFlat = value;
		}

		public int LightLevel
		{
			get => this.lightLevel;
			set => this.lightLevel = value;
		}

		public SectorSpecial Special
		{
			get => this.special;
			set => this.special = value;
		}

		public int Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		public int SoundTraversed
		{
			get => this.soundTraversed;
			set => this.soundTraversed = value;
		}

		public Mobj SoundTarget
		{
			get => this.soundTarget;
			set => this.soundTarget = value;
		}

		public int[] BlockBox
		{
			get => this.blockBox;
			set => this.blockBox = value;
		}

		public Mobj SoundOrigin
		{
			get => this.soundOrigin;
			set => this.soundOrigin = value;
		}

		public int ValidCount
		{
			get => this.validCount;
			set => this.validCount = value;
		}

		public Mobj ThingList
		{
			get => this.thingList;
			set => this.thingList = value;
		}

		public Thinker SpecialData
		{
			get => this.specialData;
			set => this.specialData = value;
		}

		public LineDef[] Lines
		{
			get => this.lines;
			set => this.lines = value;
		}
	}
}
