namespace DoomEngine.Game.Entities
{
	using Audio;
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponShotgun : Entity
	{
		public WeaponShotgun()
			: base(
				new List<Component>
				{
					new WeaponComponent(3, MobjState.Sgunup, MobjState.Sgundown, MobjState.Sgun, MobjState.Sgun1, MobjState.Sgunflash1),
					new AmmoComponent(AmmoType.Shell, 1),
					new FireHitscanComponent(Sfx.SHOTGN, 7, 5, false)
				}
			)
		{
		}
	}
}
