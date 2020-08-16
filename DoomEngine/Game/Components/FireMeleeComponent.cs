namespace DoomEngine.Game.Components
{
	using Audio;
	using Doom.Game;
	using Doom.Math;
	using Doom.World;
	using Interfaces;

	public class FireMeleeComponent : Component, INotifyFire
	{
		public readonly Fixed Range;
		public readonly Sfx MissSound;
		public readonly Sfx HitSound;
		public readonly bool IsFists;
		public readonly bool IsChainsaw;

		public FireMeleeComponent(Fixed range, Sfx missSound, Sfx hitSound, bool isFists, bool isChainsaw)
		{
			this.Range = range;
			this.MissSound = missSound;
			this.HitSound = hitSound;
			this.IsFists = isFists;
			this.IsChainsaw = isChainsaw;
		}

		void INotifyFire.Fire(World world, Player player)
		{
			var damage = 2 * (world.Random.Next() % 10 + 1);

			if (this.IsFists && player.Powers[(int) PowerType.Strength] != 0)
				damage *= 10;

			var angle = player.Mobj.Angle + new Angle((world.Random.Next() - world.Random.Next()) << 18);

			world.Hitscan.LineAttack(player.Mobj, angle, this.Range, world.Hitscan.AimLineAttack(player.Mobj, angle, this.Range), damage);

			if (world.Hitscan.LineTarget == null)
			{
				world.StartSound(player.Mobj, this.MissSound, SfxType.Weapon);

				return;
			}

			world.StartSound(player.Mobj, HitSound, SfxType.Weapon);

			var targetAngle = Geometry.PointToAngle(player.Mobj.X, player.Mobj.Y, world.Hitscan.LineTarget.X, world.Hitscan.LineTarget.Y);

			if (this.IsChainsaw)
			{
				if (targetAngle - player.Mobj.Angle > Angle.Ang180)
				{
					if ((int) (targetAngle - player.Mobj.Angle).Data < -Angle.Ang90.Data / 20)
						targetAngle += Angle.Ang90 / 21;
					else
						targetAngle -= Angle.Ang90 / 20;
				}
				else
				{
					if (targetAngle - player.Mobj.Angle > Angle.Ang90 / 20)
						targetAngle -= Angle.Ang90 / 21;
					else
						targetAngle += Angle.Ang90 / 20;
				}

				player.Mobj.Flags |= MobjFlags.JustAttacked;
			}

			player.Mobj.Angle = targetAngle;
		}
	}
}
