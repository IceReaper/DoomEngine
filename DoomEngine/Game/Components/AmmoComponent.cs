namespace DoomEngine.Game.Components
{
	using Doom.Game;
	using Doom.World;

	public class AmmoComponent : Component
	{
		public readonly AmmoType Ammo;
		public readonly int AmmoPerShot;

		public AmmoComponent(AmmoType ammo, int ammoPerShot)
		{
			this.Ammo = ammo;
			this.AmmoPerShot = ammoPerShot;
		}

		public bool TryFire(Player player)
		{
			if (player.Ammo[(int) this.Ammo] < this.AmmoPerShot)
				return false;

			player.Ammo[(int) this.Ammo] -= this.AmmoPerShot;

			return true;
		}
	}
}
