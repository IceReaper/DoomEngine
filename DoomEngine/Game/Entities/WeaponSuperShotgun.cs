namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponSuperShotgun : Weapon
	{
		public override int Slot => 3;
		public override AmmoType Ammo => AmmoType.Shell;
		public override MobjState UpState => MobjState.Dsgunup;
		public override MobjState DownState => MobjState.Dsgundown;
		public override MobjState ReadyState => MobjState.Dsgun;
		public override MobjState AttackState => MobjState.Dsgun1;
		public override MobjState FlashState => MobjState.Dsgunflash1;
	}
}
