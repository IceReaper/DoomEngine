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

	public sealed class Vertex
	{
		private static readonly int dataSize = 4;

		private Fixed x;
		private Fixed y;

		public Vertex(Fixed x, Fixed y)
		{
			this.x = x;
			this.y = y;
		}

		public static Vertex FromData(byte[] data, int offset)
		{
			var x = BitConverter.ToInt16(data, offset);
			var y = BitConverter.ToInt16(data, offset + 2);

			return new Vertex(Fixed.FromInt(x), Fixed.FromInt(y));
		}

		public static Vertex[] FromWad(string fileName)
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(fileName));
			var length = reader.BaseStream.Length;

			if (length % Vertex.dataSize != 0)
			{
				throw new Exception();
			}

			var data = reader.ReadBytes((int) reader.BaseStream.Length);
			var count = length / Vertex.dataSize;
			var vertices = new Vertex[count];
			;

			for (var i = 0; i < count; i++)
			{
				var offset = Vertex.dataSize * i;
				vertices[i] = Vertex.FromData(data, offset);
			}

			return vertices;
		}

		public Fixed X => this.x;
		public Fixed Y => this.y;
	}
}
