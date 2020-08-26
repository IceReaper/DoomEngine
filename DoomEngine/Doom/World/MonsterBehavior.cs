//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

namespace DoomEngine.Doom.World
{
	using Audio;
	using DoomEngine.Game.Components;
	using Game;
	using Info;
	using Map;
	using Math;
	using System;

	public sealed class MonsterBehavior
	{
		private World world;

		public MonsterBehavior(World world)
		{
			this.world = world;

			this.InitVile();
			this.InitBossDeath();
			this.InitBrain();
		}

		////////////////////////////////////////////////////////////
		// Sleeping monster
		////////////////////////////////////////////////////////////

		private bool LookForPlayers(Mobj actor, bool allAround)
		{
			var player = this.world.Options.Player;

			var count = 0;
			var stop = (actor.LastLook - 1) & 3;

			for (;; actor.LastLook = (actor.LastLook + 1) & 3)
			{
				if (count++ == 2 || actor.LastLook == stop)
				{
					// Done looking.
					return false;
				}

				var healthComponent = player.Entity.GetComponent<Health>();

				if (healthComponent.Current <= 0)
				{
					// Player is dead.
					continue;
				}

				if (!this.world.VisibilityCheck.CheckSight(actor, player.Mobj))
				{
					// Out of sight.
					continue;
				}

				if (!allAround)
				{
					var angle = Geometry.PointToAngle(actor.X, actor.Y, player.Mobj.X, player.Mobj.Y) - actor.Angle;

					if (angle > Angle.Ang90 && angle < Angle.Ang270)
					{
						var dist = Geometry.AproxDistance(player.Mobj.X - actor.X, player.Mobj.Y - actor.Y);

						// If real close, react anyway.
						if (dist > WeaponBehavior.MeleeRange)
						{
							// Behind back.
							continue;
						}
					}
				}

				actor.Target = player.Mobj;

				return true;
			}
		}

		public void Look(Mobj actor)
		{
			// Any shot will wake up.
			actor.Threshold = 0;

			var target = actor.Subsector.Sector.SoundTarget;

			if (target != null && (target.Flags & MobjFlags.Shootable) != 0)
			{
				actor.Target = target;

				if ((actor.Flags & MobjFlags.Ambush) != 0)
				{
					if (this.world.VisibilityCheck.CheckSight(actor, actor.Target))
					{
						goto seeYou;
					}
				}
				else
				{
					goto seeYou;
				}
			}

			if (!this.LookForPlayers(actor, false))
			{
				return;
			}

			// Go into chase state.
			seeYou:

			if (actor.Info.SeeSound != 0)
			{
				int sound;

				switch (actor.Info.SeeSound)
				{
					case Sfx.POSIT1:
					case Sfx.POSIT2:
					case Sfx.POSIT3:
						sound = (int) Sfx.POSIT1 + this.world.Random.Next() % 3;

						break;

					case Sfx.BGSIT1:
					case Sfx.BGSIT2:
						sound = (int) Sfx.BGSIT1 + this.world.Random.Next() % 2;

						break;

					default:
						sound = (int) actor.Info.SeeSound;

						break;
				}

				if (actor.Type == MobjType.Spider || actor.Type == MobjType.Cyborg)
				{
					// Full volume for boss monsters.
					this.world.StartSound(actor, (Sfx) sound, SfxType.Diffuse);
				}
				else
				{
					this.world.StartSound(actor, (Sfx) sound, SfxType.Voice);
				}
			}

			actor.SetState(actor.Info.SeeState);
		}

		////////////////////////////////////////////////////////////
		// Monster AI
		////////////////////////////////////////////////////////////

		private static readonly Fixed[] xSpeed =
		{
			new Fixed(Fixed.FracUnit), new Fixed(47000), new Fixed(0), new Fixed(-47000), new Fixed(-Fixed.FracUnit), new Fixed(-47000), new Fixed(0),
			new Fixed(47000)
		};

		private static readonly Fixed[] ySpeed =
		{
			new Fixed(0), new Fixed(47000), new Fixed(Fixed.FracUnit), new Fixed(47000), new Fixed(0), new Fixed(-47000), new Fixed(-Fixed.FracUnit),
			new Fixed(-47000)
		};

		private bool Move(Mobj actor)
		{
			if (actor.MoveDir == Direction.None)
			{
				return false;
			}

			if ((int) actor.MoveDir >= 8)
			{
				throw new Exception("Weird actor->movedir!");
			}

			var tryX = actor.X + actor.Info.Speed * MonsterBehavior.xSpeed[(int) actor.MoveDir];
			var tryY = actor.Y + actor.Info.Speed * MonsterBehavior.ySpeed[(int) actor.MoveDir];

			var tm = this.world.ThingMovement;

			var tryOk = tm.TryMove(actor, tryX, tryY);

			if (!tryOk)
			{
				// Open any specials.
				if ((actor.Flags & MobjFlags.Float) != 0 && tm.FloatOk)
				{
					// Must adjust height.
					if (actor.Z < tm.CurrentFloorZ)
					{
						actor.Z += ThingMovement.FloatSpeed;
					}
					else
					{
						actor.Z -= ThingMovement.FloatSpeed;
					}

					actor.Flags |= MobjFlags.InFloat;

					return true;
				}

				if (tm.crossedSpecialCount == 0)
				{
					return false;
				}

				actor.MoveDir = Direction.None;
				var good = false;

				while (tm.crossedSpecialCount-- > 0)
				{
					var line = tm.crossedSpecials[tm.crossedSpecialCount];

					// If the special is not a door that can be opened,
					// return false.
					if (this.world.MapInteraction.UseSpecialLine(actor, line, 0))
					{
						good = true;
					}
				}

				return good;
			}
			else
			{
				actor.Flags &= ~MobjFlags.InFloat;
			}

			if ((actor.Flags & MobjFlags.Float) == 0)
			{
				actor.Z = actor.FloorZ;
			}

			return true;
		}

