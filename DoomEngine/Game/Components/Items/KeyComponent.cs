namespace DoomEngine.Game.Components.Items
{
	public class KeyComponentInfo : ComponentInfo
	{
		public override Component Create(Entity entity)
		{
			return new KeyComponent(entity);
		}
	}

	public class KeyComponent : Component
	{
		public KeyComponent(Entity entity)
			: base(entity)
		{
		}
	}
}
