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

namespace DoomEngine.Doom.Wad
{
	using Common;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;

	public sealed class Wad : IDisposable
	{
		private List<string> names;
		private List<Stream> streams;
		private List<LumpInfo> lumpInfos;

		public Wad(params string[] fileNames)
		{
			try
			{
				Console.Write("Open wad files: ");

				this.names = new List<string>();
				this.streams = new List<Stream>();
				this.lumpInfos = new List<LumpInfo>();

				foreach (var fileName in fileNames)
				{
					this.AddFile(fileName);
				}

				Console.WriteLine("OK (" + string.Join(", ", fileNames.Select(x => Path.GetFileName(x))) + ")");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				this.Dispose();
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private void AddFile(string fileName)
		{
			this.names.Add(Path.GetFileNameWithoutExtension(fileName).ToLower());

			var stream = DoomApplication.Instance.FileSystem.Read(fileName);
			this.streams.Add(stream);

			string identification;
			int lumpCount;
			int lumpInfoTableOffset;

			{
				var data = new byte[12];

				if (stream.Read(data, 0, data.Length) != data.Length)
				{
					throw new Exception("Failed to read the WAD file.");
				}

				identification = DoomInterop.ToString(data, 0, 4);
				lumpCount = BitConverter.ToInt32(data, 4);
				lumpInfoTableOffset = BitConverter.ToInt32(data, 8);

				if (identification != "IWAD" && identification != "PWAD")
				{
					throw new Exception("The file is not a WAD file.");
				}
			}

			{
				var data = new byte[LumpInfo.DataSize * lumpCount];
				stream.Seek(lumpInfoTableOffset, SeekOrigin.Begin);

				if (stream.Read(data, 0, data.Length) != data.Length)
				{
					throw new Exception("Failed to read the WAD file.");
				}

				for (var i = 0; i < lumpCount; i++)
				{
					var offset = LumpInfo.DataSize * i;

					var lumpInfo = new LumpInfo(
						DoomInterop.ToString(data, offset + 8, 8),
						stream,
						BitConverter.ToInt32(data, offset),
						BitConverter.ToInt32(data, offset + 4)
					);

					this.lumpInfos.Add(lumpInfo);
				}
			}
		}

		public int GetLumpNumber(string name)
		{
			for (var i = this.lumpInfos.Count - 1; i >= 0; i--)
			{
				if (this.lumpInfos[i].Name == name)
				{
					return i;
				}
			}

			return -1;
		}

		public void Dispose()
		{
			Console.WriteLine("Close wad files.");

			foreach (var stream in this.streams)
			{
				stream.Dispose();
			}

			this.streams.Clear();
		}

		public IReadOnlyList<string> Names => this.names;
	}
}
