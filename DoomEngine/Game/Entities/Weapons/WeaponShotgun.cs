namespace DoomEngine.Game.Entities.Weapons
{
	using Ammos;
	using Audio;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponShotgun : EntityInfo
	{
		public WeaponShotgun()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(3, MobjState.Sgunup, MobjState.Sgundown, MobjState.Sgun, MobjState.Sgun1, MobjState.Sgunflash1),
					new RequiresAmmoComponentInfo(nameof(AmmoShells), 1),
					new FireHitscanComponentInfo(Sfx.SHOTGN, 7, 5, false)
				}
			)
		{
		}
	}
}
