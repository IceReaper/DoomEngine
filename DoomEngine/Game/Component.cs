namespace DoomEngine.Game
{
	using System.IO;

	public abstract class Component
	{
		public Entity Entity { get; set; }

		public virtual void Serialize(BinaryWriter binaryWriter)
		{
		}

		public virtual void Deserialize(BinaryReader binaryReader)
		{
		}
	}
}
