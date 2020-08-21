namespace DoomEngine.Game.Components.Weapons
{
	using Doom.Game;
	using Doom.World;
	using Interfaces;

	public class WeaponComponentInfo : ComponentInfo
	{
		public readonly int Slot;
		public readonly MobjState UpState;
		public readonly MobjState DownState;
		public readonly MobjState ReadyState;
		public readonly MobjState AttackState;
		public readonly MobjState FlashState;

		public WeaponComponentInfo(int slot, MobjState upState, MobjState downState, MobjState readyState, MobjState attackState, MobjState flashState)
		{
			this.Slot = slot;
			this.UpState = upState;
			this.DownState = downState;
			this.ReadyState = readyState;
			this.AttackState = attackState;
			this.FlashState = flashState;
		}

		public override Component Create(Entity entity)
		{
			return new WeaponComponent(entity, this);
		}
	}

	public class WeaponComponent : Component
	{
		public readonly WeaponComponentInfo Info;

		public WeaponComponent(Entity entity, WeaponComponentInfo info)
			: base(entity)
		{
			this.Info = info;
		}

		public void Fire(World world, Player player)
		{
			var ammoComponent = this.Entity.GetComponent<RequiresAmmoComponent>();

			if (ammoComponent != null && !ammoComponent.TryFire(player.Entity))
				return;

			foreach (var iNotifyFire in this.Entity.GetComponents<INotifyFire>())
				iNotifyFire.Fire(world, player);

			world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Flash, this.Info.FlashState);
		}
	}
}
