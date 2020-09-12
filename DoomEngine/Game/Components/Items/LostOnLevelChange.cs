namespace DoomEngine.Game.Components.Items
{
	using Interfaces;

	public class LostOnLevelChangeInfo : ComponentInfo
	{
		public override Component Create(Entity entity)
		{
			return new LostOnLevelChange(entity);
		}
	}

	public class LostOnLevelChange : Component, INotifyLevelChange
	{
		public LostOnLevelChange(Entity entity)
			: base(entity)
		{
		}

		public void LevelChange()
		{
			var item = this.Entity.GetComponent<ItemComponent>();
			item.Inventory.TryRemove(this.Entity.Info, item.Amount);
		}
	}
}
