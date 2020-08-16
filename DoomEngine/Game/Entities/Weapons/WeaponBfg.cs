namespace DoomEngine.Game.Entities.Weapons
{
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponBfg : EntityInfo
	{
		public WeaponBfg()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(7, MobjState.Bfgup, MobjState.Bfgdown, MobjState.Bfg, MobjState.Bfg1, MobjState.Bfgflash1),
					new RequiresAmmoComponentInfo(AmmoType.Cell, 40),
					new FireProjectileComponentInfo(MobjType.Bfg)
				}
			)
		{
		}
	}
}
