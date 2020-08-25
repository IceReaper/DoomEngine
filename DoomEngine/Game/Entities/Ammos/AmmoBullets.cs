namespace DoomEngine.Game.Entities.Ammos
{
	using Components.Items;
	using Components.Weapons;
	using System.Collections.Generic;

	public class AmmoBullets : EntityInfo
	{
		public AmmoBullets()
			: base(new List<ComponentInfo> {new ItemComponentInfo(false, 200, 10), new AmmoComponentInfo()})
		{
		}
	}
}
