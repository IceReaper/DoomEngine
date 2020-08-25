namespace DoomEngine.Game.Entities.Weapons
{
	using Ammos;
	using Components.Items;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponBfg : EntityInfo
	{
		public WeaponBfg()
			: base(
				new List<ComponentInfo>
				{
					new ItemComponentInfo(),
					new WeaponComponentInfo(7, MobjState.Bfgup, MobjState.Bfgdown, MobjState.Bfg, MobjState.Bfg1, MobjState.Bfgflash1),
					new RequiresAmmoComponentInfo(nameof(AmmoCells), 40),
					new FireProjectileComponentInfo(MobjType.Bfg)
				}
			)
		{
		}
	}
}
