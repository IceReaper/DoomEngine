namespace DoomEngine.Game.Entities.Keys
{
	using Components.Items;
	using System.Collections.Generic;

	public class YellowCard : EntityInfo
	{
		public YellowCard()
			: base(new List<ComponentInfo> {new ItemComponentInfo(), new LostOnLevelChangeInfo(), new KeyComponentInfo()})
		{
		}
	}
}
