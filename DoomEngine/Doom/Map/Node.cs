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

	public sealed class Node
	{
		private static readonly int dataSize = 28;

		private Fixed x;
		private Fixed y;
		private Fixed dx;
		private Fixed dy;

		private Fixed[][] boundingBox;

		private int[] children;

		public Node(
			Fixed x,
			Fixed y,
			Fixed dx,
			Fixed dy,
			Fixed frontBoundingBoxTop,
			Fixed frontBoundingBoxBottom,
			Fixed frontBoundingBoxLeft,
			Fixed frontBoundingBoxRight,
			Fixed backBoundingBoxTop,
			Fixed backBoundingBoxBottom,
			Fixed backBoundingBoxLeft,
			Fixed backBoundingBoxRight,
			int frontChild,
			int backChild
		)
		{
			this.x = x;
			this.y = y;
			this.dx = dx;
			this.dy = dy;

			var frontBoundingBox = new Fixed[4] {frontBoundingBoxTop, frontBoundingBoxBottom, frontBoundingBoxLeft, frontBoundingBoxRight};

			var backBoundingBox = new Fixed[4] {backBoundingBoxTop, backBoundingBoxBottom, backBoundingBoxLeft, backBoundingBoxRight};

			this.boundingBox = new Fixed[][] {frontBoundingBox, backBoundingBox};

			this.children = new int[] {frontChild, backChild};
		}

		public static Node FromData(byte[] data, int offset)
		{
			var x = BitConverter.ToInt16(data, offset);
			var y = BitConverter.ToInt16(data, offset + 2);
			var dx = BitConverter.ToInt16(data, offset + 4);
			var dy = BitConverter.ToInt16(data, offset + 6);
			var frontBoundingBoxTop = BitConverter.ToInt16(data, offset + 8);
			var frontBoundingBoxBottom = BitConverter.ToInt16(data, offset + 10);
			var frontBoundingBoxLeft = BitConverter.ToInt16(data, offset + 12);
			var frontBoundingBoxRight = BitConverter.ToInt16(data, offset + 14);
			var backBoundingBoxTop = BitConverter.ToInt16(data, offset + 16);
			var backBoundingBoxBottom = BitConverter.ToInt16(data, offset + 18);
			var backBoundingBoxLeft = BitConverter.ToInt16(data, offset + 20);
			var backBoundingBoxRight = BitConverter.ToInt16(data, offset + 22);
			var frontChild = BitConverter.ToInt16(data, offset + 24);
			var backChild = BitConverter.ToInt16(data, offset + 26);

			return new Node(
				Fixed.FromInt(x),
				Fixed.FromInt(y),
				Fixed.FromInt(dx),
				Fixed.FromInt(dy),
				Fixed.FromInt(frontBoundingBoxTop),
				Fixed.FromInt(frontBoundingBoxBottom),
				Fixed.FromInt(frontBoundingBoxLeft),
				Fixed.FromInt(frontBoundingBoxRight),
				Fixed.FromInt(backBoundingBoxTop),
				Fixed.FromInt(backBoundingBoxBottom),
				Fixed.FromInt(backBoundingBoxLeft),
				Fixed.FromInt(backBoundingBoxRight),
				frontChild,
				backChild
			);
		}

		public static Node[] FromWad(string fileName, Subsector[] subsectors)
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(fileName));
			var length = reader.BaseStream.Length;

			if (length % Node.dataSize != 0)
			{
				throw new Exception();
			}

			var data = reader.ReadBytes((int) reader.BaseStream.Length);
			var count = length / Node.dataSize;
			var nodes = new Node[count];

			for (var i = 0; i < count; i++)
			{
				var offset = Node.dataSize * i;
				nodes[i] = Node.FromData(data, offset);
			}

			return nodes;
		}

		public static bool IsSubsector(int node)
		{
			return (node & unchecked((int) 0xFFFF8000)) != 0;
		}

		public static int GetSubsector(int node)
		{
			return node ^ unchecked((int) 0xFFFF8000);
		}

		public Fixed X => this.x;
		public Fixed Y => this.y;
		public Fixed Dx => this.dx;
		public Fixed Dy => this.dy;
		public Fixed[][] BoundingBox => this.boundingBox;
		public int[] Children => this.children;
	}
}
