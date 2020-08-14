namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponChainsaw : Weapon
	{
		public override int Slot => 1;
		public override AmmoType Ammo => AmmoType.NoAmmo;
		public override MobjState UpState => MobjState.Sawup;
		public override MobjState DownState => MobjState.Sawdown;
		public override MobjState ReadyState => MobjState.Saw;
		public override MobjState AttackState => MobjState.Saw1;
		public override MobjState FlashState => MobjState.Null;
	}
}
