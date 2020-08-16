namespace DoomEngine.Game
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public abstract class EntityInfo
	{
		public readonly IEnumerable<ComponentInfo> ComponentInfos;
		public readonly string Name;

		protected EntityInfo(IEnumerable<ComponentInfo> componentInfos)
		{
			this.ComponentInfos = componentInfos;
			this.Name = this.GetType().Name;
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
			return new Entity(EntityInfo.entityInfos.First(info => info.Value.GetType() == typeof(T)).Value);
		}

		public static Entity Create(EntityInfo info)
		{
			return new Entity(info);
		}

		public static IEnumerable<EntityInfo> WithComponent<T>() where T : ComponentInfo
		{
			return EntityInfo.entityInfos.Values.Where(entityInfo => entityInfo.ComponentInfos.Any(componentInfo => componentInfo.GetType() == typeof(T)));
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
		public readonly EntityInfo Info;
		public readonly IEnumerable<Component> Components;

		public Entity(EntityInfo info)
		{
			this.Info = info;
			this.Components = info.ComponentInfos.Select(componentInfo => componentInfo.Create(this)).ToArray();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(this.Info.Name);

			foreach (var component in this.Components)
				component.Serialize(writer);
		}

		public static Entity Deserialize(BinaryReader reader)
		{
			var entity = new Entity(EntityInfo.OfName(reader.ReadString()));

			foreach (var component in entity.Components)
				component.Deserialize(reader);

			return entity;
		}
	}
}
