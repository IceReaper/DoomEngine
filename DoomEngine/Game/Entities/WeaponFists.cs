namespace DoomEngine.Game.Entities
{
	using Components;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponFists : Entity
	{
		public WeaponFists()
			: base(
				new List<Component>
				{
					new WeaponComponent(1, MobjState.Punchup, MobjState.Punchdown, MobjState.Punch, MobjState.Punch1, MobjState.Null),
					new FireMeleeComponent()
				}
			)
		{
		}
	}
}
