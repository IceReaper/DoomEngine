namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public class WeaponChaingun : Weapon
	{
		public override int Slot => 4;
		public override AmmoType Ammo => AmmoType.Clip;
		public override MobjState UpState => MobjState.Chainup;
		public override MobjState DownState => MobjState.Chaindown;
		public override MobjState ReadyState => MobjState.Chain;
		public override MobjState AttackState => MobjState.Chain1;
		public override MobjState FlashState => MobjState.Chainflash1;
	}
}
