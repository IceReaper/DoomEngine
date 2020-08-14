namespace DoomEngine.FileSystem
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class VirtualFileSystem : IWritableFileSystem
	{
		private readonly List<IReadableFileSystem> fileSystems = new List<IReadableFileSystem>();

		public VirtualFileSystem()
		{
			this.fileSystems.Add(new FolderFileSystem(Directory.GetCurrentDirectory()));
			this.fileSystems.Add(new FolderFileSystem(AppDomain.CurrentDomain.BaseDirectory));
		}

		public void Add(IReadableFileSystem fileSystem)
		{
			this.fileSystems.Add(fileSystem);
		}

		public bool Exists(string path)
		{
			return this.fileSystems.Any(fileSystem => fileSystem.Exists(path));
		}

		public Stream Read(string path)
		{
			return this.fileSystems.FirstOrDefault(fileSystem => fileSystem.Exists(path))?.Read(path);
		}

		public IEnumerable<string> Files()
		{
			var files = new List<string>();

			foreach (var fileSystem in this.fileSystems)
				files.AddRange(fileSystem.Files());

			return files;
		}

		public void Delete(string path)
		{
			(this.fileSystems.FirstOrDefault(fileSystem => fileSystem.Exists(path)) as IWritableFileSystem)?.Delete(path);
		}

		public Stream Write(string path)
		{
			return this.fileSystems.OfType<IWritableFileSystem>().FirstOrDefault()?.Write(path);
		}

		public void Dispose()
		{
			this.fileSystems.ForEach(fileSystem => fileSystem.Dispose());
		}
	}
}
