namespace DoomEngine.Game.Components
{
	public class HealthInfo : ComponentInfo
	{
		public readonly int Full;
		public readonly int Maximum;

		public HealthInfo(int full, int maximum)
		{
			this.Full = full;
			this.Maximum = maximum;
		}

		public override Component Create(Entity entity)
		{
			return new Health(entity, this);
		}
	}

	public class Health : Component
	{
		public readonly HealthInfo Info;

		public int Current;

		public Health(Entity entity, HealthInfo info)
			: base(entity)
		{
			this.Info = info;
			this.Current = info.Full;
		}
	}
}