		private bool TryWalk(Mobj actor)
		{
			if (!this.Move(actor))
			{
				return false;
			}

			actor.MoveCount = this.world.Random.Next() & 15;

			return true;
		}

		private static readonly Direction[] opposite =
		{
			Direction.west, Direction.Southwest, Direction.South, Direction.Southeast, Direction.East, Direction.Northeast, Direction.North,
			Direction.Northwest, Direction.None
		};

		private static readonly Direction[] diags = {Direction.Northwest, Direction.Northeast, Direction.Southwest, Direction.Southeast};

		private readonly Direction[] choices = new Direction[3];

		private void NewChaseDir(Mobj actor)
		{
			if (actor.Target == null)
			{
				throw new Exception("Called with no target.");
			}

			var oldDir = actor.MoveDir;
			var turnAround = MonsterBehavior.opposite[(int) oldDir];

			var deltaX = actor.Target.X - actor.X;
			var deltaY = actor.Target.Y - actor.Y;

			if (deltaX > Fixed.FromInt(10))
			{
				this.choices[1] = Direction.East;
			}
			else if (deltaX < Fixed.FromInt(-10))
			{
				this.choices[1] = Direction.west;
			}
			else
			{
				this.choices[1] = Direction.None;
			}

			if (deltaY < Fixed.FromInt(-10))
			{
				this.choices[2] = Direction.South;
			}
			else if (deltaY > Fixed.FromInt(10))
			{
				this.choices[2] = Direction.North;
			}
			else
			{
				this.choices[2] = Direction.None;
			}

			// Try direct route.
			if (this.choices[1] != Direction.None && this.choices[2] != Direction.None)
			{
				var a = (deltaY < Fixed.Zero) ? 1 : 0;
				var b = (deltaX > Fixed.Zero) ? 1 : 0;
				actor.MoveDir = MonsterBehavior.diags[(a << 1) + b];

				if (actor.MoveDir != turnAround && this.TryWalk(actor))
				{
					return;
				}
			}

			// Try other directions.
			if (this.world.Random.Next() > 200 || Fixed.Abs(deltaY) > Fixed.Abs(deltaX))
			{
				var temp = this.choices[1];
				this.choices[1] = this.choices[2];
				this.choices[2] = temp;
			}

			if (this.choices[1] == turnAround)
			{
				this.choices[1] = Direction.None;
			}

			if (this.choices[2] == turnAround)
			{
				this.choices[2] = Direction.None;
			}

			if (this.choices[1] != Direction.None)
			{
				actor.MoveDir = this.choices[1];

				if (this.TryWalk(actor))
				{
					// Either moved forward or attacked.
					return;
				}
			}

			if (this.choices[2] != Direction.None)
			{
				actor.MoveDir = this.choices[2];

				if (this.TryWalk(actor))
				{
					return;
				}
			}

			// There is no direct path to the player, so pick another direction.
			if (oldDir != Direction.None)
			{
				actor.MoveDir = oldDir;

				if (this.TryWalk(actor))
				{
					return;
				}
			}

			// Randomly determine direction of search.
			if ((this.world.Random.Next() & 1) != 0)
			{
				for (var dir = (int) Direction.East; dir <= (int) Direction.Southeast; dir++)
				{
					if ((Direction) dir != turnAround)
					{
						actor.MoveDir = (Direction) dir;

						if (this.TryWalk(actor))
						{
							return;
						}
					}
				}
			}
			else
			{
				for (var dir = (int) Direction.Southeast; dir != ((int) Direction.East - 1); dir--)
				{
					if ((Direction) dir != turnAround)
					{
						actor.MoveDir = (Direction) dir;

						if (this.TryWalk(actor))
						{
							return;
						}
					}
				}
			}

			if (turnAround != Direction.None)
			{
				actor.MoveDir = turnAround;

				if (this.TryWalk(actor))
				{
					return;
				}
			}

			// Can not move.
			actor.MoveDir = Direction.None;
		}

		private bool CheckMeleeRange(Mobj actor)
		{
			if (actor.Target == null)
			{
				return false;
			}

			var target = actor.Target;

			var dist = Geometry.AproxDistance(target.X - actor.X, target.Y - actor.Y);

			if (dist >= WeaponBehavior.MeleeRange - Fixed.FromInt(20) + target.Info.Radius)
			{
				return false;
			}

			if (!this.world.VisibilityCheck.CheckSight(actor, actor.Target))
			{
				return false;
			}

			return true;
		}

