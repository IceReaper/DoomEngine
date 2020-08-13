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

namespace DoomEngine.SoftwareRendering
{
	using Doom.Common;
	using Doom.Game;
	using System;

	public sealed class WipeEffect
    {
        private short[] y;
        private int height;
        private DoomRandom random;

        public WipeEffect(int width, int height)
        {
            this.y = new short[width];
            this.height = height;
            this.random = new DoomRandom(DateTime.Now.Millisecond);
        }

        public void Start()
        {
            this.y[0] = (short)(-(this.random.Next() % 16));
            for (var i = 1; i < this.y.Length; i++)
            {
                var r = (this.random.Next() % 3) - 1;
                this.y[i] = (short)(this.y[i - 1] + r);
                if (this.y[i] > 0)
                {
                    this.y[i] = 0;
                }
                else if (this.y[i] == -16)
                {
                    this.y[i] = -15;
                }
            }
        }

        public UpdateResult Update()
        {
            var done = true;

            for (var i = 0; i < this.y.Length; i++)
            {
                if (this.y[i] < 0)
                {
                    this.y[i]++;
                    done = false;
                }
                else if (this.y[i] < this.height)
                {
                    var dy = (this.y[i] < 16) ? this.y[i] + 1 : 8;
                    if (this.y[i] + dy >= this.height)
                    {
                        dy = this.height - this.y[i];
                    }
                    this.y[i] += (short)dy;
                    done = false;
                }
            }

            if (done)
            {
                return UpdateResult.Completed;
            }
            else
            {
                return UpdateResult.None;
            }
        }

        public short[] Y => this.y;
    }
}
