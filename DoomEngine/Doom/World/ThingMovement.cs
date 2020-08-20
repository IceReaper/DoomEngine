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
	using Game;
	using Map;
	using Math;
	using System;

	public sealed class ThingMovement
	{
		private World world;

		public ThingMovement(World world)
		{
			this.world = world;

			this.InitThingMovement();
			this.InitSlideMovement();
			this.InitTeleportMovement();
		}

		////////////////////////////////////////////////////////////
		// General thing movement
		////////////////////////////////////////////////////////////

		public static readonly Fixed FloatSpeed = Fixed.FromInt(4);

		private static readonly int maxSpecialCrossCount = 20;
		private static readonly Fixed maxMove = Fixed.FromInt(30);
		private static readonly Fixed gravity = Fixed.One;

		private Mobj currentThing;
		private MobjFlags currentFlags;
		private Fixed currentX;
		private Fixed currentY;
		private Fixed[] currentBox;

		private Fixed currentFloorZ;
		private Fixed currentCeilingZ;
		private Fixed currentDropoffZ;
		private bool floatOk;

		private LineDef currentCeilingLine;

		public int crossedSpecialCount;
		public LineDef[] crossedSpecials;

		private Func<LineDef, bool> checkLineFunc;
		private Func<Mobj, bool> checkThingFunc;

		private void InitThingMovement()
		{
			this.currentBox = new Fixed[4];
			this.crossedSpecials = new LineDef[ThingMovement.maxSpecialCrossCount];
			this.checkLineFunc = this.CheckLine;
			this.checkThingFunc = this.CheckThing;
		}

		/// <summary>
		/// Links a thing into both a block and a subsector based on
		/// its x and y. Sets thing.Subsector properly.
		/// </summary>
		public void SetThingPosition(Mobj thing)
		{
			var map = this.world.Map;

			var subsector = Geometry.PointInSubsector(thing.X, thing.Y, map);

			thing.Subsector = subsector;

			// Invisible things don't go into the sector links.
			if ((thing.Flags & MobjFlags.NoSector) == 0)
			{
				var sector = subsector.Sector;

				thing.SectorPrev = null;
				thing.SectorNext = sector.ThingList;

				if (sector.ThingList != null)
				{
					sector.ThingList.SectorPrev = thing;
				}

				sector.ThingList = thing;
			}

			// Inert things don't need to be in blockmap.
			if ((thing.Flags & MobjFlags.NoBlockMap) == 0)
			{
				var index = map.BlockMap.GetIndex(thing.X, thing.Y);

				if (index != -1)
				{
					var link = map.BlockMap.ThingLists[index];

					thing.BlockPrev = null;
					thing.BlockNext = link;

					if (link != null)
					{
						link.BlockPrev = thing;
					}

					map.BlockMap.ThingLists[index] = thing;
				}
				else
				{
					// Thing is off the map.
					thing.BlockNext = null;
					thing.BlockPrev = null;
				}
			}
		}

		/// <summary>
		/// Unlinks a thing from block map and sectors.
		/// On each position change, BLOCKMAP and other lookups
		/// maintaining lists ot things inside these structures
		/// need to be updated.
		/// </summary>
		public void UnsetThingPosition(Mobj thing)
		{
			var map = this.world.Map;

			// Invisible things don't go into the sector links.
			if ((thing.Flags & MobjFlags.NoSector) == 0)
			{
				// Unlink from subsector.
				if (thing.SectorNext != null)
				{
					thing.SectorNext.SectorPrev = thing.SectorPrev;
				}

				if (thing.SectorPrev != null)
				{
					thing.SectorPrev.SectorNext = thing.SectorNext;
				}
				else
				{
					thing.Subsector.Sector.ThingList = thing.SectorNext;
				}
			}

			// Inert things don't need to be in blockmap.
			if ((thing.Flags & MobjFlags.NoBlockMap) == 0)
			{
				// Unlink from block map.
				if (thing.BlockNext != null)
				{
					thing.BlockNext.BlockPrev = thing.BlockPrev;
				}

				if (thing.BlockPrev != null)
				{
					thing.BlockPrev.BlockNext = thing.BlockNext;
				}
				else
				{
					var index = map.BlockMap.GetIndex(thing.X, thing.Y);

					if (index != -1)
					{
						map.BlockMap.ThingLists[index] = thing.BlockNext;
					}
				}
			}
		}

		/// <summary>
		/// Adjusts currentFloorZ and currentCeilingZ as lines are contacted.
		/// </summary>
		private bool CheckLine(LineDef line)
		{
			var mc = this.world.MapCollision;

			if (this.currentBox.Right() <= line.BoundingBox.Left()
				|| this.currentBox.Left() >= line.BoundingBox.Right()
				|| this.currentBox.Top() <= line.BoundingBox.Bottom()
				|| this.currentBox.Bottom() >= line.BoundingBox.Top())
			{
				return true;
			}

			if (Geometry.BoxOnLineSide(this.currentBox, line) != -1)
			{
				return true;
			}

			// A line has been hit.
			//
			// The moving thing's destination position will cross the given line.
			// If this should not be allowed, return false.
			// If the line is special, keep track of it to process later if the move is proven ok.
			//
			// NOTE:
			//     specials are NOT sorted by order, so two special lines that are only 8 pixels
			//     apart could be crossed in either order.

			if (line.BackSector == null)
			{
				// One sided line.
				return false;
			}

			if ((this.currentThing.Flags & MobjFlags.Missile) == 0)
			{
				if ((line.Flags & LineFlags.Blocking) != 0)
				{
					// Explicitly blocking everything.
					return false;
				}

				if (this.currentThing.Player == null && (line.Flags & LineFlags.BlockMonsters) != 0)
				{
					// Block monsters only.
					return false;
				}
			}

			// Set openrange, opentop, openbottom.
			mc.LineOpening(line);

			// Adjust floor / ceiling heights.
			if (mc.OpenTop < this.currentCeilingZ)
			{
				this.currentCeilingZ = mc.OpenTop;
				this.currentCeilingLine = line;
			}

			if (mc.OpenBottom > this.currentFloorZ)
			{
				this.currentFloorZ = mc.OpenBottom;
			}

			if (mc.LowFloor < this.currentDropoffZ)
			{
				this.currentDropoffZ = mc.LowFloor;
			}

			// If contacted a special line, add it to the list.
			if (line.Special != 0)
			{
				this.crossedSpecials[this.crossedSpecialCount] = line;
				this.crossedSpecialCount++;
			}

			return true;
		}

		private bool CheckThing(Mobj thing)
		{
			if ((thing.Flags & (MobjFlags.Solid | MobjFlags.Special | MobjFlags.Shootable)) == 0)
			{
				return true;
			}

			var blockDist = thing.Radius + this.currentThing.Radius;

			if (Fixed.Abs(thing.X - this.currentX) >= blockDist || Fixed.Abs(thing.Y - this.currentY) >= blockDist)
			{
				// Didn't hit it.
				return true;
			}

			// Don't clip against self.
			if (thing == this.currentThing)
			{
				return true;
			}

			// Check for skulls slamming into things.
			if ((this.currentThing.Flags & MobjFlags.SkullFly) != 0)
			{
				var damage = ((this.world.Random.Next() % 8) + 1) * this.currentThing.Info.Damage;

				this.world.ThingInteraction.DamageMobj(thing, this.currentThing, this.currentThing, damage);

				this.currentThing.Flags &= ~MobjFlags.SkullFly;
				this.currentThing.MomX = this.currentThing.MomY = this.currentThing.MomZ = Fixed.Zero;

				this.currentThing.SetState(this.currentThing.Info.SpawnState);

				// Stop moving.
				return false;
			}

			// Missiles can hit other things.
			if ((this.currentThing.Flags & MobjFlags.Missile) != 0)
			{
				// See if it went over / under.
				if (this.currentThing.Z > thing.Z + thing.Height)
				{
					// Overhead.
					return true;
				}

				if (this.currentThing.Z + this.currentThing.Height < thing.Z)
				{
					// Underneath.
					return true;
				}

				if (this.currentThing.Target != null
					&& (this.currentThing.Target.Type == thing.Type
						|| (this.currentThing.Target.Type == MobjType.Knight && thing.Type == MobjType.Bruiser)
						|| (this.currentThing.Target.Type == MobjType.Bruiser && thing.Type == MobjType.Knight)))
				{
					// Don't hit same species as originator.
					if (thing == this.currentThing.Target)
					{
						return true;
					}

					if (thing.Type != MobjType.Player)
					{
						// Explode, but do no damage.
						// Let players missile other players.
						return false;
					}
				}

				if ((thing.Flags & MobjFlags.Shootable) == 0)
				{
					// Didn't do any damage.
					return (thing.Flags & MobjFlags.Solid) == 0;
				}

				// Damage / explode.
				var damage = ((this.world.Random.Next() % 8) + 1) * this.currentThing.Info.Damage;
				this.world.ThingInteraction.DamageMobj(thing, this.currentThing, this.currentThing.Target, damage);

				// Don't traverse any more.
				return false;
			}

			// Check for special pickup.
			if ((thing.Flags & MobjFlags.Special) != 0)
			{
				var solid = (thing.Flags & MobjFlags.Solid) != 0;

				if ((this.currentFlags & MobjFlags.PickUp) != 0)
				{
					// Can remove thing.
					this.world.ItemPickup.TouchSpecialThing(thing, this.currentThing);
				}

				return !solid;
			}

			return (thing.Flags & MobjFlags.Solid) == 0;
		}

		/// <summary>
		/// This is purely informative, nothing is modified
		/// (except things picked up).
		///
		/// In:
		///     A Mobj (can be valid or invalid)
		///     A position to be checked
		///     (doesn't need to be related to the mobj.X and Y)
		///
		/// During:
		///     Special things are touched if MobjFlags.PickUp
		///     Early out on solid lines?
		///
		/// Out:
		///     New subsector
		///     CurrentFloorZ
		///     CurrentCeilingZ
		///     CurrentDropoffZ
		///     The lowest point contacted
		///     (monsters won't move to a dropoff)
		///     crossedSpecials[]
		///     crossedSpecialCount
		/// </summary>
		public bool CheckPosition(Mobj thing, Fixed x, Fixed y)
		{
			var map = this.world.Map;
			var bm = map.BlockMap;

			this.currentThing = thing;
			this.currentFlags = thing.Flags;

			this.currentX = x;
			this.currentY = y;

			this.currentBox[Box.Top] = y + this.currentThing.Radius;
			this.currentBox[Box.Bottom] = y - this.currentThing.Radius;
			this.currentBox[Box.Right] = x + this.currentThing.Radius;
			this.currentBox[Box.Left] = x - this.currentThing.Radius;

			var newSubsector = Geometry.PointInSubsector(x, y, map);

			this.currentCeilingLine = null;

			// The base floor / ceiling is from the subsector that contains the point.
			// Any contacted lines the step closer together will adjust them.
			this.currentFloorZ = this.currentDropoffZ = newSubsector.Sector.FloorHeight;
			this.currentCeilingZ = newSubsector.Sector.CeilingHeight;

			var validCount = this.world.GetNewValidCount();

			this.crossedSpecialCount = 0;

			if ((this.currentFlags & MobjFlags.NoClip) != 0)
			{
				return true;
			}

			// Check things first, possibly picking things up.
			// The bounding box is extended by MaxThingRadius because mobj_ts are grouped into
			// mapblocks based on their origin point, and can overlap into adjacent blocks by up
			// to MaxThingRadius units.
			{
				var blockX1 = bm.GetBlockX(this.currentBox[Box.Left] - GameConst.MaxThingRadius);
				var blockX2 = bm.GetBlockX(this.currentBox[Box.Right] + GameConst.MaxThingRadius);
				var blockY1 = bm.GetBlockY(this.currentBox[Box.Bottom] - GameConst.MaxThingRadius);
				var blockY2 = bm.GetBlockY(this.currentBox[Box.Top] + GameConst.MaxThingRadius);

				for (var bx = blockX1; bx <= blockX2; bx++)
				{
					for (var by = blockY1; by <= blockY2; by++)
					{
						if (!map.BlockMap.IterateThings(bx, by, this.checkThingFunc))
						{
							return false;
						}
					}
				}
			}

			// Check lines.
			{
				var blockX1 = bm.GetBlockX(this.currentBox[Box.Left]);
				var blockX2 = bm.GetBlockX(this.currentBox[Box.Right]);
				var blockY1 = bm.GetBlockY(this.currentBox[Box.Bottom]);
				var blockY2 = bm.GetBlockY(this.currentBox[Box.Top]);

				for (var bx = blockX1; bx <= blockX2; bx++)
				{
					for (var by = blockY1; by <= blockY2; by++)
					{
						if (!map.BlockMap.IterateLines(bx, by, this.checkLineFunc, validCount))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Attempt to move to a new position, crossing special lines unless
		/// MobjFlags.Teleport is set.
		/// </summary>
		public bool TryMove(Mobj thing, Fixed x, Fixed y)
		{
			this.floatOk = false;

			if (!this.CheckPosition(thing, x, y))
			{
				// Solid wall or thing.
				return false;
			}

			if ((thing.Flags & MobjFlags.NoClip) == 0)
			{
				if (this.currentCeilingZ - this.currentFloorZ < thing.Height)
				{
					// Doesn't fit.
					return false;
				}

				this.floatOk = true;

				if ((thing.Flags & MobjFlags.Teleport) == 0 && this.currentCeilingZ - thing.Z < thing.Height)
				{
					// Mobj must lower itself to fit.
					return false;
				}

				if ((thing.Flags & MobjFlags.Teleport) == 0 && this.currentFloorZ - thing.Z > Fixed.FromInt(24))
				{
					// Too big a step up.
					return false;
				}

				if ((thing.Flags & (MobjFlags.DropOff | MobjFlags.Float)) == 0 && this.currentFloorZ - this.currentDropoffZ > Fixed.FromInt(24))
				{
					// Don't stand over a dropoff.
					return false;
				}
			}

			// The move is ok,
			// so link the thing into its new position.
			this.UnsetThingPosition(thing);

			var oldx = thing.X;
			var oldy = thing.Y;
			thing.FloorZ = this.currentFloorZ;
			thing.CeilingZ = this.currentCeilingZ;
			thing.X = x;
			thing.Y = y;

			this.SetThingPosition(thing);

			// If any special lines were hit, do the effect.
			if ((thing.Flags & (MobjFlags.Teleport | MobjFlags.NoClip)) == 0)
			{
				while (this.crossedSpecialCount-- > 0)
				{
					// See if the line was crossed.
					var line = this.crossedSpecials[this.crossedSpecialCount];
					var newSide = Geometry.PointOnLineSide(thing.X, thing.Y, line);
					var oldSide = Geometry.PointOnLineSide(oldx, oldy, line);

					if (newSide != oldSide)
					{
						if (line.Special != 0)
						{
							this.world.MapInteraction.CrossSpecialLine(line, oldSide, thing);
						}
					}
				}
			}

			return true;
		}

		private static readonly Fixed stopSpeed = new Fixed(0x1000);
		private static readonly Fixed friction = new Fixed(0xe800);

		public void XYMovement(Mobj thing)
		{
			if (thing.MomX == Fixed.Zero && thing.MomY == Fixed.Zero)
			{
				if ((thing.Flags & MobjFlags.SkullFly) != 0)
				{
					// The skull slammed into something.
					thing.Flags &= ~MobjFlags.SkullFly;
					thing.MomX = thing.MomY = thing.MomZ = Fixed.Zero;

					thing.SetState(thing.Info.SpawnState);
				}

				return;
			}

			var player = thing.Player;

			if (thing.MomX > ThingMovement.maxMove)
			{
				thing.MomX = ThingMovement.maxMove;
			}
			else if (thing.MomX < -ThingMovement.maxMove)
			{
				thing.MomX = -ThingMovement.maxMove;
			}

			if (thing.MomY > ThingMovement.maxMove)
			{
				thing.MomY = ThingMovement.maxMove;
			}
			else if (thing.MomY < -ThingMovement.maxMove)
			{
				thing.MomY = -ThingMovement.maxMove;
			}

			var moveX = thing.MomX;
			var moveY = thing.MomY;

			do
			{
				Fixed pMoveX;
				Fixed pMoveY;

				if (moveX > ThingMovement.maxMove / 2 || moveY > ThingMovement.maxMove / 2)
				{
					pMoveX = thing.X + moveX / 2;
					pMoveY = thing.Y + moveY / 2;
					moveX >>= 1;
					moveY >>= 1;
				}
				else
				{
					pMoveX = thing.X + moveX;
					pMoveY = thing.Y + moveY;
					moveX = moveY = Fixed.Zero;
				}

				if (!this.TryMove(thing, pMoveX, pMoveY))
				{
					// Blocked move.
					if (thing.Player != null)
					{
						// Try to slide along it.
						this.SlideMove(thing);
					}
					else if ((thing.Flags & MobjFlags.Missile) != 0)
					{
						// Explode a missile.
						if (this.currentCeilingLine != null
							&& this.currentCeilingLine.BackSector != null
							&& this.currentCeilingLine.BackSector.CeilingFlat == this.world.Map.SkyFlatNumber)
						{
							// Hack to prevent missiles exploding against the sky.
							// Does not handle sky floors.
							this.world.ThingAllocation.RemoveMobj(thing);

							return;
						}

						this.world.ThingInteraction.ExplodeMissile(thing);
					}
					else
					{
						thing.MomX = thing.MomY = Fixed.Zero;
					}
				}
			}
			while (moveX != Fixed.Zero || moveY != Fixed.Zero);

			// Slow down.
			if (player != null && (player.Cheats & CheatFlags.NoMomentum) != 0)
			{
				// Debug option for no sliding at all.
				thing.MomX = thing.MomY = Fixed.Zero;

				return;
			}

			if ((thing.Flags & (MobjFlags.Missile | MobjFlags.SkullFly)) != 0)
			{
				// No friction for missiles ever.
				return;
			}

			if (thing.Z > thing.FloorZ)
			{
				// No friction when airborne.
				return;
			}

			if ((thing.Flags & MobjFlags.Corpse) != 0)
			{
				// Do not stop sliding if halfway off a step with some momentum.
				if (thing.MomX > Fixed.One / 4 || thing.MomX < -Fixed.One / 4 || thing.MomY > Fixed.One / 4 || thing.MomY < -Fixed.One / 4)
				{
					if (thing.FloorZ != thing.Subsector.Sector.FloorHeight)
					{
						return;
					}
				}
			}

			if (thing.MomX > -ThingMovement.stopSpeed
				&& thing.MomX < ThingMovement.stopSpeed
				&& thing.MomY > -ThingMovement.stopSpeed
				&& thing.MomY < ThingMovement.stopSpeed
				&& (player == null || (player.Cmd.ForwardMove == 0 && player.Cmd.SideMove == 0)))
			{
				// If in a walking frame, stop moving.
				if (player != null && (player.Mobj.State.Number - (int) MobjState.PlayRun1) < 4)
				{
					player.Mobj.SetState(MobjState.Play);
				}

				thing.MomX = Fixed.Zero;
				thing.MomY = Fixed.Zero;
			}
			else
			{
				thing.MomX = thing.MomX * ThingMovement.friction;
				thing.MomY = thing.MomY * ThingMovement.friction;
			}
		}

		public void ZMovement(Mobj thing)
		{
			// Check for smooth step up.
			if (thing.Player != null && thing.Z < thing.FloorZ)
			{
				thing.Player.ViewHeight -= thing.FloorZ - thing.Z;

				thing.Player.DeltaViewHeight = (Player.NormalViewHeight - thing.Player.ViewHeight) >> 3;
			}

			// Adjust height.
			thing.Z += thing.MomZ;

			if ((thing.Flags & MobjFlags.Float) != 0 && thing.Target != null)
			{
				// Float down towards target if too close.
				if ((thing.Flags & MobjFlags.SkullFly) == 0 && (thing.Flags & MobjFlags.InFloat) == 0)
				{
					var dist = Geometry.AproxDistance(thing.X - thing.Target.X, thing.Y - thing.Target.Y);

					var delta = (thing.Target.Z + (thing.Height >> 1)) - thing.Z;

					if (delta < Fixed.Zero && dist < -(delta * 3))
					{
						thing.Z -= ThingMovement.FloatSpeed;
					}
					else if (delta > Fixed.Zero && dist < (delta * 3))
					{
						thing.Z += ThingMovement.FloatSpeed;
					}
				}
			}

			// Clip movement.
			if (thing.Z <= thing.FloorZ)
			{
				// Hit the floor.

				//
				// The lost soul bounce fix below is based on Chocolate Doom's implementation.
				//

				var correctLostSoulBounce = DoomApplication.Instance.IWad != "doom2" && DoomApplication.Instance.IWad != "freedoom2";

				if (correctLostSoulBounce && (thing.Flags & MobjFlags.SkullFly) != 0)
				{
					// The skull slammed into something.
					thing.MomZ = -thing.MomZ;
				}

				if (thing.MomZ < Fixed.Zero)
				{
					if (thing.Player != null && thing.MomZ < -ThingMovement.gravity * 8)
					{
						// Squat down.
						// Decrease viewheight for a moment after hitting the ground (hard),
						// and utter appropriate sound.
						thing.Player.DeltaViewHeight = (thing.MomZ >> 3);
						this.world.StartSound(thing, Sfx.OOF, SfxType.Voice);
					}

					thing.MomZ = Fixed.Zero;
				}

				thing.Z = thing.FloorZ;

				if (!correctLostSoulBounce && (thing.Flags & MobjFlags.SkullFly) != 0)
				{
					thing.MomZ = -thing.MomZ;
				}

				if ((thing.Flags & MobjFlags.Missile) != 0 && (thing.Flags & MobjFlags.NoClip) == 0)
				{
					this.world.ThingInteraction.ExplodeMissile(thing);

					return;
				}
			}
			else if ((thing.Flags & MobjFlags.NoGravity) == 0)
			{
				if (thing.MomZ == Fixed.Zero)
				{
					thing.MomZ = -ThingMovement.gravity * 2;
				}
				else
				{
					thing.MomZ -= ThingMovement.gravity;
				}
			}

			if (thing.Z + thing.Height > thing.CeilingZ)
			{
				// Hit the ceiling.
				if (thing.MomZ > Fixed.Zero)
				{
					thing.MomZ = Fixed.Zero;
				}

				{
					thing.Z = thing.CeilingZ - thing.Height;
				}

				if ((thing.Flags & MobjFlags.SkullFly) != 0)
				{
					// The skull slammed into something.
					thing.MomZ = -thing.MomZ;
				}

				if ((thing.Flags & MobjFlags.Missile) != 0 && (thing.Flags & MobjFlags.NoClip) == 0)
				{
					this.world.ThingInteraction.ExplodeMissile(thing);

					return;
				}
			}
		}

		public Fixed CurrentFloorZ => this.currentFloorZ;
		public Fixed CurrentCeilingZ => this.currentCeilingZ;
		public Fixed CurrentDropoffZ => this.currentDropoffZ;
		public bool FloatOk => this.floatOk;

		////////////////////////////////////////////////////////////
		// Player's slide movement
		////////////////////////////////////////////////////////////

		private Fixed bestSlideFrac;
		private Fixed secondSlideFrac;

		private LineDef bestSlideLine;
		private LineDef secondSlideLine;

		private Mobj slideThing;
		private Fixed slideMoveX;
		private Fixed slideMoveY;

		private Func<Intercept, bool> slideTraverseFunc;

		private void InitSlideMovement()
		{
			this.slideTraverseFunc = this.SlideTraverse;
		}

		/// <summary>
		/// Adjusts the x and y movement so that the next move will
		/// slide along the wall.
		/// </summary>
		private void HitSlideLine(LineDef line)
		{
			if (line.SlopeType == SlopeType.Horizontal)
			{
				this.slideMoveY = Fixed.Zero;

				return;
			}

			if (line.SlopeType == SlopeType.Vertical)
			{
				this.slideMoveX = Fixed.Zero;

				return;
			}

			var side = Geometry.PointOnLineSide(this.slideThing.X, this.slideThing.Y, line);

			var lineAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy);

			if (side == 1)
			{
				lineAngle += Angle.Ang180;
			}

			var moveAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, this.slideMoveX, this.slideMoveY);

			var deltaAngle = moveAngle - lineAngle;

			if (deltaAngle > Angle.Ang180)
			{
				deltaAngle += Angle.Ang180;
			}

			var moveDist = Geometry.AproxDistance(this.slideMoveX, this.slideMoveY);
			var newDist = moveDist * Trig.Cos(deltaAngle);

			this.slideMoveX = newDist * Trig.Cos(lineAngle);
			this.slideMoveY = newDist * Trig.Sin(lineAngle);
		}

		private bool SlideTraverse(Intercept intercept)
		{
			var mc = this.world.MapCollision;

			if (intercept.Line == null)
			{
				throw new Exception("ThingMovement.SlideTraverse: Not a line?");
			}

			var line = intercept.Line;

			if ((line.Flags & LineFlags.TwoSided) == 0)
			{
				if (Geometry.PointOnLineSide(this.slideThing.X, this.slideThing.Y, line) != 0)
				{
					// Don't hit the back side.
					return true;
				}

				goto isBlocking;
			}

			// Set openrange, opentop, openbottom.
			mc.LineOpening(line);

			if (mc.OpenRange < this.slideThing.Height)
			{
				// Doesn't fit.
				goto isBlocking;
			}

			if (mc.OpenTop - this.slideThing.Z < this.slideThing.Height)
			{
				// Mobj is too high.
				goto isBlocking;
			}

			if (mc.OpenBottom - this.slideThing.Z > Fixed.FromInt(24))
			{
				// Too big a step up.
				goto isBlocking;
			}

			// This line doesn't block movement.
			return true;

			// The line does block movement, see if it is closer than best so far.
			isBlocking:

			if (intercept.Frac < this.bestSlideFrac)
			{
				this.secondSlideFrac = this.bestSlideFrac;
				this.secondSlideLine = this.bestSlideLine;
				this.bestSlideFrac = intercept.Frac;
				this.bestSlideLine = line;
			}

			// Stop.
			return false;
		}

		/// <summary>
		/// The MomX / MomY move is bad, so try to slide along a wall.
		/// Find the first line hit, move flush to it, and slide along it.
		/// This is a kludgy mess.
		/// </summary>
		private void SlideMove(Mobj thing)
		{
			var pt = this.world.PathTraversal;

			this.slideThing = thing;

			var hitCount = 0;

			retry:

			// Don't loop forever.
			if (++hitCount == 3)
			{
				// The move most have hit the middle, so stairstep.
				this.StairStep(thing);

				return;
			}

			Fixed leadX;
			Fixed leadY;
			Fixed trailX;
			Fixed trailY;

			// Trace along the three leading corners.
			if (thing.MomX > Fixed.Zero)
			{
				leadX = thing.X + thing.Radius;
				trailX = thing.X - thing.Radius;
			}
			else
			{
				leadX = thing.X - thing.Radius;
				trailX = thing.X + thing.Radius;
			}

			if (thing.MomY > Fixed.Zero)
			{
				leadY = thing.Y + thing.Radius;
				trailY = thing.Y - thing.Radius;
			}
			else
			{
				leadY = thing.Y - thing.Radius;
				trailY = thing.Y + thing.Radius;
			}

			this.bestSlideFrac = Fixed.OnePlusEpsilon;

			pt.PathTraverse(leadX, leadY, leadX + thing.MomX, leadY + thing.MomY, PathTraverseFlags.AddLines, this.slideTraverseFunc);

			pt.PathTraverse(trailX, leadY, trailX + thing.MomX, leadY + thing.MomY, PathTraverseFlags.AddLines, this.slideTraverseFunc);

			pt.PathTraverse(leadX, trailY, leadX + thing.MomX, trailY + thing.MomY, PathTraverseFlags.AddLines, this.slideTraverseFunc);

			// Move up to the wall.
			if (this.bestSlideFrac == Fixed.OnePlusEpsilon)
			{
				// The move most have hit the middle, so stairstep.
				this.StairStep(thing);

				return;
			}

			// Fudge a bit to make sure it doesn't hit.
			this.bestSlideFrac = new Fixed(this.bestSlideFrac.Data - 0x800);

			if (this.bestSlideFrac > Fixed.Zero)
			{
				var newX = thing.MomX * this.bestSlideFrac;
				var newY = thing.MomY * this.bestSlideFrac;

				if (!this.TryMove(thing, thing.X + newX, thing.Y + newY))
				{
					// The move most have hit the middle, so stairstep.
					this.StairStep(thing);

					return;
				}
			}

			// Now continue along the wall.
			// First calculate remainder.
			this.bestSlideFrac = new Fixed(Fixed.FracUnit - (this.bestSlideFrac.Data + 0x800));

			if (this.bestSlideFrac > Fixed.One)
			{
				this.bestSlideFrac = Fixed.One;
			}

			if (this.bestSlideFrac <= Fixed.Zero)
			{
				return;
			}

			this.slideMoveX = thing.MomX * this.bestSlideFrac;
			this.slideMoveY = thing.MomY * this.bestSlideFrac;

			// Clip the moves.
			this.HitSlideLine(this.bestSlideLine);

			thing.MomX = this.slideMoveX;
			thing.MomY = this.slideMoveY;

			if (!this.TryMove(thing, thing.X + this.slideMoveX, thing.Y + this.slideMoveY))
			{
				goto retry;
			}
		}

		private void StairStep(Mobj thing)
		{
			if (!this.TryMove(thing, thing.X, thing.Y + thing.MomY))
			{
				this.TryMove(thing, thing.X + thing.MomX, thing.Y);
			}
		}

		////////////////////////////////////////////////////////////
		// Teleport movement
		////////////////////////////////////////////////////////////

		private Func<Mobj, bool> stompThingFunc;

		private void InitTeleportMovement()
		{
			this.stompThingFunc = this.StompThing;
		}

		private bool StompThing(Mobj thing)
		{
			if ((thing.Flags & MobjFlags.Shootable) == 0)
			{
				return true;
			}

			var blockDist = thing.Radius + this.currentThing.Radius;
			var dx = Fixed.Abs(thing.X - this.currentX);
			var dy = Fixed.Abs(thing.Y - this.currentY);

			if (dx >= blockDist || dy >= blockDist)
			{
				// Didn't hit it.
				return true;
			}

			// Don't clip against self.
			if (thing == this.currentThing)
			{
				return true;
			}

			// Monsters don't stomp things except on boss level.
			if (this.currentThing.Player == null && this.world.Options.Map != 30)
			{
				return false;
			}

			this.world.ThingInteraction.DamageMobj(thing, this.currentThing, this.currentThing, 10000);

			return true;
		}

		public bool TeleportMove(Mobj thing, Fixed x, Fixed y)
		{
			// Kill anything occupying the position.
			this.currentThing = thing;
			this.currentFlags = thing.Flags;

			this.currentX = x;
			this.currentY = y;

			this.currentBox[Box.Top] = y + this.currentThing.Radius;
			this.currentBox[Box.Bottom] = y - this.currentThing.Radius;
			this.currentBox[Box.Right] = x + this.currentThing.Radius;
			this.currentBox[Box.Left] = x - this.currentThing.Radius;

			var ss = Geometry.PointInSubsector(x, y, this.world.Map);

			this.currentCeilingLine = null;

			// The base floor / ceiling is from the subsector that contains the point.
			// Any contacted lines the step closer together will adjust them.
			this.currentFloorZ = this.currentDropoffZ = ss.Sector.FloorHeight;
			this.currentCeilingZ = ss.Sector.CeilingHeight;

			var validcount = this.world.GetNewValidCount();

			this.crossedSpecialCount = 0;

			// Stomp on any things contacted.
			var bm = this.world.Map.BlockMap;
			var blockX1 = bm.GetBlockX(this.currentBox[Box.Left] - GameConst.MaxThingRadius);
			var blockX2 = bm.GetBlockX(this.currentBox[Box.Right] + GameConst.MaxThingRadius);
			var blockY1 = bm.GetBlockY(this.currentBox[Box.Bottom] - GameConst.MaxThingRadius);
			var blockY2 = bm.GetBlockY(this.currentBox[Box.Top] + GameConst.MaxThingRadius);

			for (var bx = blockX1; bx <= blockX2; bx++)
			{
				for (var by = blockY1; by <= blockY2; by++)
				{
					if (!bm.IterateThings(bx, by, this.stompThingFunc))
					{
						return false;
					}
				}
			}

			// the move is ok, so link the thing into its new position
			this.UnsetThingPosition(thing);

			thing.FloorZ = this.currentFloorZ;
			thing.CeilingZ = this.currentCeilingZ;
			thing.X = x;
			thing.Y = y;

			this.SetThingPosition(thing);

			return true;
		}
	}
}
