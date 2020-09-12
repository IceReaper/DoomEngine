namespace DoomEngine.Game.Entities.Keys
{
	using Components.Items;
	using System.Collections.Generic;

	public class BlueCard : EntityInfo
	{
		public BlueCard()
			: base(new List<ComponentInfo> {new ItemComponentInfo(), new LostOnLevelChangeInfo(), new KeyComponentInfo()})
		{
		}
	}
}
