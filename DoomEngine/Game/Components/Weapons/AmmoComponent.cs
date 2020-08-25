namespace DoomEngine.Game.Components.Weapons
{
	public class AmmoComponentInfo : ComponentInfo
	{
		public override Component Create(Entity entity)
		{
			return new AmmoComponent(entity);
		}
	}

	public class AmmoComponent : Component
	{
		public AmmoComponent(Entity entity)
			: base(entity)
		{
		}
	}
}
