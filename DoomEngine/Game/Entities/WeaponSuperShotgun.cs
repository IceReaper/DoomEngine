namespace DoomEngine.Game.Entities
{
	using Audio;
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponSuperShotgun : Entity
	{
		public WeaponSuperShotgun()
			: base(
				new List<Component>
				{
					new WeaponComponent(3, MobjState.Dsgunup, MobjState.Dsgundown, MobjState.Dsgun, MobjState.Dsgun1, MobjState.Dsgunflash1),
					new AmmoComponent(AmmoType.Shell, 2),
					new FireHitscanComponent(Sfx.DSHTGN, 20, 10, false)
				}
			)
		{
		}
	}
}
