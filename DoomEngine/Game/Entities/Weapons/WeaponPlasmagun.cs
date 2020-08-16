namespace DoomEngine.Game.Entities.Weapons
{
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponPlasmagun : EntityInfo
	{
		public WeaponPlasmagun()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(6, MobjState.Plasmaup, MobjState.Plasmadown, MobjState.Plasma, MobjState.Plasma1, MobjState.Plasmaflash1),
					new RequiresAmmoComponentInfo(AmmoType.Cell, 1),
					new FireProjectileComponentInfo(MobjType.Plasma)
				}
			)
		{
		}
	}
}
