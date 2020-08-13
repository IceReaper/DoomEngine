namespace DoomEngine.Platform
{
	using Doom.Game;
	using SoftwareRendering;
	using System;

	public interface IRenderer : IDisposable
	{
		public int MaxWindowSize { get; }
		public int WindowSize { get; set; }

		public bool DisplayMessage { get; set; }

		public int MaxGammaCorrectionLevel { get; }
		public int GammaCorrectionLevel { get; set; }
		int WipeBandCount { get; }
		int WipeHeight { get; }
		void InitializeWipe();
		void Render(DoomApplication app);
		void RenderWipe(DoomApplication doomEngine, WipeEffect wipe);
		void RenderGame(DoomGame game);
	}
}
