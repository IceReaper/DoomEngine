namespace DoomEngine.Game.Entities.Ammos
{
	using Components;
	using System.Collections.Generic;

	public class AmmoShells : EntityInfo
	{
		public AmmoShells()
			: base(new List<ComponentInfo> {new AmmoComponentInfo(4, 50)})
		{
		}
	}
}
