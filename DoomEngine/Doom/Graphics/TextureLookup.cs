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
	using Common;
	using Info;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.ExceptionServices;

	public sealed class TextureLookup : IReadOnlyList<Texture>
	{
		private List<Texture> textures;
		private Dictionary<string, Texture> nameToTexture;
		private Dictionary<string, int> nameToNumber;

		private int[] switchList;

		public TextureLookup()
		{
			this.Init();
			this.InitSwitchList();
		}

		private void Init()
		{
			try
			{
				Console.Write("Load textures: ");

				this.textures = new List<Texture>();
				this.nameToTexture = new Dictionary<string, Texture>();
				this.nameToNumber = new Dictionary<string, int>();

				var patches = TextureLookup.LoadPatches();

				for (var n = 1; n <= 2; n++)
				{
					var name = "TEXTURE" + n;

					if (!DoomApplication.Instance.FileSystem.Exists(name))
					{
						break;
					}

					var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(name));

					var data = reader.ReadBytes((int) reader.BaseStream.Length);
					reader.BaseStream.Position = 0;

					var count = reader.ReadInt32();

					for (var i = 0; i < count; i++)
					{
						var offset = reader.ReadInt32();
						var texture = Texture.FromData(data, offset, patches);
						this.nameToNumber.Add(texture.Name, this.textures.Count);
						this.textures.Add(texture);
						this.nameToTexture.Add(texture.Name, texture);
					}
				}

				Console.WriteLine("OK (" + this.nameToTexture.Count + " textures)");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private void InitSwitchList()
		{
			var list = new List<int>();

			foreach (var tuple in DoomInfo.SwitchNames)
			{
				var texNum1 = this.GetNumber(tuple.Item1);
				var texNum2 = this.GetNumber(tuple.Item2);

				if (texNum1 != -1 && texNum2 != -1)
				{
					list.Add(texNum1);
					list.Add(texNum2);
				}
			}

			this.switchList = list.ToArray();
		}

		public int GetNumber(string name)
		{
			if (name[0] == '-')
			{
				return 0;
			}

			int number;

			if (this.nameToNumber.TryGetValue(name, out number))
			{
				return number;
			}
			else
			{
				return -1;
			}
		}

		private static Patch[] LoadPatches()
		{
			var patchNames = TextureLookup.LoadPatchNames();
			var patches = new Patch[patchNames.Length];

			for (var i = 0; i < patches.Length; i++)
			{
				var name = patchNames[i];

				// This check is necessary to avoid crash in DOOM1.WAD.
				if (!DoomApplication.Instance.FileSystem.Exists(name))
				{
					if (name == "TFOGF0" || name == "TFOGI0") // TNT.WAD uses this sprites as patches...
						name = $"SPRITES/{name}";
					else if (name == "BOSFA0") // PLUTONIA.WAD uses this sprite as patch... 
						name = $"SPRITES/{name}";
					else
						name = $"PATCHES/{name}";

					if (!DoomApplication.Instance.FileSystem.Exists(name))
						continue;
				}

				var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(name));
				patches[i] = Patch.FromData(name, reader.ReadBytes((int) reader.BaseStream.Length));
			}

			return patches;
		}

		private static string[] LoadPatchNames()
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read("PNAMES"));
			var count = reader.ReadInt32();
			var names = new string[count];

			for (var i = 0; i < names.Length; i++)
			{
				names[i] = DoomInterop.ToString(reader.ReadBytes(8), 0, 8);
			}

			return names;
		}

		public IEnumerator<Texture> GetEnumerator()
		{
			return this.textures.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.textures.GetEnumerator();
		}

		public int Count => this.textures.Count;
		public Texture this[int num] => this.textures[num];
		public Texture this[string name] => this.nameToTexture[name];
		public int[] SwitchList => this.switchList;
	}
}
