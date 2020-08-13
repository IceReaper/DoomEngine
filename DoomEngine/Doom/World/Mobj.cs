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
	using Graphics;
	using Info;
	using Map;
	using Math;

	public class Mobj : Thinker
    {
        //
        // NOTES: mobj_t
        //
        // mobj_ts are used to tell the refresh where to draw an image,
        // tell the world simulation when objects are contacted,
        // and tell the sound driver how to position a sound.
        //
        // The refresh uses the next and prev links to follow
        // lists of things in sectors as they are being drawn.
        // The sprite, frame, and angle elements determine which patch_t
        // is used to draw the sprite if it is visible.
        // The sprite and frame values are allmost allways set
        // from state_t structures.
        // The statescr.exe utility generates the states.h and states.c
        // files that contain the sprite/frame numbers from the
        // statescr.txt source file.
        // The xyz origin point represents a point at the bottom middle
        // of the sprite (between the feet of a biped).
        // This is the default origin position for patch_ts grabbed
        // with lumpy.exe.
        // A walking creature will have its z equal to the floor
        // it is standing on.
        //
        // The sound code uses the x,y, and subsector fields
        // to do stereo positioning of any sound effited by the mobj_t.
        //
        // The play simulation uses the blocklinks, x,y,z, radius, height
        // to determine when mobj_ts are touching each other,
        // touching lines in the map, or hit by trace lines (gunshots,
        // lines of sight, etc).
        // The mobj_t->flags element has various bit flags
        // used by the simulation.
        //
        // Every mobj_t is linked into a single sector
        // based on its origin coordinates.
        // The subsector_t is found with R_PointInSubsector(x,y),
        // and the sector_t can be found with subsector->sector.
        // The sector links are only used by the rendering code,
        // the play simulation does not care about them at all.
        //
        // Any mobj_t that needs to be acted upon by something else
        // in the play world (block movement, be shot, etc) will also
        // need to be linked into the blockmap.
        // If the thing has the MF_NOBLOCK flag set, it will not use
        // the block links. It can still interact with other things,
        // but only as the instigator (missiles will run into other
        // things, but nothing can run into a missile).
        // Each block in the grid is 128*128 units, and knows about
        // every line_t that it contains a piece of, and every
        // interactable mobj_t that has its origin contained.  
        //
        // A valid mobj_t is a mobj_t that has the proper subsector_t
        // filled in for its xy coordinates and is linked into the
        // sector from which the subsector was made, or has the
        // MF_NOSECTOR flag set (the subsector_t needs to be valid
        // even if MF_NOSECTOR is set), and is linked into a blockmap
        // block or has the MF_NOBLOCKMAP flag set.
        // Links should only be modified by the P_[Un]SetThingPosition()
        // functions.
        // Do not change the MF_NO? flags while a thing is valid.
        //
        // Any questions?
        //

        public static readonly Fixed OnFloorZ = Fixed.MinValue;
        public static readonly Fixed OnCeilingZ = Fixed.MaxValue;

        private World world;

        // Info for drawing: position.
        private Fixed x;
        private Fixed y;
        private Fixed z;

        // More list: links in sector (if needed).
        private Mobj sectorNext;
        private Mobj sectorPrev;

        // More drawing info: to determine current sprite.
        private Angle angle; // Orientation.
        private Sprite sprite; // Used to find patch_t and flip value.
        private int frame; // Might be ORed with FF_FULLBRIGHT.

        // Interaction info, by BLOCKMAP.
        // Links in blocks (if needed).
        private Mobj blockNext;
        private Mobj blockPrev;

        private Subsector subsector;

        // The closest interval over all contacted Sectors.
        private Fixed floorZ;
        private Fixed ceilingZ;

        // For movement checking.
        private Fixed radius;
        private Fixed height;

        // Momentums, used to update position.
        private Fixed momX;
        private Fixed momY;
        private Fixed momZ;

        // If == validCount, already checked.
        private int validCount;

        private MobjType type;
        private MobjInfo info;

        private int tics; // State tic counter.
        private MobjStateDef state;
        private MobjFlags flags;
        private int health;

        // Movement direction, movement generation (zig-zagging).
        private Direction moveDir;
        private int moveCount; // When 0, select a new dir.

        // Thing being chased / attacked (or null),
        // also the originator for missiles.
        private Mobj target;

        // Reaction time: if non 0, don't attack yet.
        // Used by player to freeze a bit after teleporting.
        private int reactionTime;

        // If >0, the target will be chased
        // no matter what (even if shot).
        private int threshold;

        // Additional info record for player avatars only.
        // Only valid if type == MT_PLAYER
        private Player player;

        // Player number last looked for.
        private int lastLook;

        // For nightmare respawn.
        private MapThing spawnPoint;

        // Thing being chased/attacked for tracers.
        private Mobj tracer;

        public Mobj(World world)
        {
            this.world = world;
        }

        public override void Run()
        {
            // Momentum movement.
            if (this.momX != Fixed.Zero || this.momY != Fixed.Zero ||
                (this.flags & MobjFlags.SkullFly) != 0)
            {
                this.world.ThingMovement.XYMovement(this);

                if (this.ThinkerState == ThinkerState.Removed)
                {
                    // Mobj was removed.
                    return;
                }
            }

            if ((this.z != this.floorZ) || this.momZ != Fixed.Zero)
            {
                this.world.ThingMovement.ZMovement(this);

                if (this.ThinkerState == ThinkerState.Removed)
                {
                    // Mobj was removed.
                    return;
                }
            }

            // Cycle through states,
            // calling action functions at transitions.
            if (this.tics != -1)
            {
                this.tics--;

                // You can cycle through multiple states in a tic.
                if (this.tics == 0)
                {
                    if (!this.SetState(this.state.Next))
                    {
                        // Freed itself.
                        return;
                    }
                }
            }
            else
            {
                // Check for nightmare respawn.
                if ((this.flags & MobjFlags.CountKill) == 0)
                {
                    return;
                }

                var options = this.world.Options;
                if (!(options.Skill == GameSkill.Nightmare || options.RespawnMonsters))
                {
                    return;
                }

                this.moveCount++;

                if (this.moveCount < 12 * 35)
                {
                    return;
                }

                if ((this.world.LevelTime & 31) != 0)
                {
                    return;
                }

                if (this.world.Random.Next() > 4)
                {
                    return;
                }

                this.NightmareRespawn();
            }
        }

        public bool SetState(MobjState state)
        {
            do
            {
                if (state == MobjState.Null)
                {
                    this.state = DoomInfo.States[(int)MobjState.Null];
                    this.world.ThingAllocation.RemoveMobj(this);
                    return false;
                }

                var st = DoomInfo.States[(int)state];
                this.state = st;
                this.tics = this.GetTics(st);
                this.sprite = st.Sprite;
                this.frame = st.Frame;

                // Modified handling.
                // Call action functions when the state is set.
                if (st.MobjAction != null)
                {
                    st.MobjAction(this.world, this);
                }

                state = st.Next;
            }
            while (this.tics == 0);

            return true;
        }

        private int GetTics(MobjStateDef state)
        {
            var options = this.world.Options;
            if (options.FastMonsters || options.Skill == GameSkill.Nightmare)
            {
                if ((int)MobjState.SargRun1 <= state.Number &&
                    state.Number <= (int)MobjState.SargPain2)
                {
                    return state.Tics >> 1;
                }
                else
                {
                    return state.Tics;
                }
            }
            else
            {
                return state.Tics;
            }
        }

        private void NightmareRespawn()
        {
            MapThing sp;
            if (this.spawnPoint != null)
            {
                sp = this.spawnPoint;
            }
            else
            {
                sp = MapThing.Empty;
            }

            // Somthing is occupying it's position?
            if (!this.world.ThingMovement.CheckPosition(this, sp.X, sp.Y))
            {
                // No respwan.
                return;
            }

            var ta = this.world.ThingAllocation;

            // Spawn a teleport fog at old spot.
            var fog1 = ta.SpawnMobj(
                this.x, this.y,
                this.subsector.Sector.FloorHeight,
                MobjType.Tfog);

            // Initiate teleport sound.
            this.world.StartSound(fog1, Sfx.TELEPT, SfxType.Misc);

            // Spawn a teleport fog at the new spot.
            var ss = Geometry.PointInSubsector(sp.X, sp.Y, this.world.Map);

            var fog2 = ta.SpawnMobj(
                sp.X, sp.Y,
                ss.Sector.FloorHeight, MobjType.Tfog);

            this.world.StartSound(fog2, Sfx.TELEPT, SfxType.Misc);

            // Spawn the new monster.
            Fixed z;
            if ((this.info.Flags & MobjFlags.SpawnCeiling) != 0)
            {
                z = Mobj.OnCeilingZ;
            }
            else
            {
                z = Mobj.OnFloorZ;
            }

            // Inherit attributes from deceased one.
            var mobj = ta.SpawnMobj(sp.X, sp.Y, z, this.type);
            mobj.SpawnPoint = this.spawnPoint;
            mobj.Angle = sp.Angle;

            if ((sp.Flags & ThingFlags.Ambush) != 0)
            {
                mobj.Flags |= MobjFlags.Ambush;
            }

            mobj.ReactionTime = 18;

            // Remove the old monster.
            this.world.ThingAllocation.RemoveMobj(this);
        }

        public World World => this.world;

        public Fixed X
        {
            get => this.x;
            set => this.x = value;
        }

        public Fixed Y
        {
            get => this.y;
            set => this.y = value;
        }

        public Fixed Z
        {
            get => this.z;
            set => this.z = value;
        }

        public Mobj SectorNext
        {
            get => this.sectorNext;
            set => this.sectorNext = value;
        }

        public Mobj SectorPrev
        {
            get => this.sectorPrev;
            set => this.sectorPrev = value;
        }

        public Angle Angle
        {
            get => this.angle;
            set => this.angle = value;
        }

        public Sprite Sprite
        {
            get => this.sprite;
            set => this.sprite = value;
        }

        public int Frame
        {
            get => this.frame;
            set => this.frame = value;
        }

        public Mobj BlockNext
        {
            get => this.blockNext;
            set => this.blockNext = value;
        }

        public Mobj BlockPrev
        {
            get => this.blockPrev;
            set => this.blockPrev = value;
        }

        public Subsector Subsector
        {
            get => this.subsector;
            set => this.subsector = value;
        }

        public Fixed FloorZ
        {
            get => this.floorZ;
            set => this.floorZ = value;
        }

        public Fixed CeilingZ
        {
            get => this.ceilingZ;
            set => this.ceilingZ = value;
        }

        public Fixed Radius
        {
            get => this.radius;
            set => this.radius = value;
        }

        public Fixed Height
        {
            get => this.height;
            set => this.height = value;
        }

        public Fixed MomX
        {
            get => this.momX;
            set => this.momX = value;
        }

        public Fixed MomY
        {
            get => this.momY;
            set => this.momY = value;
        }

        public Fixed MomZ
        {
            get => this.momZ;
            set => this.momZ = value;
        }

        public int ValidCount
        {
            get => this.validCount;
            set => this.validCount = value;
        }

        public MobjType Type
        {
            get => this.type;
            set => this.type = value;
        }

        public MobjInfo Info
        {
            get => this.info;
            set => this.info = value;
        }

        public int Tics
        {
            get => this.tics;
            set => this.tics = value;
        }

        public MobjStateDef State
        {
            get => this.state;
            set => this.state = value;
        }

        public MobjFlags Flags
        {
            get => this.flags;
            set => this.flags = value;
        }

        public int Health
        {
            get => this.health;
            set => this.health = value;
        }

        public Direction MoveDir
        {
            get => this.moveDir;
            set => this.moveDir = value;
        }

        public int MoveCount
        {
            get => this.moveCount;
            set => this.moveCount = value;
        }

        public Mobj Target
        {
            get => this.target;
            set => this.target = value;
        }

        public int ReactionTime
        {
            get => this.reactionTime;
            set => this.reactionTime = value;
        }

        public int Threshold
        {
            get => this.threshold;
            set => this.threshold = value;
        }

        public Player Player
        {
            get => this.player;
            set => this.player = value;
        }

        public int LastLook
        {
            get => this.lastLook;
            set => this.lastLook = value;
        }

        public MapThing SpawnPoint
        {
            get => this.spawnPoint;
            set => this.spawnPoint = value;
        }

        public Mobj Tracer
        {
            get => this.tracer;
            set => this.tracer = value;
        }
    }
}
