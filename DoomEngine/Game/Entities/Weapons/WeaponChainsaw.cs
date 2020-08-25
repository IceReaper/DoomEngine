namespace DoomEngine.Game.Entities.Weapons
{
	using Audio;
	using Components.Items;
	using Components.Weapons;
	using Doom.Math;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponChainsaw : EntityInfo
	{
		public WeaponChainsaw()
			: base(
				new List<ComponentInfo>
				{
					new ItemComponentInfo(),
					new WeaponComponentInfo(1, MobjState.Sawup, MobjState.Sawdown, MobjState.Saw, MobjState.Saw1, MobjState.Null),
					new FireMeleeComponentInfo(WeaponBehavior.MeleeRange + Fixed.Epsilon, Sfx.SAWFUL, Sfx.SAWHIT, false, true)
				}
			)
		{
		}
	}
}
