namespace DoomEngine.FileSystem
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class FolderFileSystem : IWritableFileSystem
	{
		private readonly string path;

		public FolderFileSystem(string path)
		{
			this.path = path;
		}

		public bool Exists(string path)
		{
			return File.Exists(Path.Combine(this.path, path));
		}

		public Stream Read(string path)
		{
			return File.OpenRead(Path.Combine(this.path, path));
		}

		public IEnumerable<string> Files(string path)
		{
			return Directory.GetFiles(Path.Combine(this.path, path), "*", SearchOption.AllDirectories)
				.Select(file => file.Substring(this.path.Length).Replace('\\', '/').Trim('/'));
		}

		public void Delete(string path)
		{
			File.Delete(Path.Combine(this.path, path));
		}

		public Stream Write(string path)
		{
			return File.OpenWrite(Path.Combine(this.path, path));
		}
	}
}
