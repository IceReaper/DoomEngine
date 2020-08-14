namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponPistol : Weapon
	{
		public override int Slot => 2;
		public override AmmoType Ammo => AmmoType.Clip;
		public override MobjState UpState => MobjState.Pistolup;
		public override MobjState DownState => MobjState.Pistoldown;
		public override MobjState ReadyState => MobjState.Pistol;
		public override MobjState AttackState => MobjState.Pistol1;
		public override MobjState FlashState => MobjState.Pistolflash;
	}
}