		private bool CheckMissileRange(Mobj actor)
		{
			if (!this.world.VisibilityCheck.CheckSight(actor, actor.Target))
			{
				return false;
			}

			if ((actor.Flags & MobjFlags.JustHit) != 0)
			{
				// The target just hit the enemy, so fight back!
				actor.Flags &= ~MobjFlags.JustHit;

				return true;
			}

			if (actor.ReactionTime > 0)
			{
				// Do not attack yet
				return false;
			}

			// OPTIMIZE:
			//     Get this from a global checksight.
			var dist = Geometry.AproxDistance(actor.X - actor.Target.X, actor.Y - actor.Target.Y) - Fixed.FromInt(64);

			if (actor.Info.MeleeState == 0)
			{
				// No melee attack, so fire more.
				dist -= Fixed.FromInt(128);
			}

			var attackDist = dist.Data >> 16;

			if (actor.Type == MobjType.Vile)
			{
				if (attackDist > 14 * 64)
				{
					// Too far away.
					return false;
				}
			}

			if (actor.Type == MobjType.Undead)
			{
				if (attackDist < 196)
				{
					// Close for fist attack.
					return false;
				}

				attackDist >>= 1;
			}

			if (actor.Type == MobjType.Cyborg || actor.Type == MobjType.Spider || actor.Type == MobjType.Skull)
			{
				attackDist >>= 1;
			}

			if (attackDist > 200)
			{
				attackDist = 200;
			}

			if (actor.Type == MobjType.Cyborg && attackDist > 160)
			{
				attackDist = 160;
			}

			if (this.world.Random.Next() < attackDist)
			{
				return false;
			}

			return true;
		}

		public void Chase(Mobj actor)
		{
			if (actor.ReactionTime > 0)
			{
				actor.ReactionTime--;
			}

			// Modify target threshold.
			if (actor.Threshold > 0)
			{
				if (actor.Target == null || actor.Target.Health <= 0)
				{
					actor.Threshold = 0;
				}
				else
				{
					actor.Threshold--;
				}
			}

			// Turn towards movement direction if not there yet.
			if ((int) actor.MoveDir < 8)
			{
				actor.Angle = new Angle((int) actor.Angle.Data & (7 << 29));

				var delta = (int) (actor.Angle - new Angle((int) actor.MoveDir << 29)).Data;

				if (delta > 0)
				{
					actor.Angle -= new Angle(Angle.Ang90.Data / 2);
				}
				else if (delta < 0)
				{
					actor.Angle += new Angle(Angle.Ang90.Data / 2);
				}
			}

			if (actor.Target == null || (actor.Target.Flags & MobjFlags.Shootable) == 0)
			{
				// Look for a new target.
				if (this.LookForPlayers(actor, true))
				{
					// Got a new target.
					return;
				}

				actor.SetState(actor.Info.SpawnState);

				return;
			}

			// Do not attack twice in a row.
			if ((actor.Flags & MobjFlags.JustAttacked) != 0)
			{
				actor.Flags &= ~MobjFlags.JustAttacked;

				if (this.world.Options.Skill != GameSkill.Nightmare && !this.world.Options.FastMonsters)
				{
					this.NewChaseDir(actor);
				}

				return;
			}

			// Check for melee attack.
			if (actor.Info.MeleeState != 0 && this.CheckMeleeRange(actor))
			{
				if (actor.Info.AttackSound != 0)
				{
					this.world.StartSound(actor, actor.Info.AttackSound, SfxType.Weapon);
				}

				actor.SetState(actor.Info.MeleeState);

				return;
			}

			// Check for missile attack.
			if (actor.Info.MissileState != 0)
			{
				if (this.world.Options.Skill < GameSkill.Nightmare && !this.world.Options.FastMonsters && actor.MoveCount != 0)
				{
					goto noMissile;
				}

				if (!this.CheckMissileRange(actor))
				{
					goto noMissile;
				}

				actor.SetState(actor.Info.MissileState);
				actor.Flags |= MobjFlags.JustAttacked;

				return;
			}

			noMissile:

			// Chase towards player.
			if (--actor.MoveCount < 0 || !this.Move(actor))
			{
				this.NewChaseDir(actor);
			}

			// Make active sound.
			if (actor.Info.ActiveSound != 0 && this.world.Random.Next() < 3)
			{
				this.world.StartSound(actor, actor.Info.ActiveSound, SfxType.Voice);
			}
		}

		////////////////////////////////////////////////////////////
		// Monster death
		////////////////////////////////////////////////////////////

		public void Pain(Mobj actor)
		{
			if (actor.Info.PainSound != 0)
			{
				this.world.StartSound(actor, actor.Info.PainSound, SfxType.Voice);
			}
		}

		public void Scream(Mobj actor)
		{
			int sound;

			switch (actor.Info.DeathSound)
			{
				case 0:
					return;

				case Sfx.PODTH1:
				case Sfx.PODTH2:
				case Sfx.PODTH3:
					sound = (int) Sfx.PODTH1 + this.world.Random.Next() % 3;

					break;

				case Sfx.BGDTH1:
				case Sfx.BGDTH2:
					sound = (int) Sfx.BGDTH1 + this.world.Random.Next() % 2;

					break;

				default:
					sound = (int) actor.Info.DeathSound;

					break;
			}

			// Check for bosses.
			if (actor.Type == MobjType.Spider || actor.Type == MobjType.Cyborg)
			{
				// Full volume.
				this.world.StartSound(actor, (Sfx) sound, SfxType.Diffuse);
			}
			else
			{
				this.world.StartSound(actor, (Sfx) sound, SfxType.Voice);
			}
		}

		public void XScream(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.SLOP, SfxType.Voice);
		}

		public void Fall(Mobj actor)
		{
			// Actor is on ground, it can be walked over.
			actor.Flags &= ~MobjFlags.Solid;
		}

		////////////////////////////////////////////////////////////
		// Monster attack
		////////////////////////////////////////////////////////////

		public void FaceTarget(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			actor.Flags &= ~MobjFlags.Ambush;

			actor.Angle = Geometry.PointToAngle(actor.X, actor.Y, actor.Target.X, actor.Target.Y);

			var random = this.world.Random;

			if ((actor.Target.Flags & MobjFlags.Shadow) != 0)
			{
				actor.Angle += new Angle((random.Next() - random.Next()) << 21);
			}
		}

