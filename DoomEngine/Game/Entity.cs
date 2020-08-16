namespace DoomEngine.Game
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public abstract class EntityInfo
	{
		public readonly IEnumerable<ComponentInfo> Components;

		protected EntityInfo(IEnumerable<ComponentInfo> components)
		{
			this.Components = components;
		}
	}

	public sealed class Entity
	{
		public static readonly IEnumerable<EntityInfo> EntityInfos;

		static Entity()
		{
			Entity.EntityInfos = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(EntityInfo))))
				.Select(type => (EntityInfo) type.GetConstructor(new Type[0])?.Invoke(new object[0]))
				.ToArray();
		}

		public static Entity Create<T>() where T : EntityInfo
		{
			return new Entity(Entity.EntityInfos.First(info => info.GetType() == typeof(T)));
		}

		public static Entity Create(EntityInfo info)
		{
			return new Entity(info);
		}

		public readonly EntityInfo Info;
		public readonly IEnumerable<Component> Components;

		private Entity(EntityInfo info)
		{
			this.Info = info;
			this.Components = info.Components.Select(componentInfo => componentInfo.Create(this));
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(this.Info.GetType().Name);

			foreach (var component in this.Components)
				component.Serialize(writer);
		}

		public static Entity Deserialize(BinaryReader reader)
		{
			var type = reader.ReadString();
			var entity = new Entity(Entity.EntityInfos.First(info => info.GetType().Name == type));

			foreach (var component in entity.Components)
				component.Deserialize(reader);

			return entity;
		}
	}
}
