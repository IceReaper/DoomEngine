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
	using Info;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;

	public sealed class SpriteLookup
	{
		private SpriteDef[] spriteDefs;

		public SpriteLookup()
		{
			try
			{
				Console.Write("Load sprites: ");

				var temp = new Dictionary<string, List<SpriteInfo>>();

				for (var i = 0; i < (int) Sprite.Count; i++)
				{
					temp.Add(DoomInfo.SpriteNames[i], new List<SpriteInfo>());
				}

				var cache = new Dictionary<string, Patch>();

				foreach (var sprite in DoomApplication.Instance.FileSystem.Files().Where(file => file.StartsWith("SPRITES/")).ToArray())
				{
					var lumpName = sprite.Substring(8);
					var name = lumpName.Substring(0, 4);

					if (!temp.ContainsKey(name))
					{
						continue;
					}

					var list = temp[name];

					{
						var frame = lumpName[4] - 'A';
						var rotation = lumpName[5] - '0';

						while (list.Count < frame + 1)
						{
							list.Add(new SpriteInfo());
						}

						if (rotation == 0)
						{
							for (var i = 0; i < 8; i++)
							{
								if (list[frame].Patches[i] == null)
								{
									list[frame].Patches[i] = SpriteLookup.CachedRead(sprite, cache);
									list[frame].Flip[i] = false;
								}
							}
						}
						else
						{
							if (list[frame].Patches[rotation - 1] == null)
							{
								list[frame].Patches[rotation - 1] = SpriteLookup.CachedRead(sprite, cache);
								list[frame].Flip[rotation - 1] = false;
							}
						}
					}

					if (lumpName.Length == 8)
					{
						var frame = lumpName[6] - 'A';
						var rotation = lumpName[7] - '0';

						while (list.Count < frame + 1)
						{
							list.Add(new SpriteInfo());
						}

						if (rotation == 0)
						{
							for (var i = 0; i < 8; i++)
							{
								if (list[frame].Patches[i] == null)
								{
									list[frame].Patches[i] = SpriteLookup.CachedRead(sprite, cache);
									list[frame].Flip[i] = true;
								}
							}
						}
						else
						{
							if (list[frame].Patches[rotation - 1] == null)
							{
								list[frame].Patches[rotation - 1] = SpriteLookup.CachedRead(sprite, cache);
								list[frame].Flip[rotation - 1] = true;
							}
						}
					}
				}

				this.spriteDefs = new SpriteDef[(int) Sprite.Count];

				for (var i = 0; i < this.spriteDefs.Length; i++)
				{
					var list = temp[DoomInfo.SpriteNames[i]];

					var frames = new SpriteFrame[list.Count];

					for (var j = 0; j < frames.Length; j++)
					{
						list[j].CheckCompletion();

						var frame = new SpriteFrame(list[j].HasRotation(), list[j].Patches, list[j].Flip);
						frames[j] = frame;
					}

					this.spriteDefs[i] = new SpriteDef(frames);
				}

				Console.WriteLine("OK (" + cache.Count + " sprites)");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private static Patch CachedRead(string name, Dictionary<string, Patch> cache)
		{
			if (!cache.ContainsKey(name))
			{
				var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(name));
				cache.Add(name, Patch.FromData(name, reader.ReadBytes((int) reader.BaseStream.Length)));
			}

			return cache[name];
		}

		private class SpriteInfo
		{
			public Patch[] Patches;
			public bool[] Flip;

			public SpriteInfo()
			{
				this.Patches = new Patch[8];
				this.Flip = new bool[8];
			}

			public void CheckCompletion()
			{
				for (var i = 0; i < this.Patches.Length; i++)
				{
					if (this.Patches[i] == null)
					{
						throw new Exception("Missing sprite!");
					}
				}
			}

			public bool HasRotation()
			{
				for (var i = 1; i < this.Patches.Length; i++)
				{
					if (this.Patches[i] != this.Patches[0])
					{
						return true;
					}
				}

				return false;
			}
		}

		public SpriteDef this[Sprite sprite]
		{
			get
			{
				return this.spriteDefs[(int) sprite];
			}
		}
	}
}
