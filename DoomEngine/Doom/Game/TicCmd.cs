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

namespace DoomEngine.Doom.Game
{
	public sealed class TicCmd
    {
        private sbyte forwardMove;
        private sbyte sideMove;
        private short angleTurn;
        private byte buttons;

        public void Clear()
        {
            this.forwardMove = 0;
            this.sideMove = 0;
            this.angleTurn = 0;
            this.buttons = 0;
        }

        public void CopyFrom(TicCmd cmd)
        {
            this.forwardMove = cmd.forwardMove;
            this.sideMove = cmd.sideMove;
            this.angleTurn = cmd.angleTurn;
            this.buttons = cmd.buttons;
        }

        public sbyte ForwardMove
        {
            get => this.forwardMove;
            set => this.forwardMove = value;
        }

        public sbyte SideMove
        {
            get => this.sideMove;
            set => this.sideMove = value;
        }

        public short AngleTurn
        {
            get => this.angleTurn;
            set => this.angleTurn = value;
        }

        public byte Buttons
        {
            get => this.buttons;
            set => this.buttons = value;
        }
    }
}
