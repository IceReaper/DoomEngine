namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponBfg : Weapon
	{
		public override int Slot => 7;
		public override AmmoType Ammo => AmmoType.Cell;
		public override MobjState UpState => MobjState.Bfgup;
		public override MobjState DownState => MobjState.Bfgdown;
		public override MobjState ReadyState => MobjState.Bfg;
		public override MobjState AttackState => MobjState.Bfg1;
		public override MobjState FlashState => MobjState.Bfgflash1;
	}
}
