namespace DoomEngine.Game.Entities.Ammos
{
	using Components;
	using System.Collections.Generic;

	public class AmmoCells : EntityInfo
	{
		public AmmoCells()
			: base(new List<ComponentInfo> {new AmmoComponentInfo(20, 300)})
		{
		}
	}
}
