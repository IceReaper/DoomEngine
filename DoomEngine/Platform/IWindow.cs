namespace DoomEngine.Platform
{
	using System;
	using System.Drawing;
	using UserInput;

	public interface IWindow : IDisposable
	{
		event EventHandler Closed;
		event EventHandler<DoomKey> KeyPressed;
		event EventHandler<DoomKey> KeyReleased;

		bool IsOpen { get; }
		void DispatchEvents();
		void Display();
		void Close();
		void Clear(Color color);
		void SetFramerateLimit(int limit);
	}
}
