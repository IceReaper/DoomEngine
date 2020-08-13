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

	public sealed class DivLine
    {
        private Fixed x;
        private Fixed y;
        private Fixed dx;
        private Fixed dy;

        public void MakeFrom(LineDef line)
        {
            this.x = line.Vertex1.X;
            this.y = line.Vertex1.Y;
            this.dx = line.Dx;
            this.dy = line.Dy;
        }

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

        public Fixed Dx
        {
            get => this.dx;
            set => this.dx = value;
        }

        public Fixed Dy
        {
            get => this.dy;
            set => this.dy = value;
        }
    }
}
