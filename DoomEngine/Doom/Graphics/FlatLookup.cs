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
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;

	public sealed class FlatLookup : IReadOnlyList<Flat>
	{
		private Flat[] flats;

		private Dictionary<string, Flat> nameToFlat;
		private Dictionary<string, int> nameToNumber;

		private int skyFlatNumber;
		private Flat skyFlat;

		public FlatLookup()
		{
			try
			{
				Console.Write("Load flats: ");

				var flats = DoomApplication.Instance.FileSystem.Files().Where(file => file.StartsWith("FLATS/")).ToArray();
				var count = flats.Length;

				this.flats = new Flat[count];

				this.nameToFlat = new Dictionary<string, Flat>();
				this.nameToNumber = new Dictionary<string, int>();

				for (var i = 0; i < flats.Length; i++)
				{
					var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(flats[i]));

					if (reader.BaseStream.Length != 4096)
					{
						continue;
					}

					var name = flats[i].Substring(6);
					var flat = new Flat(name, reader.ReadBytes((int) reader.BaseStream.Length));

					this.flats[i] = flat;
					this.nameToFlat[name] = flat;
					this.nameToNumber[name] = i;
				}

				this.skyFlatNumber = this.nameToNumber["F_SKY1"];
				this.skyFlat = this.nameToFlat["F_SKY1"];

				Console.WriteLine("OK (" + this.nameToFlat.Count + " flats)");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public int GetNumber(string name)
		{
			if (this.nameToNumber.ContainsKey(name))
			{
				return this.nameToNumber[name];
			}
			else
			{
				return -1;
			}
		}

		public IEnumerator<Flat> GetEnumerator()
		{
			return ((IEnumerable<Flat>) this.flats).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.flats.GetEnumerator();
		}

		public int Count => this.flats.Length;
		public Flat this[int num] => this.flats[num];
		public Flat this[string name] => this.nameToFlat[name];
		public int SkyFlatNumber => this.skyFlatNumber;
		public Flat SkyFlat => this.skyFlat;
	}
}
