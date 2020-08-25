namespace DoomEngine.Game.Entities
{
	using Components;
	using Components.Items;
	using System.Collections.Generic;

	public class Player : EntityInfo
	{
		public Player()
			: base(new List<ComponentInfo> {new HealthInfo(100, 200), new InventoryComponentInfo()})
		{
		}
	}
}
