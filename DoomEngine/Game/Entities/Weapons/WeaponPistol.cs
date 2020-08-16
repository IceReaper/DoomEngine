namespace DoomEngine.Game.Entities.Weapons
{
	using Ammos;
	using Audio;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponPistol : EntityInfo
	{
		public WeaponPistol()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(2, MobjState.Pistolup, MobjState.Pistoldown, MobjState.Pistol, MobjState.Pistol1, MobjState.Pistolflash),
					new RequiresAmmoComponentInfo(nameof(AmmoBullets), 1),
					new FireHitscanComponentInfo(Sfx.PISTOL, 1, 5, true)
				}
			)
		{
		}
	}
}
