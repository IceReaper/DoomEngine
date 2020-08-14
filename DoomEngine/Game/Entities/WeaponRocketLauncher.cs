namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponRocketLauncher : Weapon
	{
		public override int Slot => 5;
		public override AmmoType Ammo => AmmoType.Missile;
		public override MobjState UpState => MobjState.Missileup;
		public override MobjState DownState => MobjState.Missiledown;
		public override MobjState ReadyState => MobjState.Missile;
		public override MobjState AttackState => MobjState.Missile1;
		public override MobjState FlashState => MobjState.Missileflash1;
	}
}
