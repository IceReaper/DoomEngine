namespace DoomEngine.Platform
{
	using Doom.Common;
	using Doom.Wad;

	public interface IPlatform
	{
		IRenderer CreateRenderer(Config config, IWindow window, CommonResource resource);
		ISound CreateSound(Config config, Wad wad);
		IMusic CreateMusic(Config config, Wad wad);
		IUserInput CreateUserInput(Config config, IWindow window, bool useMouse);
		IWindow CreateWindow(string title, Config config);
		int[] GetDefaultVideoMode();
	}
}
