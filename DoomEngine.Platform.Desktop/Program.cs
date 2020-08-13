namespace DoomEngine.Platform.Desktop
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			using var engine = new DoomApplication(new SfmlPlatform(), new CommandLineArgs(args));
			engine.Run();
		}
	}
}
