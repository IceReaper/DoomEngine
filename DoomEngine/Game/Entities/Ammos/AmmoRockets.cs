namespace DoomEngine.Game.Entities.Ammos
{
	using Components;
	using System.Collections.Generic;

	public class AmmoRockets : EntityInfo
	{
		public AmmoRockets()
			: base(new List<ComponentInfo> {new AmmoComponentInfo(1, 50)})
		{
		}
	}
}
