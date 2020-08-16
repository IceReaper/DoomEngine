namespace DoomEngine.Game.Entities.Ammos
{
	using Components;
	using System.Collections.Generic;

	public class AmmoBullets : EntityInfo
	{
		public AmmoBullets()
			: base(new List<ComponentInfo> {new AmmoComponentInfo(10, 200)})
		{
		}
	}
}
