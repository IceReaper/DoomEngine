namespace DoomEngine.Game.Entities
{
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponBfg : Entity
	{
		public WeaponBfg()
			: base(
				new List<Component>
				{
					new WeaponComponent(7, MobjState.Bfgup, MobjState.Bfgdown, MobjState.Bfg, MobjState.Bfg1, MobjState.Bfgflash1),
					new AmmoComponent(AmmoType.Cell, 40),
					new FireProjectileComponent(MobjType.Bfg)
				}
			)
		{
		}
	}
}
