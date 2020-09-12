namespace DoomEngine.Game.Entities.Keys
{
	using Components.Items;
	using System.Collections.Generic;

	public class BlueSkull : EntityInfo
	{
		public BlueSkull()
			: base(new List<ComponentInfo> {new ItemComponentInfo(), new LostOnLevelChangeInfo(), new KeyComponentInfo()})
		{
		}
	}
}
