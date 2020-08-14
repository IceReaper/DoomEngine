namespace DoomEngine.Game.Entities
{
	using Audio;
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponPistol : Entity
	{
		public WeaponPistol()
			: base(
				new List<Component>
				{
					new WeaponComponent(2, MobjState.Pistolup, MobjState.Pistoldown, MobjState.Pistol, MobjState.Pistol1, MobjState.Pistolflash),
					new AmmoComponent(AmmoType.Clip, 1),
					new FireHitscanComponent(Sfx.PISTOL, 1, 5, true)
				}
			)
		{
		}
	}
}
