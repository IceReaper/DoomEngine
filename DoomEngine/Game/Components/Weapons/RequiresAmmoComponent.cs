namespace DoomEngine.Game.Components.Weapons
{
	using Doom.Game;
	using Doom.World;

	public class RequiresAmmoComponentInfo : ComponentInfo
	{
		public readonly AmmoType Ammo;
		public readonly int AmmoPerShot;

		public RequiresAmmoComponentInfo(AmmoType ammo, int ammoPerShot)
		{
			this.Ammo = ammo;
			this.AmmoPerShot = ammoPerShot;
		}

		public override Component Create(Entity entity)
		{
			return new RequiresAmmoComponent(entity, this);
		}
	}

	public class RequiresAmmoComponent : Component
	{
		public readonly RequiresAmmoComponentInfo Info;

		public RequiresAmmoComponent(Entity entity, RequiresAmmoComponentInfo info)
			: base(entity)
		{
			this.Info = info;
		}

		public bool TryFire(Player player)
		{
			if (player.Ammo[(int) this.Info.Ammo] < this.Info.AmmoPerShot)
				return false;

			player.Ammo[(int) this.Info.Ammo] -= this.Info.AmmoPerShot;

			return true;
		}
	}
}
