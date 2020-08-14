namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponPlasmaGun : Weapon
	{
		public override int Slot => 6;
		public override AmmoType Ammo => AmmoType.Cell;
		public override MobjState UpState => MobjState.Plasmaup;
		public override MobjState DownState => MobjState.Plasmadown;
		public override MobjState ReadyState => MobjState.Plasma;
		public override MobjState AttackState => MobjState.Plasma1;
		public override MobjState FlashState => MobjState.Plasmaflash1;
	}
}
