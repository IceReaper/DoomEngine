namespace DoomEngine.Game.Components.Items
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class InventoryComponentInfo : ComponentInfo
	{
		public override Component Create(Entity entity)
		{
			return new InventoryComponent(entity);
		}
	}

	public class InventoryComponent : Component
	{
		private readonly List<Entity> items = new List<Entity>();

		public IEnumerable<Entity> Items => this.items.ToArray();

		public InventoryComponent(Entity entity)
			: base(entity)
		{
		}

		public bool TryAdd(Entity item)
		{
			var itemComponent = item.GetComponent<ItemComponent>();

			if (itemComponent == null)
				return false;

			if (itemComponent.Amount == 0)
				return true;

			var ownedItems = this.items.Where(ownedItem => ownedItem.Info == item.Info).ToArray();
			var anyAdded = false;

			foreach (var ownedItem in ownedItems)
			{
				var ownedItemComponent = ownedItem.GetComponent<ItemComponent>();
				var availableStackSize = ownedItemComponent.Info.StackSize - ownedItemComponent.Amount;

				if (availableStackSize <= 0)
					continue;

				var transferAmount = Math.Min(itemComponent.Amount, availableStackSize);
				itemComponent.Amount -= transferAmount;
				ownedItemComponent.Amount += transferAmount;
				anyAdded = true;

				if (itemComponent.Amount == 0)
					return true;
			}

			if (ownedItems.Length > 0 && !itemComponent.Info.AllowMultiple)
				return anyAdded;

			this.items.Add(item);

			return true;
		}

		public bool TryRemove(EntityInfo itemInfo, int amount)
		{
			var ownedItems = this.items.Where(ownedItem => ownedItem.Info == itemInfo).ToArray();
			var ownedAmount = ownedItems.Sum(ownedItem => ownedItem.GetComponent<ItemComponent>().Amount);

			if (ownedAmount < amount)
				return false;

			foreach (var ownedItem in ownedItems)
			{
				var itemComponent = ownedItem.GetComponent<ItemComponent>();

				var consume = Math.Min(itemComponent.Amount, amount);
				itemComponent.Amount -= consume;
				amount -= consume;

				if (itemComponent.Amount == 0)
					this.items.Remove(ownedItem);

				if (amount == 0)
					break;
			}

			return true;
		}

		public override void Serialize(BinaryWriter writer)
		{
			writer.Write(this.items.Count);
			this.items.ForEach(item => item.Serialize(writer));
		}

		public override void Deserialize(BinaryReader reader)
		{
			var numItems = reader.ReadInt32();

			for (var i = 0; i < numItems; i++)
				this.items.Add(Entity.Deserialize(reader));
		}
	}
}
