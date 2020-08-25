namespace DoomEngine.Game.Entities.Weapons
{
	using Ammos;
	using Components.Items;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponRocketLauncher : EntityInfo
	{
		public WeaponRocketLauncher()
			: base(
				new List<ComponentInfo>
				{
					new ItemComponentInfo(),
					new WeaponComponentInfo(5, MobjState.Missileup, MobjState.Missiledown, MobjState.Missile, MobjState.Missile1, MobjState.Missileflash1),
					new RequiresAmmoComponentInfo(nameof(AmmoRockets), 1),
					new FireProjectileComponentInfo(MobjType.Rocket)
				}
			)
		{
		}
	}
}
