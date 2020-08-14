namespace DoomEngine.Game.Components
{
	using Audio;
	using Doom.Game;
	using Doom.Math;
	using Doom.World;
	using Interfaces;

	public class FireHitscanComponent : Component, INotifyFire
	{
		public readonly Sfx Sound;
		public readonly int Bullets;
		public readonly int Spread;
		public readonly bool FirstShotAccurate;

		private Fixed currentBulletSlope;

		public FireHitscanComponent(Sfx sound, int bullets, int spread, bool firstShotAccurate)
		{
			this.Sound = sound;
			this.Bullets = bullets;
			this.Spread = spread;
			this.FirstShotAccurate = firstShotAccurate;
		}

		void INotifyFire.Fire(World world, Player player)
		{
			world.StartSound(player.Mobj, this.Sound, SfxType.Weapon);
			player.Mobj.SetState(MobjState.PlayAtk2);

			this.BulletSlope(world, player.Mobj);

			for (var i = 0; i < this.Bullets; i++)
			{
				var damage = 5 * (world.Random.Next() % 3 + 1);

				var angle = player.Mobj.Angle;

				if (!this.FirstShotAccurate || player.Refire != 0)
					angle += Angle.FromDegree((world.Random.Next() - world.Random.Next()) / 255 * this.Spread);

				world.Hitscan.LineAttack(player.Mobj, angle, WeaponBehavior.MissileRange, this.currentBulletSlope, damage);
			}
		}

		private void BulletSlope(World world, Mobj mo)
		{
			var angle = mo.Angle;
			this.currentBulletSlope = world.Hitscan.AimLineAttack(mo, angle, Fixed.FromInt(1024));

			if (world.Hitscan.LineTarget != null)
				return;

			angle += new Angle(1 << 26);
			this.currentBulletSlope = world.Hitscan.AimLineAttack(mo, angle, Fixed.FromInt(1024));

			if (world.Hitscan.LineTarget != null)
				return;

			angle -= new Angle(2 << 26);
			this.currentBulletSlope = world.Hitscan.AimLineAttack(mo, angle, Fixed.FromInt(1024));
		}
	}
}
