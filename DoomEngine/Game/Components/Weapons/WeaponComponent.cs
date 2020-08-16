namespace DoomEngine.Game.Components.Weapons
{
	using Doom.Game;
	using Doom.World;
	using Interfaces;
	using System.Linq;

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

		public void Fire(World world, Player player, PlayerSpriteDef psp)
		{
			var ammoComponent = this.Entity.Components.OfType<RequiresAmmoComponent>().FirstOrDefault();

			if (ammoComponent != null && !ammoComponent.TryFire(player))
				return;

			foreach (var iNotifyFire in this.Entity.Components.OfType<INotifyFire>())
				iNotifyFire.Fire(world, player);

			world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Flash, this.Info.FlashState);
		}
	}
}
