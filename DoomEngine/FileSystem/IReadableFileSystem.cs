namespace DoomEngine.FileSystem
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	public interface IReadableFileSystem : IDisposable
	{
		bool Exists(string path);
		Stream Read(string path);
		IEnumerable<string> Files();
	}
}
