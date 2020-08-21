namespace DoomEngine.Game.Components.Player
{
	using System.Collections.Generic;
	using System.IO;

	public class InventoryComponentInfo : ComponentInfo
	{
		public override Component Create(Entity entity)
		{
			return new InventoryComponent(entity, this);
		}
	}

	public class InventoryComponent : Component
	{
		public readonly InventoryComponentInfo Info;
		public readonly List<Entity> Items = new List<Entity>();

		public InventoryComponent(Entity entity, InventoryComponentInfo info)
			: base(entity)
		{
			this.Info = info;
		}

		public override void Serialize(BinaryWriter writer)
		{
			writer.Write(this.Items.Count);
			this.Items.ForEach(item => item.Serialize(writer));
		}

		public override void Deserialize(BinaryReader reader)
		{
			var items = reader.ReadInt32();

			for (var i = 0; i < items; i++)
				this.Items.Add(Entity.Deserialize(reader));
		}
	}
}
