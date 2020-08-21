namespace DoomEngine.Game.Components.Weapons
{
	using Player;
	using System.Linq;

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
			var inventory = player.GetComponent<InventoryComponent>();
			var ammo = inventory.Items.FirstOrDefault(entity => entity.Info.Name == this.Info.Ammo && entity.GetComponent<AmmoComponent>() != null);
			var component = ammo?.GetComponent<AmmoComponent>();

			if (component == null)
				return false;

			if (component.Amount < this.Info.AmmoPerShot)
				return false;

			component.Amount -= this.Info.AmmoPerShot;

			if (component.Amount == 0)
				inventory.Items.Remove(ammo);

			return true;
		}
	}
}
