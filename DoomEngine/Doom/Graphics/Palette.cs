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

namespace DoomEngine.Doom.Graphics
{
	using System;
	using System.IO;
	using System.Runtime.ExceptionServices;

	public sealed class Palette
	{
		public static readonly int DamageStart = 1;
		public static readonly int DamageCount = 8;

		public static readonly int BonusStart = 9;
		public static readonly int BonusCount = 4;

		public static readonly int IronFeet = 13;

		private byte[] data;

		private uint[][] palettes;

		public Palette()
		{
			try
			{
				Console.Write("Load palette: ");

				var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read("PLAYPAL"));
				this.data = reader.ReadBytes((int) reader.BaseStream.Length);

				var count = this.data.Length / (3 * 256);
				this.palettes = new uint[count][];

				for (var i = 0; i < this.palettes.Length; i++)
				{
					this.palettes[i] = new uint[256];
				}

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public void ResetColors(double p)
		{
			for (var i = 0; i < this.palettes.Length; i++)
			{
				var paletteOffset = (3 * 256) * i;

				for (var j = 0; j < 256; j++)
				{
					var colorOffset = paletteOffset + 3 * j;

					var r = this.data[colorOffset];
					var g = this.data[colorOffset + 1];
					var b = this.data[colorOffset + 2];

					r = (byte) Math.Round(255 * Palette.CorrectionCurve(r / 255.0, p));
					g = (byte) Math.Round(255 * Palette.CorrectionCurve(g / 255.0, p));
					b = (byte) Math.Round(255 * Palette.CorrectionCurve(b / 255.0, p));

					this.palettes[i][j] = (uint) ((r << 0) | (g << 8) | (b << 16) | (255 << 24));
				}
			}
		}

		private static double CorrectionCurve(double x, double p)
		{
			return Math.Pow(x, p);
		}

		public uint[] this[int paletteNumber]
		{
			get
			{
				return this.palettes[paletteNumber];
			}
		}
	}
}
