﻿//
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

namespace DoomEngine.Doom.Graphics
{
	using System;
	using System.Runtime.ExceptionServices;
	using Wad;

	public sealed class ColorMap
	{
		public static readonly int Inverse = 32;

		private byte[][] data;

		public ColorMap(Wad wad)
		{
			try
			{
				Console.Write("Load color map: ");

				var raw = wad.ReadLump("COLORMAP");
				var num = raw.Length / 256;
				this.data = new byte[num][];

				for (var i = 0; i < num; i++)
				{
					this.data[i] = new byte[256];
					var offset = 256 * i;

					for (var c = 0; c < 256; c++)
					{
						this.data[i][c] = raw[offset + c];
					}
				}

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public byte[] this[int index]
		{
			get
			{
				return this.data[index];
			}
		}

		public byte[] FullBright
		{
			get
			{
				return this.data[0];
			}
		}
	}
}
