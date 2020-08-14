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
	using System.IO;
	using World;

	public sealed class LineDef
	{
		private static readonly int dataSize = 14;

		private Vertex vertex1;
		private Vertex vertex2;

		private Fixed dx;
		private Fixed dy;

		private LineFlags flags;
		private LineSpecial special;
		private short tag;

		private SideDef frontSide;
		private SideDef backSide;

		private Fixed[] boundingBox;

		private SlopeType slopeType;

		private Sector frontSector;
		private Sector backSector;

		private int validCount;

		private Thinker specialData;

		private Mobj soundOrigin;

		public LineDef(Vertex vertex1, Vertex vertex2, LineFlags flags, LineSpecial special, short tag, SideDef frontSide, SideDef backSide)
		{
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;
			this.flags = flags;
			this.special = special;
			this.tag = tag;
			this.frontSide = frontSide;
			this.backSide = backSide;

			this.dx = vertex2.X - vertex1.X;
			this.dy = vertex2.Y - vertex1.Y;

			if (this.dx == Fixed.Zero)
			{
				this.slopeType = SlopeType.Vertical;
			}
			else if (this.dy == Fixed.Zero)
			{
				this.slopeType = SlopeType.Horizontal;
			}
			else
			{
				if (this.dy / this.dx > Fixed.Zero)
				{
					this.slopeType = SlopeType.Positive;
				}
				else
				{
					this.slopeType = SlopeType.Negative;
				}
			}

			this.boundingBox = new Fixed[4];
			this.boundingBox[Box.Top] = Fixed.Max(vertex1.Y, vertex2.Y);
			this.boundingBox[Box.Bottom] = Fixed.Min(vertex1.Y, vertex2.Y);
			this.boundingBox[Box.Left] = Fixed.Min(vertex1.X, vertex2.X);
			this.boundingBox[Box.Right] = Fixed.Max(vertex1.X, vertex2.X);

			this.frontSector = frontSide?.Sector;
			this.backSector = backSide?.Sector;
		}

		public static LineDef FromData(byte[] data, int offset, Vertex[] vertices, SideDef[] sides)
		{
			var vertex1Number = BitConverter.ToInt16(data, offset);
			var vertex2Number = BitConverter.ToInt16(data, offset + 2);
			var flags = BitConverter.ToInt16(data, offset + 4);
			var special = BitConverter.ToInt16(data, offset + 6);
			var tag = BitConverter.ToInt16(data, offset + 8);
			var side0Number = BitConverter.ToInt16(data, offset + 10);
			var side1Number = BitConverter.ToInt16(data, offset + 12);

			return new LineDef(
				vertices[vertex1Number],
				vertices[vertex2Number],
				(LineFlags) flags,
				(LineSpecial) special,
				tag,
				sides[side0Number],
				side1Number != -1 ? sides[side1Number] : null
			);
		}

		public static LineDef[] FromWad(string fileName, Vertex[] vertices, SideDef[] sides)
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(fileName));
			var length = reader.BaseStream.Length;

			if (length % LineDef.dataSize != 0)
			{
				throw new Exception();
			}

			var data = reader.ReadBytes((int) reader.BaseStream.Length);
			var count = length / LineDef.dataSize;
			var lines = new LineDef[count];
			;

			for (var i = 0; i < count; i++)
			{
				var offset = 14 * i;
				lines[i] = LineDef.FromData(data, offset, vertices, sides);
			}

			return lines;
		}

		public Vertex Vertex1 => this.vertex1;
		public Vertex Vertex2 => this.vertex2;

		public Fixed Dx => this.dx;
		public Fixed Dy => this.dy;

		public LineFlags Flags
		{
			get => this.flags;
			set => this.flags = value;
		}

		public LineSpecial Special
		{
			get => this.special;
			set => this.special = value;
		}

		public short Tag
		{
			get => this.tag;
			set => this.tag = value;
		}

		public SideDef FrontSide => this.frontSide;
		public SideDef BackSide => this.backSide;

		public Fixed[] BoundingBox => this.boundingBox;

		public SlopeType SlopeType => this.slopeType;

		public Sector FrontSector => this.frontSector;
		public Sector BackSector => this.backSector;

		public int ValidCount
		{
			get => this.validCount;
			set => this.validCount = value;
		}

		public Thinker SpecialData
		{
			get => this.specialData;
			set => this.specialData = value;
		}

		public Mobj SoundOrigin
		{
			get => this.soundOrigin;
			set => this.soundOrigin = value;
		}
	}
}
