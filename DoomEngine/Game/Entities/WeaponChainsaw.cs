namespace DoomEngine.Game.Entities
{
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponChainsaw : Entity
	{
		public WeaponChainsaw()
			: base(
				new List<Component>
				{
					new WeaponComponent(1, MobjState.Sawup, MobjState.Sawdown, MobjState.Saw, MobjState.Saw1, MobjState.Null), new FireMeleeComponent()
				}
			)
		{
		}
	}
}
