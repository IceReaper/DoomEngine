namespace DoomEngine.Game.Components.Weapons
{
	using Items;

	public class RequiresAmmoComponentInfo : ComponentInfo
	{
		public readonly string Ammo;
		public readonly int AmmoPerShot;

		public RequiresAmmoComponentInfo(string ammo, int ammoPerShot)
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

		public bool TryFire(Entity player)
		{
			return player.GetComponent<InventoryComponent>().TryRemove(EntityInfo.OfName(this.Info.Ammo), this.Info.AmmoPerShot);
		}
	}
}
