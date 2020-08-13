namespace DoomEngine.Platform
{
	using Doom.Game;
	using System;

	public interface IUserInput : IDisposable
	{
		public int MaxMouseSensitivity { get; }
		public int MouseSensitivity { get; set; }
		void Reset();
		void BuildTicCmd(TicCmd cmd);
	}
}
