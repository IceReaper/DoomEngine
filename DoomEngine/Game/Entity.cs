namespace DoomEngine.Game
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public abstract class EntityInfo
	{
		private readonly IEnumerable<ComponentInfo> componentInfos;

		public readonly string Name;

		protected EntityInfo(IEnumerable<ComponentInfo> componentInfos)
		{
			this.componentInfos = componentInfos;
			this.Name = this.GetType().Name;
		}

		public T GetComponentInfo<T>() where T : ComponentInfo
		{
			return this.GetComponentInfos<T>().FirstOrDefault();
		}

		public IEnumerable<T> GetComponentInfos<T>() where T : ComponentInfo
		{
			return this.componentInfos.OfType<T>();
		}

		private static readonly Dictionary<string, EntityInfo> entityInfos;

		static EntityInfo()
		{
			EntityInfo.entityInfos = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(EntityInfo))))
				.ToDictionary(type => type.Name, type => (EntityInfo) type.GetConstructor(new Type[0])?.Invoke(new object[0]));
		}

		public static Entity Create<T>() where T : EntityInfo
		{
			return EntityInfo.Create(EntityInfo.entityInfos.First(info => info.Value.GetType() == typeof(T)).Value);
		}

		public static Entity Create(EntityInfo info)
		{
			return new Entity(info, entity => info.componentInfos.Select(componentInfo => componentInfo.Create(entity)).ToArray());
		}

		public static IEnumerable<EntityInfo> WithComponent<T>() where T : ComponentInfo
		{
			return EntityInfo.entityInfos.Values.Where(entityInfo => entityInfo.componentInfos.Any(componentInfo => componentInfo.GetType() == typeof(T)));
		}

		public static EntityInfo OfName(string name)
		{
			return EntityInfo.entityInfos.ContainsKey(name) ? EntityInfo.entityInfos[name] : null;
		}

		public static EntityInfo OfType<T>() where T : EntityInfo
		{
			return EntityInfo.entityInfos.Values.FirstOrDefault(entityInfo => entityInfo.GetType() == typeof(T));
		}
	}

	public sealed class Entity
	{
		private readonly IEnumerable<Component> components;

		public readonly EntityInfo Info;

		public Entity(EntityInfo info, Func<Entity, Component[]> createComponents)
		{
			this.Info = info;
			this.components = createComponents(this);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(this.Info.Name);

			foreach (var component in this.components)
				component.Serialize(writer);
		}

		public static Entity Deserialize(BinaryReader reader)
		{
			var entity = EntityInfo.Create(EntityInfo.OfName(reader.ReadString()));

			foreach (var component in entity.components)
				component.Deserialize(reader);

			return entity;
		}

		public T GetComponent<T>() where T : Component
		{
			return this.GetComponents<T>().FirstOrDefault();
		}

		public IEnumerable<T> GetComponents<T>()
		{
			return this.components.OfType<T>();
		}
	}
}
