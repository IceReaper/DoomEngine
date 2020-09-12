namespace DoomEngine.Game.Components.Items
{
	using Entities;
	using System.IO;
	using World = Doom.World.World;

	public class ItemComponentInfo : ComponentInfo
	{
		public readonly bool AllowMultiple;
		public readonly int StackSize;
		public readonly int InitialAmount;

		public ItemComponentInfo(bool allowMultiple = false, int stackSize = 1, int initialAmount = 1)
		{
			this.AllowMultiple = allowMultiple;
			this.StackSize = stackSize;
			this.InitialAmount = initialAmount;
		}

		public override Component Create(Entity entity)
		{
			return new ItemComponent(entity, this);
		}
	}

	public class ItemComponent : Component
	{
		public readonly ItemComponentInfo Info;

		public int Amount;
		public InventoryComponent Inventory;

		public ItemComponent(Entity entity, ItemComponentInfo info)
			: base(entity)
		{
			this.Info = info;
			this.Amount = info.InitialAmount;
		}

		public override void Serialize(BinaryWriter writer)
		{
			writer.Write(this.Amount);
		}

		public override void Deserialize(World world, BinaryReader reader)
		{
			this.Amount = reader.ReadInt32();
		}
	}
}
