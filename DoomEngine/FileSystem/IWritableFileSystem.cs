namespace DoomEngine.FileSystem
{
	using System.IO;

	public interface IWritableFileSystem : IReadableFileSystem
	{
		void Delete(string path);
		Stream Write(string path);
	}
}
