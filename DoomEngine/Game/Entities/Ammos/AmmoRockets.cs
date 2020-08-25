namespace DoomEngine.Game.Entities.Ammos
{
	using Components.Items;
	using Components.Weapons;
	using System.Collections.Generic;

	public class AmmoRockets : EntityInfo
	{
		public AmmoRockets()
			: base(new List<ComponentInfo> {new ItemComponentInfo(false, 50), new AmmoComponentInfo()})
		{
		}
	}
}
