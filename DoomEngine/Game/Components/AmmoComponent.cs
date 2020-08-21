namespace DoomEngine.Game.Components
{
	using System.IO;

	public class AmmoComponentInfo : ComponentInfo
	{
		public readonly int ClipSize;
		public readonly int Maximum;

		public AmmoComponentInfo(int clipSize, int maximum)
		{
			this.ClipSize = clipSize;
			this.Maximum = maximum;
		}

		public override Component Create(Entity entity)
		{
			return new AmmoComponent(entity, this);
		}
	}

	public class AmmoComponent : Component
	{
		public readonly AmmoComponentInfo Info;

		public int Amount;

		public AmmoComponent(Entity entity, AmmoComponentInfo info)
			: base(entity)
		{
			this.Info = info;
			this.Amount = info.ClipSize;
		}

		public override void Serialize(BinaryWriter writer)
		{
			writer.Write(this.Amount);
		}

		public override void Deserialize(BinaryReader reader)
		{
			this.Amount = reader.ReadInt32();
		}
	}
}
