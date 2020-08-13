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

namespace DoomEngine.Doom.Menu
{
	using System;

	public class SliderMenuItem : MenuItem
    {
        private string name;
        private int itemX;
        private int itemY;

        private int sliderLength;
        private int sliderPosition;

        private Func<int> reset;
        private Action<int> action;

        public SliderMenuItem(
            string name,
            int skullX, int skullY,
            int itemX, int itemY,
            int sliderLength,
            Func<int> reset,
            Action<int> action)
            : base(skullX, skullY, null)
        {
            this.name = name;
            this.itemX = itemX;
            this.itemY = itemY;

            this.sliderLength = sliderLength;
            this.sliderPosition = 0;

            this.action = action;
            this.reset = reset;
        }

        public void Reset()
        {
            if (this.reset != null)
            {
                this.sliderPosition = this.reset();
            }
        }

        public void Up()
        {
            if (this.sliderPosition < this.SliderLength - 1)
            {
                this.sliderPosition++;
            }

            if (this.action != null)
            {
                this.action(this.sliderPosition);
            }
        }

        public void Down()
        {
            if (this.sliderPosition > 0)
            {
                this.sliderPosition--;
            }

            if (this.action != null)
            {
                this.action(this.sliderPosition);
            }
        }

        public string Name => this.name;
        public int ItemX => this.itemX;
        public int ItemY => this.itemY;

        public int SliderX => this.itemX;
        public int SliderY => this.itemY + 16;
        public int SliderLength => this.sliderLength;
        public int SliderPosition => this.sliderPosition;
    }
}
