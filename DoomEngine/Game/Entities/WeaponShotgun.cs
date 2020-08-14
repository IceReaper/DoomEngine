namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponShotgun : Weapon
	{
		public override int Slot => 3;
		public override AmmoType Ammo => AmmoType.Shell;
		public override MobjState UpState => MobjState.Sgunup;
		public override MobjState DownState => MobjState.Sgundown;
		public override MobjState ReadyState => MobjState.Sgun;
		public override MobjState AttackState => MobjState.Sgun1;
		public override MobjState FlashState => MobjState.Sgunflash1;
	}
}
