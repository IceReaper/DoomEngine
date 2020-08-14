namespace DoomEngine.Game.Interfaces
{
	using Doom.Game;
	using Doom.World;

	public interface INotifyFire
	{
		public void Fire(World world, Player player);
	}
}
