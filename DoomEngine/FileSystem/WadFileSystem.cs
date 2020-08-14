namespace DoomEngine.FileSystem
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Utils;

	public class WadFileSystem : IReadableFileSystem
	{
		private readonly Dictionary<string, SegmentStream> files = new Dictionary<string, SegmentStream>();
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
					var key = string.Concat(groups.Select(group => $"{group}/")) + name;

					// TODO while in theory a same named file with different contents may exist, we rely on loading the first only for now.
					if (!this.files.ContainsKey(key))
						this.files.Add(key, new SegmentStream(stream, position, length));

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
			return this.Exists(path) ? this.files[path] : null;
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
