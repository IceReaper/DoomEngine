namespace DoomEngine.Game.Components
{
	using Doom.Game;
	using Doom.World;
	using Interfaces;
	using System.Linq;

	public class WeaponComponent : Component
	{
		public readonly int Slot;
		public readonly MobjState UpState;
		public readonly MobjState DownState;
		public readonly MobjState ReadyState;
		public readonly MobjState AttackState;
		public readonly MobjState FlashState;

		public WeaponComponent(int slot, MobjState upState, MobjState downState, MobjState readyState, MobjState attackState, MobjState flashState)
		{
			this.Slot = slot;
			this.UpState = upState;
			this.DownState = downState;
			this.ReadyState = readyState;
			this.AttackState = attackState;
			this.FlashState = flashState;
		}

		public void Fire(World world, Player player, PlayerSpriteDef psp)
		{
			var ammoComponent = this.Entity.GetComponents<AmmoComponent>().FirstOrDefault();

			if (ammoComponent != null && !ammoComponent.TryFire(player))
				return;

			foreach (var iNotifyFire in this.Entity.GetComponents<INotifyFire>())
				iNotifyFire.Fire(world, player);

			// TODO this.FlashState:
			// TODO - Plasmagun + (world.Random.Next() & 1)
			// TODO - Chaingun + psp.State.Number - DoomInfo.States[(int) MobjState.Chain1].Number
			world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Flash, this.FlashState);
		}
	}
}
