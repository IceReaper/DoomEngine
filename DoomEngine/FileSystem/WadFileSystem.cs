namespace DoomEngine.FileSystem
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class WadFileSystem : IReadableFileSystem
	{
		private class LumpInfo
		{
			public readonly int Position;
			public readonly int Length;

			public LumpInfo(int position, int length)
			{
				this.Position = position;
				this.Length = length;
			}
		}

		private readonly Dictionary<string, LumpInfo> files = new Dictionary<string, LumpInfo>();
		private Stream stream;

		public WadFileSystem(Stream stream)
		{
			this.stream = stream;

			var reader = new BinaryReader(stream);

			var magic = new string(reader.ReadChars(4));

			if (magic != "IWAD" && magic != "PWAD")
				throw new Exception("The file is not a WAD file.");

			var filesAmount = reader.ReadInt32();
			var filesOffset = reader.ReadInt32();

			stream.Position = filesOffset;

			var groups = new List<string>();
			var inMap = false;

			for (var i = 0; i < filesAmount; i++)
			{
				var position = reader.ReadInt32();
				var length = reader.ReadInt32();
				var name = new string(reader.ReadChars(8)).Split((char) 0)[0];

				if (length == 0)
				{
					if (inMap)
						throw new Exception("Unexpected group in map found.");

					if (name.Length > 6 && name.Substring(name.Length - 6) == "_START")
						groups.Add(name.Substring(0, name.Length - 6));
					else if (name.Length > 4 && name.Substring(name.Length - 4) == "_END")
					{
						if (groups.Last() == name.Substring(0, name.Length - 4))
							groups.RemoveAt(groups.Count - 1);
						else
							throw new Exception("Unexpected group end found.");
					}
					else
					{
						if (groups.Count != 0)
							throw new Exception("Unexpected map in group found.");

						groups.Add(name);
						inMap = true;
					}
				}
				else
				{
					string key;

					if (groups.Count == 0)
						key = name;
					else if (inMap)
						key = $"MAPS/{groups[0]}/{name}";
					else
						key = groups[0] switch
						{
							"S" => $"SPRITES/{name}",
							"F" => $"FLATS/{name}",
							"P" => $"PATCHES/{name}",
							_ => throw new Exception("Unknown group found.")
						};

					// TODO while in theory a same named file with different contents may exist, we rely on loading the first only for now.
					if (!this.files.ContainsKey(key))
						this.files.Add(key, new LumpInfo(position, length));

					if (!inMap || name != "BLOCKMAP")
						continue;

					groups.RemoveAt(groups.Count - 1);
					inMap = false;
				}
			}

			if (groups.Count > 0)
				throw new Exception("Unterminated group left.");
		}

		public bool Exists(string path)
		{
			return this.files.ContainsKey(path);
		}

		public Stream Read(string path)
		{
			if (!this.Exists(path))
				return null;

			this.stream.Position = this.files[path].Position;
			var buffer = new byte[this.files[path].Length];
			this.stream.Read(buffer, 0, buffer.Length);

			return new MemoryStream(buffer);
		}

		public IEnumerable<string> Files()
		{
			return this.files.Keys;
		}

		public void Dispose()
		{
			this.stream.Dispose();
		}
	}
}
