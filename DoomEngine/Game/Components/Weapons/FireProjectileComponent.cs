namespace DoomEngine.Game.Components.Weapons
{
	using Doom.Game;
	using Doom.World;
	using Interfaces;

	public class FireProjectileComponentInfo : ComponentInfo
	{
		public readonly MobjType Projectile;

		public FireProjectileComponentInfo(MobjType projectile)
		{
			this.Projectile = projectile;
		}

		public override Component Create(Entity entity)
		{
			return new FireProjectileComponent(entity, this);
		}
	}

	public class FireProjectileComponent : Component, INotifyFire
	{
		private readonly FireProjectileComponentInfo info;

		public FireProjectileComponent(Entity entity, FireProjectileComponentInfo info)
			: base(entity)
		{
			this.info = info;
		}

		void INotifyFire.Fire(World world, Player player)
		{
			world.ThingAllocation.SpawnPlayerMissile(player.Mobj, this.info.Projectile);
		}
	}
}
