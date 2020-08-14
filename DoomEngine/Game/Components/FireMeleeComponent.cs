namespace DoomEngine.Game.Components
{
	using Audio;
	using Doom.Game;
	using Doom.Math;
	using Doom.World;
	using Interfaces;

	public class FireMeleeComponent : Component, INotifyFire
	{
		void INotifyFire.Fire(World world, Player player)
		{
			// TODO combine melee and chainsaw
			{
				// TODO this is from fists:
				var damage = ((world.Random.Next() % 10 + 1) << 1) * player.Powers[(int) PowerType.Strength] != 0 ? 10 : 1;
				var angle = player.Mobj.Angle + new Angle((world.Random.Next() - world.Random.Next()) << 18);

				world.Hitscan.LineAttack(
					player.Mobj,
					angle,
					WeaponBehavior.MeleeRange,
					world.Hitscan.AimLineAttack(player.Mobj, angle, WeaponBehavior.MeleeRange),
					damage
				);

				if (world.Hitscan.LineTarget == null)
					return;

				world.StartSound(player.Mobj, Sfx.PUNCH, SfxType.Weapon);
				player.Mobj.Angle = Geometry.PointToAngle(player.Mobj.X, player.Mobj.Y, world.Hitscan.LineTarget.X, world.Hitscan.LineTarget.Y);
			}

			{
				// TODO this is from chainsaw:
				var damage = 2 * (world.Random.Next() % 10 + 1);
				var angle = player.Mobj.Angle + new Angle((world.Random.Next() - world.Random.Next()) << 18);

				world.Hitscan.LineAttack(
					player.Mobj,
					angle,
					WeaponBehavior.MeleeRange + Fixed.Epsilon,
					world.Hitscan.AimLineAttack(player.Mobj, angle, WeaponBehavior.MeleeRange + Fixed.Epsilon),
					damage
				);

				if (world.Hitscan.LineTarget == null)
				{
					world.StartSound(player.Mobj, Sfx.SAWFUL, SfxType.Weapon);

					return;
				}

				world.StartSound(player.Mobj, Sfx.SAWHIT, SfxType.Weapon);

				var targetAngle = Geometry.PointToAngle(player.Mobj.X, player.Mobj.Y, world.Hitscan.LineTarget.X, world.Hitscan.LineTarget.Y);

				if (targetAngle - player.Mobj.Angle > Angle.Ang180)
				{
					if ((int) (targetAngle - player.Mobj.Angle).Data < -Angle.Ang90.Data / 20)
						player.Mobj.Angle = targetAngle + Angle.Ang90 / 21;
					else
						player.Mobj.Angle -= Angle.Ang90 / 20;
				}
				else
				{
					if (targetAngle - player.Mobj.Angle > Angle.Ang90 / 20)
						player.Mobj.Angle = targetAngle - Angle.Ang90 / 21;
					else
						player.Mobj.Angle += Angle.Ang90 / 20;
				}

				player.Mobj.Flags |= MobjFlags.JustAttacked;
			}
		}
	}
}
