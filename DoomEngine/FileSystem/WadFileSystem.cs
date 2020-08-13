namespace DoomEngine.FileSystem
{
	using System.Collections.Generic;
	using System.IO;

	public class WadFileSystem : IReadableFileSystem
	{
		public WadFileSystem(Stream wad)
		{
			throw new System.NotImplementedException();
		}

		public bool Exists(string path)
		{
			throw new System.NotImplementedException();
		}

		public Stream Read(string path)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<string> Files(string path)
		{
			throw new System.NotImplementedException();
		}
	}
}
