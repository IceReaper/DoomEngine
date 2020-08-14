namespace DoomEngine.Platform
{
	using Doom.Common;

	public interface IPlatform
	{
		IRenderer CreateRenderer(Config config, IWindow window, CommonResource resource);
		ISound CreateSound(Config config);
		IMusic CreateMusic(Config config);
		IUserInput CreateUserInput(Config config, IWindow window, bool useMouse);
		IWindow CreateWindow(string title, Config config);
		int[] GetDefaultVideoMode();
	}
}
