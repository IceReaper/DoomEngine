namespace DoomEngine.Game.Entities.Keys
{
	using Components.Items;
	using System.Collections.Generic;

	public class RedSkull : EntityInfo
	{
		public RedSkull()
			: base(new List<ComponentInfo> {new ItemComponentInfo(), new LostOnLevelChangeInfo(), new KeyComponentInfo()})
		{
		}
	}
}
