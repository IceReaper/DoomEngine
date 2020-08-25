namespace DoomEngine.Game.Entities.Weapons
{
	using Ammos;
	using Audio;
	using Components.Items;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponChaingun : EntityInfo
	{
		public WeaponChaingun()
			: base(
				new List<ComponentInfo>
				{
					new ItemComponentInfo(),
					new WeaponComponentInfo(4, MobjState.Chainup, MobjState.Chaindown, MobjState.Chain, MobjState.Chain1, MobjState.Chainflash1),
					new RequiresAmmoComponentInfo(nameof(AmmoBullets), 1),
					new FireHitscanComponentInfo(Sfx.PISTOL, 1, 5, true)
				}
			)
		{
		}
	}
}
