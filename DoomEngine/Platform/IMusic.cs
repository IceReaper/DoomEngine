namespace DoomEngine.Platform
{
	using Audio;
	using System;

	public interface IMusic : IDisposable
	{
		void StartMusic(Bgm bgm, bool loop);

		public int MaxVolume { get; }
		public int Volume { get; set; }
	}
}
