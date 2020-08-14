namespace DoomEngine.Game.Components
{
	using Doom.Game;
	using Doom.World;
	using Interfaces;

	public class FireProjectileComponent : Component, INotifyFire
	{
		public readonly MobjType Projectile;

		public FireProjectileComponent(MobjType projectile)
		{
			this.Projectile = projectile;
		}

		void INotifyFire.Fire(World world, Player player)
		{
			world.ThingAllocation.SpawnPlayerMissile(player.Mobj, this.Projectile);
		}
	}
}
