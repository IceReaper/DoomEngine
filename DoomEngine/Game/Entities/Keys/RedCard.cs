namespace DoomEngine.Game.Entities.Keys
{
	using Components.Items;
	using System.Collections.Generic;

	public class RedCard : EntityInfo
	{
		public RedCard()
			: base(new List<ComponentInfo> {new ItemComponentInfo(), new LostOnLevelChangeInfo(), new KeyComponentInfo()})
		{
		}
	}
}
