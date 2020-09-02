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
	using Map;
	using Math;
	using System;

	public sealed class Hitscan
	{
		private World world;

		public Hitscan(World world)
		{
			this.world = world;

			this.aimTraverseFunc = this.AimTraverse;
			this.shootTraverseFunc = this.ShootTraverse;
		}

		private Func<Intercept, bool> aimTraverseFunc;
		private Func<Intercept, bool> shootTraverseFunc;

		// Who got hit (or null).
		private Mobj lineTarget;

		private Mobj currentShooter;
		private Fixed currentShooterZ;

		private Fixed currentRange;
		private Fixed currentAimSlope;
		private int currentDamage;

		// Slopes to top and bottom of target.
		private Fixed topSlope;
		private Fixed bottomSlope;

		/// <summary>
		/// Find a thing or wall which is on the aiming line.
		/// Sets lineTaget and aimSlope when a target is aimed at.
		/// </summary>
		private bool AimTraverse(Intercept intercept)
		{
			if (intercept.Line != null)
			{
				var line = intercept.Line;

				if ((line.Flags & LineFlags.TwoSided) == 0)
				{
					// Stop.
					return false;
				}

				var mc = this.world.MapCollision;

				// Crosses a two sided line.
				// A two sided line will restrict the possible target ranges.
				mc.LineOpening(line);

				if (mc.OpenBottom >= mc.OpenTop)
				{
					// Stop.
					return false;
				}

				var dist = this.currentRange * intercept.Frac;

				// The null check of the backsector below is necessary to avoid crash
				// in certain PWADs, which contain two-sided lines with no backsector.
				// These are imported from Chocolate Doom.

				if (line.BackSector == null || line.FrontSector.FloorHeight != line.BackSector.FloorHeight)
				{
					var slope = (mc.OpenBottom - this.currentShooterZ) / dist;

					if (slope > this.bottomSlope)
					{
						this.bottomSlope = slope;
					}
				}

				if (line.BackSector == null || line.FrontSector.CeilingHeight != line.BackSector.CeilingHeight)
				{
					var slope = (mc.OpenTop - this.currentShooterZ) / dist;

					if (slope < this.topSlope)
					{
						this.topSlope = slope;
					}
				}

				if (this.topSlope <= this.bottomSlope)
				{
					// Stop.
					return false;
				}

				// Shot continues.
				return true;
			}

			// Shoot a thing.
			var thing = intercept.Thing;

			if (thing == this.currentShooter)
			{
				// Can't shoot self.
				return true;
			}

			{
				if ((thing.Flags & MobjFlags.Shootable) == 0)
				{
					// Corpse or something.
					return true;
				}

				// Check angles to see if the thing can be aimed at.
				var dist = this.currentRange * intercept.Frac;
				var thingTopSlope = (thing.Z + thing.Height - this.currentShooterZ) / dist;

				if (thingTopSlope < this.bottomSlope)
				{
					// Shot over the thing.
					return true;
				}

				var thingBottomSlope = (thing.Z - this.currentShooterZ) / dist;

				if (thingBottomSlope > this.topSlope)
				{
					// Shot under the thing.
					return true;
				}

				// This thing can be hit!
				if (thingTopSlope > this.topSlope)
				{
					thingTopSlope = this.topSlope;
				}

				if (thingBottomSlope < this.bottomSlope)
				{
					thingBottomSlope = this.bottomSlope;
				}

				this.currentAimSlope = (thingTopSlope + thingBottomSlope) / 2;
				this.lineTarget = thing;

				// Don't go any farther.
				return false;
			}
		}

		/// <summary>
		/// Fire a hitscan bullet along the aiming line.
		/// </summary>
		private bool ShootTraverse(Intercept intercept)
		{
			var mi = this.world.MapInteraction;
			var pt = this.world.PathTraversal;

			if (intercept.Line != null)
			{
				var line = intercept.Line;

				if (line.Special != 0)
				{
					mi.ShootSpecialLine(this.currentShooter, line);
				}

				if ((line.Flags & LineFlags.TwoSided) == 0)
				{
					goto hitLine;
				}

				var mc = this.world.MapCollision;

				// Crosses a two sided line.
				mc.LineOpening(line);

				var dist = this.currentRange * intercept.Frac;

				// Similar to AimTraverse, the code below is imported from Chocolate Doom.
				if (line.BackSector == null)
				{
					{
						var slope = (mc.OpenBottom - this.currentShooterZ) / dist;

						if (slope > this.currentAimSlope)
						{
							goto hitLine;
						}
					}

					{
						var slope = (mc.OpenTop - this.currentShooterZ) / dist;

						if (slope < this.currentAimSlope)
						{
							goto hitLine;
						}
					}
				}
				else
				{
					if (line.FrontSector.FloorHeight != line.BackSector.FloorHeight)
					{
						var slope = (mc.OpenBottom - this.currentShooterZ) / dist;

						if (slope > this.currentAimSlope)
						{
							goto hitLine;
						}
					}

					if (line.FrontSector.CeilingHeight != line.BackSector.CeilingHeight)
					{
						var slope = (mc.OpenTop - this.currentShooterZ) / dist;

						if (slope < this.currentAimSlope)
						{
							goto hitLine;
						}
					}
				}

				// Shot continues.
				return true;

				// Hit line.
				hitLine:

				// Position a bit closer.
				var frac = intercept.Frac - Fixed.FromInt(4) / this.currentRange;
				var x = pt.Trace.X + pt.Trace.Dx * frac;
				var y = pt.Trace.Y + pt.Trace.Dy * frac;
				var z = this.currentShooterZ + this.currentAimSlope * (frac * this.currentRange);

				if (line.FrontSector.CeilingFlat == this.world.Map.SkyFlatNumber)
				{
					// Don't shoot the sky!
					if (z > line.FrontSector.CeilingHeight)
					{
						return false;
					}

					// It's a sky hack wall.
					if (line.BackSector != null && line.BackSector.CeilingFlat == this.world.Map.SkyFlatNumber)
					{
						return false;
					}
				}

				// Spawn bullet puffs.
				this.SpawnPuff(x, y, z);

				// Don't go any farther.
				return false;
			}

			{
				// Shoot a thing.
				var thing = intercept.Thing;

				if (thing == this.currentShooter)
				{
					// Can't shoot self.
					return true;
				}

				if ((thing.Flags & MobjFlags.Shootable) == 0)
				{
					// Corpse or something.
					return true;
				}

				// Check angles to see if the thing can be aimed at.
				var dist = this.currentRange * intercept.Frac;
				var thingTopSlope = (thing.Z + thing.Height - this.currentShooterZ) / dist;

				if (thingTopSlope < this.currentAimSlope)
				{
					// Shot over the thing.
					return true;
				}

				var thingBottomSlope = (thing.Z - this.currentShooterZ) / dist;

				if (thingBottomSlope > this.currentAimSlope)
				{
					// Shot under the thing.
					return true;
				}

				// Hit thing.
				// Position a bit closer.
				var frac = intercept.Frac - Fixed.FromInt(10) / this.currentRange;

				var x = pt.Trace.X + pt.Trace.Dx * frac;
				var y = pt.Trace.Y + pt.Trace.Dy * frac;
				var z = this.currentShooterZ + this.currentAimSlope * (frac * this.currentRange);

				// Spawn bullet puffs or blod spots, depending on target type.
				if ((intercept.Thing.Flags & MobjFlags.NoBlood) != 0)
				{
					this.SpawnPuff(x, y, z);
				}
				else
				{
					this.SpawnBlood(x, y, z, this.currentDamage);
				}

				if (this.currentDamage != 0)
				{
					this.world.ThingInteraction.DamageMobj(thing, this.currentShooter, this.currentShooter, this.currentDamage);
				}

				// Don't go any farther.
				return false;
			}
		}

		/// <summary>
		/// Find a target on the aiming line.
		/// Sets LineTaget when a target is aimed at.
		/// </summary>
		public Fixed AimLineAttack(Mobj shooter, Angle angle, Fixed range)
		{
			this.currentShooter = shooter;
			this.currentShooterZ = shooter.Z + (shooter.Height >> 1) + Fixed.FromInt(8);
			this.currentRange = range;

			var targetX = shooter.X + range.ToIntFloor() * Trig.Cos(angle);
			var targetY = shooter.Y + range.ToIntFloor() * Trig.Sin(angle);

			// Can't shoot outside view angles.
			this.topSlope = Fixed.FromInt(100) / 160;
			this.bottomSlope = Fixed.FromInt(-100) / 160;

			this.lineTarget = null;

			this.world.PathTraversal.PathTraverse(
				shooter.X,
				shooter.Y,
				targetX,
				targetY,
				PathTraverseFlags.AddLines | PathTraverseFlags.AddThings,
				this.aimTraverseFunc
			);

			if (this.lineTarget != null)
			{
				return this.currentAimSlope;
			}

			return Fixed.Zero;
		}

		/// <summary>
		/// Fire a hitscan bullet.
		/// If damage == 0, it is just a test trace that will leave linetarget set.
		/// </summary>
		public void LineAttack(Mobj shooter, Angle angle, Fixed range, Fixed slope, int damage)
		{
			this.currentShooter = shooter;
			this.currentShooterZ = shooter.Z + (shooter.Height >> 1) + Fixed.FromInt(8);
			this.currentRange = range;
			this.currentAimSlope = slope;
			this.currentDamage = damage;

			var targetX = shooter.X + range.ToIntFloor() * Trig.Cos(angle);
			var targetY = shooter.Y + range.ToIntFloor() * Trig.Sin(angle);

			this.world.PathTraversal.PathTraverse(
				shooter.X,
				shooter.Y,
				targetX,
				targetY,
				PathTraverseFlags.AddLines | PathTraverseFlags.AddThings,
				this.shootTraverseFunc
			);
		}

		/// <summary>
		/// Spawn a bullet puff.
		/// </summary>
		public void SpawnPuff(Fixed x, Fixed y, Fixed z)
		{
			var random = this.world.Random;

			z += new Fixed((random.Next() - random.Next()) << 10);

			var thing = this.world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Puff);
			thing.MomZ = Fixed.One;
			thing.Tics -= random.Next() & 3;

			if (thing.Tics < 1)
			{
				thing.Tics = 1;
			}

			// Don't make punches spark on the wall.
			if (this.currentRange == WeaponBehavior.MeleeRange)
			{
				thing.SetState(MobjState.Puff3);
			}
		}

		/// <summary>
		/// Spawn blood.
		/// </summary>
		public void SpawnBlood(Fixed x, Fixed y, Fixed z, int damage)
		{
			var random = this.world.Random;

			z += new Fixed((random.Next() - random.Next()) << 10);

			var thing = this.world.ThingAllocation.SpawnMobj(x, y, z, MobjType.Blood);
			thing.MomZ = Fixed.FromInt(2);
			thing.Tics -= random.Next() & 3;

			if (thing.Tics < 1)
			{
				thing.Tics = 1;
			}

			if (damage <= 12 && damage >= 9)
			{
				thing.SetState(MobjState.Blood2);
			}
			else if (damage < 9)
			{
				thing.SetState(MobjState.Blood3);
			}
		}

		public Mobj LineTarget => this.lineTarget;
		public Fixed BottomSlope => this.bottomSlope;
		public Fixed TopSlope => this.topSlope;
	}
}
