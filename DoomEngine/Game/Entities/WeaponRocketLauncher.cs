namespace DoomEngine.Game.Entities
{
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponRocketLauncher : Entity
	{
		public WeaponRocketLauncher()
			: base(
				new List<Component>
				{
					new WeaponComponent(5, MobjState.Missileup, MobjState.Missiledown, MobjState.Missile, MobjState.Missile1, MobjState.Missileflash1),
					new AmmoComponent(AmmoType.Missile, 1),
					new FireProjectileComponent(MobjType.Rocket)
				}
			)
		{
		}
	}
}
