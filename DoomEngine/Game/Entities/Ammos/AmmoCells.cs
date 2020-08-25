namespace DoomEngine.Game.Entities.Ammos
{
	using Components.Items;
	using Components.Weapons;
	using System.Collections.Generic;

	public class AmmoCells : EntityInfo
	{
		public AmmoCells()
			: base(new List<ComponentInfo> {new ItemComponentInfo(false, 300, 20), new AmmoComponentInfo()})
		{
		}
	}
}