		public void PosAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			var angle = actor.Angle;
			var slope = this.world.Hitscan.AimLineAttack(actor, angle, WeaponBehavior.MissileRange);

			this.world.StartSound(actor, Sfx.PISTOL, SfxType.Weapon);

			var random = this.world.Random;
			angle += new Angle((random.Next() - random.Next()) << 20);
			var damage = ((random.Next() % 5) + 1) * 3;

			this.world.Hitscan.LineAttack(actor, angle, WeaponBehavior.MissileRange, slope, damage);
		}

		public void SPosAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.world.StartSound(actor, Sfx.SHOTGN, SfxType.Weapon);

			this.FaceTarget(actor);

			var center = actor.Angle;
			var slope = this.world.Hitscan.AimLineAttack(actor, center, WeaponBehavior.MissileRange);

			var random = this.world.Random;

			for (var i = 0; i < 3; i++)
			{
				var angle = center + new Angle((random.Next() - random.Next()) << 20);
				var damage = ((random.Next() % 5) + 1) * 3;

				this.world.Hitscan.LineAttack(actor, angle, WeaponBehavior.MissileRange, slope, damage);
			}
		}

		public void CPosAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.world.StartSound(actor, Sfx.SHOTGN, SfxType.Weapon);

			this.FaceTarget(actor);

			var center = actor.Angle;
			var slope = this.world.Hitscan.AimLineAttack(actor, center, WeaponBehavior.MissileRange);

			var random = this.world.Random;
			var angle = center + new Angle((random.Next() - random.Next()) << 20);
			var damage = ((random.Next() % 5) + 1) * 3;

			this.world.Hitscan.LineAttack(actor, angle, WeaponBehavior.MissileRange, slope, damage);
		}

		public void CPosRefire(Mobj actor)
		{
			// Keep firing unless target got out of sight.
			this.FaceTarget(actor);

			if (this.world.Random.Next() < 40)
			{
				return;
			}

			if (actor.Target == null || actor.Target.Health <= 0 || !this.world.VisibilityCheck.CheckSight(actor, actor.Target))
			{
				actor.SetState(actor.Info.SeeState);
			}
		}

		public void TroopAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			if (this.CheckMeleeRange(actor))
			{
				this.world.StartSound(actor, Sfx.CLAW, SfxType.Weapon);

				var damage = (this.world.Random.Next() % 8 + 1) * 3;
				this.world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);

				return;
			}

			// Launch a missile.
			this.world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Troopshot);
		}

		public void SargAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			if (this.CheckMeleeRange(actor))
			{
				var damage = ((this.world.Random.Next() % 10) + 1) * 4;
				this.world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);
			}
		}

		public void HeadAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			if (this.CheckMeleeRange(actor))
			{
				var damage = (this.world.Random.Next() % 6 + 1) * 10;
				this.world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);

				return;
			}

			// Launch a missile.
			this.world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Headshot);
		}

		public void BruisAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			if (this.CheckMeleeRange(actor))
			{
				this.world.StartSound(actor, Sfx.CLAW, SfxType.Weapon);

				var damage = (this.world.Random.Next() % 8 + 1) * 10;
				this.world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);

				return;
			}

			// Launch a missile.
			this.world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Bruisershot);
		}

		private static readonly Fixed skullSpeed = Fixed.FromInt(20);

		public void SkullAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			var dest = actor.Target;

			actor.Flags |= MobjFlags.SkullFly;

			this.world.StartSound(actor, actor.Info.AttackSound, SfxType.Voice);

			this.FaceTarget(actor);

			var angle = actor.Angle;
			actor.MomX = MonsterBehavior.skullSpeed * Trig.Cos(angle);
			actor.MomY = MonsterBehavior.skullSpeed * Trig.Sin(angle);

			var dist = Geometry.AproxDistance(dest.X - actor.X, dest.Y - actor.Y);

			var num = (dest.Z + (dest.Height >> 1) - actor.Z).Data;
			var den = dist.Data / MonsterBehavior.skullSpeed.Data;

			if (den < 1)
			{
				den = 1;
			}

			actor.MomZ = new Fixed(num / den);
		}

		public void FatRaise(Mobj actor)
		{
			this.FaceTarget(actor);

			this.world.StartSound(actor, Sfx.MANATK, SfxType.Voice);
		}

		private static readonly Angle fatSpread = Angle.Ang90 / 8;

		public void FatAttack1(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			var ta = this.world.ThingAllocation;

			// Change direction to...
			actor.Angle += MonsterBehavior.fatSpread;
			ta.SpawnMissile(actor, actor.Target, MobjType.Fatshot);

			var missile = ta.SpawnMissile(actor, actor.Target, MobjType.Fatshot);
			missile.Angle += MonsterBehavior.fatSpread;
			var angle = missile.Angle;
			missile.MomX = new Fixed(missile.Info.Speed) * Trig.Cos(angle);
			missile.MomY = new Fixed(missile.Info.Speed) * Trig.Sin(angle);
		}

		public void FatAttack2(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			var ta = this.world.ThingAllocation;

			// Now here choose opposite deviation.
			actor.Angle -= MonsterBehavior.fatSpread;
			ta.SpawnMissile(actor, actor.Target, MobjType.Fatshot);

			var missile = ta.SpawnMissile(actor, actor.Target, MobjType.Fatshot);
			missile.Angle -= MonsterBehavior.fatSpread * 2;
			var angle = missile.Angle;
			missile.MomX = new Fixed(missile.Info.Speed) * Trig.Cos(angle);
			missile.MomY = new Fixed(missile.Info.Speed) * Trig.Sin(angle);
		}

		public void FatAttack3(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			var ta = this.world.ThingAllocation;

			var missile1 = ta.SpawnMissile(actor, actor.Target, MobjType.Fatshot);
			missile1.Angle -= MonsterBehavior.fatSpread / 2;
			var angle1 = missile1.Angle;
			missile1.MomX = new Fixed(missile1.Info.Speed) * Trig.Cos(angle1);
			missile1.MomY = new Fixed(missile1.Info.Speed) * Trig.Sin(angle1);

			var missile2 = ta.SpawnMissile(actor, actor.Target, MobjType.Fatshot);
			missile2.Angle += MonsterBehavior.fatSpread / 2;
			var angle2 = missile2.Angle;
			missile2.MomX = new Fixed(missile2.Info.Speed) * Trig.Cos(angle2);
			missile2.MomY = new Fixed(missile2.Info.Speed) * Trig.Sin(angle2);
		}

		public void BspiAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			// Launch a missile.
			this.world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Arachplaz);
		}

		public void SpidRefire(Mobj actor)
		{
			// Keep firing unless target got out of sight.
			this.FaceTarget(actor);

			if (this.world.Random.Next() < 10)
			{
				return;
			}

			if (actor.Target == null || actor.Target.Health <= 0 || !this.world.VisibilityCheck.CheckSight(actor, actor.Target))
			{
				actor.SetState(actor.Info.SeeState);
			}
		}

		public void CyberAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			this.world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Rocket);
		}

		////////////////////////////////////////////////////////////
		// Miscellaneous
		////////////////////////////////////////////////////////////

		public void Explode(Mobj actor)
		{
			this.world.ThingInteraction.RadiusAttack(actor, actor.Target, 128);
		}

		public void Metal(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.METAL, SfxType.Footstep);

			this.Chase(actor);
		}

		public void BabyMetal(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.BSPWLK, SfxType.Footstep);

			this.Chase(actor);
		}

		public void Hoof(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.HOOF, SfxType.Footstep);

			this.Chase(actor);
		}

		////////////////////////////////////////////////////////////
		// Arch vile
		////////////////////////////////////////////////////////////

		private Func<Mobj, bool> vileCheckFunc;
		private Mobj vileTargetCorpse;
		private Fixed vileTryX;
		private Fixed vileTryY;

		private void InitVile()
		{
			this.vileCheckFunc = this.VileCheck;
		}

		private bool VileCheck(Mobj thing)
		{
			if ((thing.Flags & MobjFlags.Corpse) == 0)
			{
				// Not a monster.
				return true;
			}

			if (thing.Tics != -1)
			{
				// Not lying still yet.
				return true;
			}

			if (thing.Info.Raisestate == MobjState.Null)
			{
				// Monster doesn't have a raise state.
				return true;
			}

			var maxDist = thing.Info.Radius + DoomInfo.MobjInfos[(int) MobjType.Vile].Radius;

			if (Fixed.Abs(thing.X - this.vileTryX) > maxDist || Fixed.Abs(thing.Y - this.vileTryY) > maxDist)
			{
				// Not actually touching.
				return true;
			}

			this.vileTargetCorpse = thing;
			this.vileTargetCorpse.MomX = this.vileTargetCorpse.MomY = Fixed.Zero;
			this.vileTargetCorpse.Height <<= 2;

			var check = this.world.ThingMovement.CheckPosition(this.vileTargetCorpse, this.vileTargetCorpse.X, this.vileTargetCorpse.Y);

			this.vileTargetCorpse.Height >>= 2;

			if (!check)
			{
				// Doesn't fit here.
				return true;
			}

			// Got one, so stop checking.
			return false;
		}

		public void VileChase(Mobj actor)
		{
			if (actor.MoveDir != Direction.None)
			{
				// Check for corpses to raise.
				this.vileTryX = actor.X + actor.Info.Speed * MonsterBehavior.xSpeed[(int) actor.MoveDir];
				this.vileTryY = actor.Y + actor.Info.Speed * MonsterBehavior.ySpeed[(int) actor.MoveDir];

				var bm = this.world.Map.BlockMap;

				var maxRadius = GameConst.MaxThingRadius * 2;
				var blockX1 = bm.GetBlockX(this.vileTryX - maxRadius);
				var blockX2 = bm.GetBlockX(this.vileTryX + maxRadius);
				var blockY1 = bm.GetBlockY(this.vileTryY - maxRadius);
				var blockY2 = bm.GetBlockY(this.vileTryY + maxRadius);

				for (var bx = blockX1; bx <= blockX2; bx++)
				{
					for (var by = blockY1; by <= blockY2; by++)
					{
						// Call VileCheck to check whether object is a corpse that canbe raised.
						if (!bm.IterateThings(bx, by, this.vileCheckFunc))
						{
							// Got one!
							var temp = actor.Target;
							actor.Target = this.vileTargetCorpse;
							this.FaceTarget(actor);
							actor.Target = temp;
							actor.SetState(MobjState.VileHeal1);

							this.world.StartSound(this.vileTargetCorpse, Sfx.SLOP, SfxType.Misc);

							var info = this.vileTargetCorpse.Info;
							this.vileTargetCorpse.SetState(info.Raisestate);
							this.vileTargetCorpse.Height <<= 2;
							this.vileTargetCorpse.Flags = info.Flags;
							this.vileTargetCorpse.Health = info.SpawnHealth;
							this.vileTargetCorpse.Target = null;

							return;
						}
					}
				}
			}

			// Return to normal attack.
			this.Chase(actor);
		}

		public void VileStart(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.VILATK, SfxType.Weapon);
		}

		public void StartFire(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.FLAMST, SfxType.Weapon);

			this.Fire(actor);
		}

		public void FireCrackle(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.FLAME, SfxType.Weapon);

			this.Fire(actor);
		}

		public void Fire(Mobj actor)
		{
			var dest = actor.Tracer;

			if (dest == null)
			{
				return;
			}

			// Don't move it if the vile lost sight.
			if (!this.world.VisibilityCheck.CheckSight(actor.Target, dest))
			{
				return;
			}

			this.world.ThingMovement.UnsetThingPosition(actor);

			var angle = dest.Angle;
			actor.X = dest.X + Fixed.FromInt(24) * Trig.Cos(angle);
			actor.Y = dest.Y + Fixed.FromInt(24) * Trig.Sin(angle);
			actor.Z = dest.Z;

			this.world.ThingMovement.SetThingPosition(actor);
		}

		public void VileTarget(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			var fog = this.world.ThingAllocation.SpawnMobj(actor.Target.X, actor.Target.X, actor.Target.Z, MobjType.Fire);

			actor.Tracer = fog;
			fog.Target = actor;
			fog.Tracer = actor.Target;
			this.Fire(fog);
		}

		public void VileAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			if (!this.world.VisibilityCheck.CheckSight(actor, actor.Target))
			{
				return;
			}

			this.world.StartSound(actor, Sfx.BAREXP, SfxType.Weapon);
			this.world.ThingInteraction.DamageMobj(actor.Target, actor, actor, 20);
			actor.Target.MomZ = Fixed.FromInt(1000) / actor.Target.Info.Mass;

			var fire = actor.Tracer;

			if (fire == null)
			{
				return;
			}

			var angle = actor.Angle;

			// Move the fire between the vile and the player.
			fire.X = actor.Target.X - Fixed.FromInt(24) * Trig.Cos(angle);
			fire.Y = actor.Target.Y - Fixed.FromInt(24) * Trig.Sin(angle);
			this.world.ThingInteraction.RadiusAttack(fire, actor, 70);
		}

		////////////////////////////////////////////////////////////
		// Revenant
		////////////////////////////////////////////////////////////

		public void SkelMissile(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			// Missile spawns higher.
			actor.Z += Fixed.FromInt(16);

			var missile = this.world.ThingAllocation.SpawnMissile(actor, actor.Target, MobjType.Tracer);

			// Back to normal.
			actor.Z -= Fixed.FromInt(16);

			missile.X += missile.MomX;
			missile.Y += missile.MomY;
			missile.Tracer = actor.Target;
		}

		private static Angle traceAngle = new Angle(0xc000000);

		public void Tracer(Mobj actor)
		{
			if ((this.world.GameTic & 3) != 0)
			{
				return;
			}

			// Spawn a puff of smoke behind the rocket.
			this.world.Hitscan.SpawnPuff(actor.X, actor.Y, actor.Z);

			var smoke = this.world.ThingAllocation.SpawnMobj(actor.X - actor.MomX, actor.Y - actor.MomY, actor.Z, MobjType.Smoke);

			smoke.MomZ = Fixed.One;
			smoke.Tics -= this.world.Random.Next() & 3;

			if (smoke.Tics < 1)
			{
				smoke.Tics = 1;
			}

			// Adjust direction.
			var dest = actor.Tracer;

			if (dest == null || dest.Health <= 0)
			{
				return;
			}

			// Change angle.
			var exact = Geometry.PointToAngle(actor.X, actor.Y, dest.X, dest.Y);

			if (exact != actor.Angle)
			{
				if (exact - actor.Angle > Angle.Ang180)
				{
					actor.Angle -= MonsterBehavior.traceAngle;

					if (exact - actor.Angle < Angle.Ang180)
					{
						actor.Angle = exact;
					}
				}
				else
				{
					actor.Angle += MonsterBehavior.traceAngle;

					if (exact - actor.Angle > Angle.Ang180)
					{
						actor.Angle = exact;
					}
				}
			}

			exact = actor.Angle;
			actor.MomX = new Fixed(actor.Info.Speed) * Trig.Cos(exact);
			actor.MomY = new Fixed(actor.Info.Speed) * Trig.Sin(exact);

			// Change slope.
			var dist = Geometry.AproxDistance(dest.X - actor.X, dest.Y - actor.Y);

			var num = (dest.Z + Fixed.FromInt(40) - actor.Z).Data;
			var den = dist.Data / actor.Info.Speed;

			if (den < 1)
			{
				den = 1;
			}

			var slope = new Fixed(num / den);

			if (slope < actor.MomZ)
			{
				actor.MomZ -= Fixed.One / 8;
			}
			else
			{
				actor.MomZ += Fixed.One / 8;
			}
		}

		public void SkelWhoosh(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			this.world.StartSound(actor, Sfx.SKESWG, SfxType.Weapon);
		}

		public void SkelFist(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			if (this.CheckMeleeRange(actor))
			{
				var damage = ((this.world.Random.Next() % 10) + 1) * 6;
				this.world.StartSound(actor, Sfx.SKEPCH, SfxType.Weapon);
				this.world.ThingInteraction.DamageMobj(actor.Target, actor, actor, damage);
			}
		}

		////////////////////////////////////////////////////////////
		// Pain elemental
		////////////////////////////////////////////////////////////

		private void PainShootSkull(Mobj actor, Angle angle)
		{
			// Count total number of skull currently on the level.
			var count = 0;

			foreach (var thinker in this.world.Thinkers)
			{
				var mobj = thinker as Mobj;

				if (mobj != null && mobj.Type == MobjType.Skull)
				{
					count++;
				}
			}

			// If there are allready 20 skulls on the level,
			// don't spit another one.
			if (count > 20)
			{
				return;
			}

			// Okay, there's playe for another one.

			var preStep = Fixed.FromInt(4) + 3 * (actor.Info.Radius + DoomInfo.MobjInfos[(int) MobjType.Skull].Radius) / 2;

			var x = actor.X + preStep * Trig.Cos(angle);
			var y = actor.Y + preStep * Trig.Sin(angle);
			var z = actor.Z + Fixed.FromInt(8);

			var skull = this.world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Skull);

			// Check for movements.
			if (!this.world.ThingMovement.TryMove(skull, skull.X, skull.Y))
			{
				// Kill it immediately.
				this.world.ThingInteraction.DamageMobj(skull, actor, actor, 10000);

				return;
			}

			skull.Target = actor.Target;

			this.SkullAttack(skull);
		}

		public void PainAttack(Mobj actor)
		{
			if (actor.Target == null)
			{
				return;
			}

			this.FaceTarget(actor);

			this.PainShootSkull(actor, actor.Angle);
		}

		public void PainDie(Mobj actor)
		{
			this.Fall(actor);

			this.PainShootSkull(actor, actor.Angle + Angle.Ang90);
			this.PainShootSkull(actor, actor.Angle + Angle.Ang180);
			this.PainShootSkull(actor, actor.Angle + Angle.Ang270);
		}

		////////////////////////////////////////////////////////////
		// Boss death
		////////////////////////////////////////////////////////////

		private LineDef junk;

		private void InitBossDeath()
		{
			var v = new Vertex(Fixed.Zero, Fixed.Zero);
			this.junk = new LineDef(v, v, 0, 0, 0, null, null);
		}

		public void BossDeath(Mobj actor)
		{
			var options = this.world.Options;

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				if (options.Map != 7)
				{
					return;
				}

				if ((actor.Type != MobjType.Fatso) && (actor.Type != MobjType.Baby))
				{
					return;
				}
			}
			else
			{
				switch (options.Episode)
				{
					case 1:
						if (options.Map != 8)
						{
							return;
						}

						if (actor.Type != MobjType.Bruiser)
						{
							return;
						}

						break;

					case 2:
						if (options.Map != 8)
						{
							return;
						}

						if (actor.Type != MobjType.Cyborg)
						{
							return;
						}

						break;

					case 3:
						if (options.Map != 8)
						{
							return;
						}

						if (actor.Type != MobjType.Spider)
						{
							return;
						}

						break;

					case 4:
						switch (options.Map)
						{
							case 6:
								if (actor.Type != MobjType.Cyborg)
								{
									return;
								}

								break;

							case 8:
								if (actor.Type != MobjType.Spider)
								{
									return;
								}

								break;

							default:
								return;
						}

						break;

					default:
						if (options.Map != 8)
						{
							return;
						}

						break;
				}
			}

			// Make sure there is a player alive for victory.
			var player = this.world.Options.Player;
			var healthComponent = player.Entity.GetComponent<Health>();

			if (healthComponent.Current == 0)
			{
				// No one left alive, so do not end game.
				return;
			}

			// Scan the remaining thinkers to see if all bosses are dead.
			foreach (var thinker in this.world.Thinkers)
			{
				var mo2 = thinker as Mobj;

				if (mo2 == null)
				{
					continue;
				}

				if (mo2 != actor && mo2.Type == actor.Type && mo2.Health > 0)
				{
					// Other boss not dead.
					return;
				}
			}

			// Victory!
			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				if (options.Map == 7)
				{
					if (actor.Type == MobjType.Fatso)
					{
						this.junk.Tag = 666;
						this.world.SectorAction.DoFloor(this.junk, FloorMoveType.LowerFloorToLowest);

						return;
					}

					if (actor.Type == MobjType.Baby)
					{
						this.junk.Tag = 667;
						this.world.SectorAction.DoFloor(this.junk, FloorMoveType.RaiseToTexture);

						return;
					}
				}
			}
			else
			{
				switch (options.Episode)
				{
					case 1:
						this.junk.Tag = 666;
						this.world.SectorAction.DoFloor(this.junk, FloorMoveType.LowerFloorToLowest);

						return;

					case 4:
						switch (options.Map)
						{
							case 6:
								this.junk.Tag = 666;
								this.world.SectorAction.DoDoor(this.junk, VerticalDoorType.BlazeOpen);

								return;

							case 8:
								this.junk.Tag = 666;
								this.world.SectorAction.DoFloor(this.junk, FloorMoveType.LowerFloorToLowest);

								return;
						}

						break;
				}
			}

			this.world.ExitLevel();
		}

		public void KeenDie(Mobj actor)
		{
			this.Fall(actor);

			// scan the remaining thinkers
			// to see if all Keens are dead
			foreach (var thinker in this.world.Thinkers)
			{
				var mo2 = thinker as Mobj;

				if (mo2 == null)
				{
					continue;
				}

				if (mo2 != actor && mo2.Type == actor.Type && mo2.Health > 0)
				{
					// other Keen not dead
					return;
				}
			}

			this.junk.Tag = 666;
			this.world.SectorAction.DoDoor(this.junk, VerticalDoorType.Open);
		}

		////////////////////////////////////////////////////////////
		// Icon of sin
		////////////////////////////////////////////////////////////

		private Mobj[] brainTargets;
		private int brainTargetCount;
		private int currentBrainTarget;
		private bool easy;

		private void InitBrain()
		{
			this.brainTargets = new Mobj[32];
			this.brainTargetCount = 0;
			this.currentBrainTarget = 0;
			this.easy = false;
		}

		public void BrainAwake(Mobj actor)
		{
			// Find all the target spots.
			this.brainTargetCount = 0;
			this.currentBrainTarget = 0;

			foreach (var thinker in this.world.Thinkers)
			{
				var mobj = thinker as Mobj;

				if (mobj == null)
				{
					// Not a mobj.
					continue;
				}

				if (mobj.Type == MobjType.Bosstarget)
				{
					this.brainTargets[this.brainTargetCount] = mobj;
					this.brainTargetCount++;
				}
			}

			this.world.StartSound(actor, Sfx.BOSSIT, SfxType.Diffuse);
		}

		public void BrainPain(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.BOSPN, SfxType.Diffuse);
		}

		public void BrainScream(Mobj actor)
		{
			var random = this.world.Random;

			for (var x = actor.X - Fixed.FromInt(196); x < actor.X + Fixed.FromInt(320); x += Fixed.FromInt(8))
			{
				var y = actor.Y - Fixed.FromInt(320);
				var z = new Fixed(128) + random.Next() * Fixed.FromInt(2);

				var explosion = this.world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Rocket);
				explosion.MomZ = new Fixed(random.Next() * 512);
				explosion.SetState(MobjState.Brainexplode1);
				explosion.Tics -= random.Next() & 7;

				if (explosion.Tics < 1)
				{
					explosion.Tics = 1;
				}
			}

			this.world.StartSound(actor, Sfx.BOSDTH, SfxType.Diffuse);
		}

		public void BrainExplode(Mobj actor)
		{
			var random = this.world.Random;

			var x = actor.X + new Fixed((random.Next() - random.Next()) * 2048);
			var y = actor.Y;
			var z = new Fixed(128) + random.Next() * Fixed.FromInt(2);

			var explosion = this.world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Rocket);
			explosion.MomZ = new Fixed(random.Next() * 512);
			explosion.SetState(MobjState.Brainexplode1);
			explosion.Tics -= random.Next() & 7;

			if (explosion.Tics < 1)
			{
				explosion.Tics = 1;
			}
		}

		public void BrainDie(Mobj actor)
		{
			this.world.ExitLevel();
		}

		public void BrainSpit(Mobj actor)
		{
			this.easy = !this.easy;

			if (this.world.Options.Skill <= GameSkill.Easy && (!this.easy))
			{
				return;
			}

			// If the game is reconstructed from a savedata, brain targets might be cleared.
			// If so, re-initialize them to avoid crash.
			if (this.brainTargetCount == 0)
			{
				this.BrainAwake(actor);
			}

			// Shoot a cube at current target.
			var target = this.brainTargets[this.currentBrainTarget];
			this.currentBrainTarget = (this.currentBrainTarget + 1) % this.brainTargetCount;

			// Spawn brain missile.
			var missile = this.world.ThingAllocation.SpawnMissile(actor, target, MobjType.Spawnshot);
			missile.Target = target;
			missile.ReactionTime = ((target.Y - actor.Y).Data / missile.MomY.Data) / missile.State.Tics;

			this.world.StartSound(actor, Sfx.BOSPIT, SfxType.Diffuse);
		}

		public void SpawnSound(Mobj actor)
		{
			this.world.StartSound(actor, Sfx.BOSCUB, SfxType.Misc);
			this.SpawnFly(actor);
		}

		public void SpawnFly(Mobj actor)
		{
			if (--actor.ReactionTime > 0)
			{
				// Still flying.
				return;
			}

			var target = actor.Target;

			// If the game is reconstructed from a savedata, the target might be null.
			// If so, use own position to spawn the monster.
			if (target == null)
			{
				target = actor;
				actor.Z = actor.Subsector.Sector.FloorHeight;
			}

			var ta = this.world.ThingAllocation;

			// First spawn teleport fog.
			var fog = ta.SpawnMobj(target.X, target.Y, target.Z, MobjType.Spawnfire);
			this.world.StartSound(fog, Sfx.TELEPT, SfxType.Misc);

			// Randomly select monster to spawn.
			var r = this.world.Random.Next();

			// Probability distribution (kind of :), decreasing likelihood.
			MobjType type;

			if (r < 50)
			{
				type = MobjType.Troop;
			}
			else if (r < 90)
			{
				type = MobjType.Sergeant;
			}
			else if (r < 120)
			{
				type = MobjType.Shadows;
			}
			else if (r < 130)
			{
				type = MobjType.Pain;
			}
			else if (r < 160)
			{
				type = MobjType.Head;
			}
			else if (r < 162)
			{
				type = MobjType.Vile;
			}
			else if (r < 172)
			{
				type = MobjType.Undead;
			}
			else if (r < 192)
			{
				type = MobjType.Baby;
			}
			else if (r < 222)
			{
				type = MobjType.Fatso;
			}
			else if (r < 246)
			{
				type = MobjType.Knight;
			}
			else
			{
				type = MobjType.Bruiser;
			}

			var monster = ta.SpawnMobj(target.X, target.Y, target.Z, type);

			if (this.LookForPlayers(monster, true))
			{
				monster.SetState(monster.Info.SeeState);
			}

			// Telefrag anything in this spot.
			this.world.ThingMovement.TeleportMove(monster, monster.X, monster.Y);

			// Remove self (i.e., cube).
			this.world.ThingAllocation.RemoveMobj(actor);
		}
	}
}
