namespace DoomEngine.Game
{
	using System.IO;

	public abstract class ComponentInfo
	{
		public abstract Component Create(Entity entity);
	}

	public abstract class Component
	{
		protected readonly Entity Entity;

		protected Component(Entity entity)
		{
			this.Entity = entity;
		}

		public virtual void Serialize(BinaryWriter writer)
		{
		}

		public virtual void Deserialize(BinaryReader reader)
		{
		}
	}
}
