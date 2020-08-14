namespace DoomEngine.Game.Entities
{
	using Audio;
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponChaingun : Entity
	{
		public WeaponChaingun()
			: base(
				new List<Component>
				{
					new WeaponComponent(4, MobjState.Chainup, MobjState.Chaindown, MobjState.Chain, MobjState.Chain1, MobjState.Chainflash1),
					new AmmoComponent(AmmoType.Clip, 1),
					new FireHitscanComponent(Sfx.PISTOL, 1, 5, true)
				}
			)
		{
		}
	}
}
