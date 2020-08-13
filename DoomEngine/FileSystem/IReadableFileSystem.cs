namespace DoomEngine.FileSystem
{
	using System.Collections.Generic;
	using System.IO;

	public interface IReadableFileSystem
	{
		bool Exists(string path);
		Stream Read(string path);
		IEnumerable<string> Files(string path);
	}
}
