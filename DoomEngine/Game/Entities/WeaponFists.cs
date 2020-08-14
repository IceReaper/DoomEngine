namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponFists : Weapon
	{
		public override int Slot => 1;
		public override AmmoType Ammo => AmmoType.NoAmmo;
		public override MobjState UpState => MobjState.Punchup;
		public override MobjState DownState => MobjState.Punchdown;
		public override MobjState ReadyState => MobjState.Punch;
		public override MobjState AttackState => MobjState.Punch1;
		public override MobjState FlashState => MobjState.Null;
	}
}
