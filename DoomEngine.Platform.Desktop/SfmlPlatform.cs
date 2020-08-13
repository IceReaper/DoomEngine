namespace DoomEngine.Platform.Desktop
{
	using Doom.Common;
	using Doom.Wad;
	using SFML.Window;
	using System.IO;

	public class SfmlPlatform : IPlatform
	{
		public IRenderer CreateRenderer(Config config, IWindow window, CommonResource resource)
		{
			return new SfmlRenderer(config, (SfmlWindow) window, resource);
		}

		public ISound CreateSound(Config config, Wad wad)
		{
			return new SfmlSound(config, wad);
		}

		public IMusic CreateMusic(Config config, Wad wad)
		{
			var sfPath = "TimGM6mb.sf2";

			if (File.Exists(sfPath))
			{
				return new SfmlMusic(config, wad, sfPath);
			}
			else
			{
				return null;
			}
		}

		public IUserInput CreateUserInput(Config config, IWindow window, bool useMouse)
		{
			return new SfmlUserInput(config, (SfmlWindow) window, useMouse);
		}

		public IWindow CreateWindow(string title, Config config)
		{
			var videoMode = new VideoMode((uint) config.video_screenwidth, (uint) config.video_screenheight);
			var style = Styles.Close | Styles.Titlebar;

			if (config.video_fullscreen)
				style = Styles.Fullscreen;

			return new SfmlWindow(videoMode, title, style);
		}

		public int[] GetDefaultVideoMode()
		{
			var desktop = VideoMode.DesktopMode;

			var baseWidth = 640;
			var baseHeight = 400;

			var currentWidth = baseWidth;
			var currentHeight = baseHeight;

			while (true)
			{
				var nextWidth = currentWidth + baseWidth;
				var nextHeight = currentHeight + baseHeight;

				if (nextWidth >= 0.9 * desktop.Width || nextHeight >= 0.9 * desktop.Height)
				{
					break;
				}

				currentWidth = nextWidth;
				currentHeight = nextHeight;
			}

			return new[] {currentWidth, currentHeight};
		}
	}
}
