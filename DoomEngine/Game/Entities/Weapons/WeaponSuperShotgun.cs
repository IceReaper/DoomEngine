namespace DoomEngine.Game.Entities.Weapons
{
	using Audio;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponSuperShotgun : EntityInfo
	{
		public WeaponSuperShotgun()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(3, MobjState.Dsgunup, MobjState.Dsgundown, MobjState.Dsgun, MobjState.Dsgun1, MobjState.Dsgunflash1),
					new RequiresAmmoComponentInfo(AmmoType.Shell, 2),
					new FireHitscanComponentInfo(Sfx.DSHTGN, 20, 10, false)
				}
			)
		{
		}
	}
}
