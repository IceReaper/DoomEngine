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
		public readonly FireProjectileComponentInfo Info;

		public FireProjectileComponent(Entity entity, FireProjectileComponentInfo info)
			: base(entity)
		{
			this.Info = info;
		}

		void INotifyFire.Fire(World world, Player player)
		{
			world.ThingAllocation.SpawnPlayerMissile(player.Mobj, this.Info.Projectile);
		}
	}
}
