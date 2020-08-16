namespace DoomEngine.Game.Entities.Weapons
{
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponRocketLauncher : EntityInfo
	{
		public WeaponRocketLauncher()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(5, MobjState.Missileup, MobjState.Missiledown, MobjState.Missile, MobjState.Missile1, MobjState.Missileflash1),
					new RequiresAmmoComponentInfo(AmmoType.Missile, 1),
					new FireProjectileComponentInfo(MobjType.Rocket)
				}
			)
		{
		}
	}
}
