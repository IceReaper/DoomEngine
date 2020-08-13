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

	public sealed class MapCollision
    {
        private World world;

        private Fixed openTop;
        private Fixed openBottom;
        private Fixed openRange;
        private Fixed lowFloor;

        public MapCollision(World world)
        {
            this.world = world;
        }

        /// <summary>
        /// Sets opentop and openbottom to the window through a two sided line.
        /// </summary>
        public void LineOpening(LineDef line)
        {
            if (line.BackSide == null)
            {
                // If the line is single sided, nothing can pass through.
                this.openRange = Fixed.Zero;
                return;
            }

            var front = line.FrontSector;
            var back = line.BackSector;

            if (front.CeilingHeight < back.CeilingHeight)
            {
                this.openTop = front.CeilingHeight;
            }
            else
            {
                this.openTop = back.CeilingHeight;
            }

            if (front.FloorHeight > back.FloorHeight)
            {
                this.openBottom = front.FloorHeight;
                this.lowFloor = back.FloorHeight;
            }
            else
            {
                this.openBottom = back.FloorHeight;
                this.lowFloor = front.FloorHeight;
            }

            this.openRange = this.openTop - this.openBottom;
        }

        public Fixed OpenTop => this.openTop;
        public Fixed OpenBottom => this.openBottom;
        public Fixed OpenRange => this.openRange;
        public Fixed LowFloor => this.lowFloor;
    }
}
