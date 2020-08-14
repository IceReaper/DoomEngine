namespace DoomEngine.Game.Entities
{
	using Doom.World;

	public abstract class Weapon : Entity
	{
		public abstract AmmoType Ammo { get; }
		public abstract MobjState UpState { get; }
		public abstract MobjState DownState { get; }
		public abstract MobjState ReadyState { get; }
		public abstract MobjState AttackState { get; }
		public abstract MobjState FlashState { get; }
		public abstract int Slot { get; }
	}
}
