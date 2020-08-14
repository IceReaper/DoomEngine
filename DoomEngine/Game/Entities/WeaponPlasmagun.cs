namespace DoomEngine.Game.Entities
{
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponPlasmagun : Entity
	{
		public WeaponPlasmagun()
			: base(
				new List<Component>
				{
					new WeaponComponent(6, MobjState.Plasmaup, MobjState.Plasmadown, MobjState.Plasma, MobjState.Plasma1, MobjState.Plasmaflash1),
					new AmmoComponent(AmmoType.Cell, 1),
					new FireProjectileComponent(MobjType.Plasma)
				}
			)
		{
		}
	}
}
