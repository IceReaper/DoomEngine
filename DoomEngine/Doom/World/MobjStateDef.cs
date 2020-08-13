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
	using Game;
	using Graphics;
	using System;

	public class MobjStateDef
    {
        private int number;
        private Sprite sprite;
        private int frame;
        private int tics;
        private Action<World, Player, PlayerSpriteDef> playerAction;
        private Action<World, Mobj> mobjAction;
        private MobjState next;
        private int misc1;
        private int misc2;

        public MobjStateDef(
            int number,
            Sprite sprite,
            int frame,
            int tics,
            Action<World, Player, PlayerSpriteDef> playerAction,
            Action<World, Mobj> mobjAction,
            MobjState next,
            int misc1,
            int misc2)
        {
            this.number = number;
            this.sprite = sprite;
            this.frame = frame;
            this.tics = tics;
            this.playerAction = playerAction;
            this.mobjAction = mobjAction;
            this.next = next;
            this.misc1 = misc1;
            this.misc2 = misc2;
        }

        public int Number
        {
            get => this.number;
            set => this.number = value;
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

        public int Tics
        {
            get => this.tics;
            set => this.tics = value;
        }

        public Action<World, Player, PlayerSpriteDef> PlayerAction
        {
            get => this.playerAction;
            set => this.playerAction = value;
        }

        public Action<World, Mobj> MobjAction
        {
            get => this.mobjAction;
            set => this.mobjAction = value;
        }

        public MobjState Next
        {
            get => this.next;
            set => this.next = value;
        }

        public int Misc1
        {
            get => this.misc1;
            set => this.misc1 = value;
        }

        public int Misc2
        {
            get => this.misc2;
            set => this.misc2 = value;
        }
    }
}
