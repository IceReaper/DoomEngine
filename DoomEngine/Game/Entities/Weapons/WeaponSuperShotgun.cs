namespace DoomEngine.Game.Entities.Weapons
{
	using Ammos;
	using Audio;
	using Components.Items;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponSuperShotgun : EntityInfo
	{
		public WeaponSuperShotgun()
			: base(
				new List<ComponentInfo>
				{
					new ItemComponentInfo(),
					new WeaponComponentInfo(3, MobjState.Dsgunup, MobjState.Dsgundown, MobjState.Dsgun, MobjState.Dsgun1, MobjState.Dsgunflash1),
					new RequiresAmmoComponentInfo(nameof(AmmoShells), 2),
					new FireHitscanComponentInfo(Sfx.DSHTGN, 20, 10, false)
				}
			)
		{
		}
	}
}
