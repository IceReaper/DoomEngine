namespace DoomEngine.Game.Components.Weapons
{
	using Doom.Game;
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

		public bool TryFire(Player player)
		{
			var ammo = player.Inventory.FirstOrDefault(entity => entity.Info.Name == this.Info.Ammo && entity.Components.OfType<AmmoComponent>().Any());

			var component = ammo?.Components.OfType<AmmoComponent>().FirstOrDefault();

			if (component == null)
				return false;

			if (component.Amount < this.Info.AmmoPerShot)
				return false;

			component.Amount -= this.Info.AmmoPerShot;

			if (component.Amount == 0)
				player.Inventory.Remove(ammo);

			return true;
		}
	}
}
