namespace DoomEngine.Game
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public abstract class Entity
	{
		private static readonly Dictionary<string, Type> types;

		static Entity()
		{
			Entity.types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Entity))))
				.ToDictionary(type => type.Name, type => type);
		}

		public static Entity Create(string type)
		{
			if (!Entity.types.ContainsKey(type))
				return null;

			return (Entity) Entity.types[type].GetConstructor(new Type[0])?.Invoke(new object[0]);
		}

		private readonly List<Component> components = new List<Component>();

		protected Entity(IEnumerable<Component> components)
		{
			this.components.AddRange(components);
			this.components.ForEach(c => c.Entity = this);
		}

		public IEnumerable<T> GetComponents<T>()
		{
			return this.components.OfType<T>();
		}

		public bool HasComponents<T>()
		{
			return this.components.OfType<T>().Any();
		}

		public void Serialize(BinaryWriter writer)
		{
			this.components.ForEach(iComponent => iComponent.Serialize(writer));
		}

		public void Deserialize(BinaryReader reader)
		{
			this.components.ForEach(iComponent => iComponent.Deserialize(reader));
		}
	}
}
