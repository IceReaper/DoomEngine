namespace DoomEngine.Game.Entities
{
	using Components.Player;
	using System.Collections.Generic;

	public class Player : EntityInfo
	{
		public Player()
			: base(new List<ComponentInfo> {new InventoryComponentInfo()})
		{
		}
	}
}
