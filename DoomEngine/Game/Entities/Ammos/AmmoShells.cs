namespace DoomEngine.Game.Entities.Ammos
{
	using Components.Items;
	using Components.Weapons;
	using System.Collections.Generic;

	public class AmmoShells : EntityInfo
	{
		public AmmoShells()
			: base(new List<ComponentInfo> {new ItemComponentInfo(false, 50, 4), new AmmoComponentInfo()})
		{
		}
	}
}
