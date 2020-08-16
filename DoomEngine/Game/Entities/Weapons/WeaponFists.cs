namespace DoomEngine.Game.Entities.Weapons
{
	using Audio;
	using Components.Weapons;
	using Doom.World;
	using System.Collections.Generic;

	public class WeaponFists : EntityInfo
	{
		public WeaponFists()
			: base(
				new List<ComponentInfo>
				{
					new WeaponComponentInfo(1, MobjState.Punchup, MobjState.Punchdown, MobjState.Punch, MobjState.Punch1, MobjState.Null),
					new FireMeleeComponentInfo(WeaponBehavior.MeleeRange, Sfx.NONE, Sfx.PUNCH, true, false)
				}
			)
		{
		}
	}
}
