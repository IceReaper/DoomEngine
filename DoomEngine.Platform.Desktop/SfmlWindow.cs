namespace DoomEngine.Platform.Desktop
{
	using SFML.Graphics;
	using SFML.Window;
	using System;
	using UserInput;
	using Color = System.Drawing.Color;

	public class SfmlWindow : RenderWindow, IWindow
	{
		public new event EventHandler<DoomKey> KeyPressed;

		public new event EventHandler<DoomKey> KeyReleased;

		public SfmlWindow(VideoMode mode, string title, Styles style)
			: base(mode, title, style)
		{
			base.KeyPressed += (sender, args) => this.KeyPressed?.Invoke(sender, (DoomKey) args.Code);
			base.KeyReleased += (sender, args) => this.KeyReleased?.Invoke(sender, (DoomKey) args.Code);
		}

		public void Clear(Color color)
		{
			this.Clear(new SFML.Graphics.Color(color.R, color.G, color.B));
		}

		public void SetFramerateLimit(int limit)
		{
			base.SetFramerateLimit((uint) limit);
		}
	}
}
