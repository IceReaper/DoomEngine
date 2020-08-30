namespace DoomEngine.Game
{
	using Doom.World;
	using System.IO;

	public abstract class ComponentInfo
	{
		public readonly string Name;

		protected ComponentInfo()
		{
			this.Name = this.GetType().Name;
		}

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

		public virtual void Deserialize(World world, BinaryReader reader)
		{
		}
	}
}
