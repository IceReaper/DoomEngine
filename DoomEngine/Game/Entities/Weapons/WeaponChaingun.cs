namespace DoomEngine.Game.Entities.Weapons
{
	using Audio;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponChaingun : EntityInfo
	{
		public WeaponChaingun()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(4, MobjState.Chainup, MobjState.Chaindown, MobjState.Chain, MobjState.Chain1, MobjState.Chainflash1),
					new RequiresAmmoComponentInfo(AmmoType.Clip, 1),
					new FireHitscanComponentInfo(Sfx.PISTOL, 1, 5, true)
				}
			)
		{
		}
	}
}
