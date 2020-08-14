namespace DoomEngine.Game.Entities
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

			return (Entity) Activator.CreateInstance(Entity.types[type]);
		}

		public virtual void Serialize(BinaryWriter writer)
		{
		}

		public virtual void Deserialize(BinaryReader reader)
		{
		}
	}
}
