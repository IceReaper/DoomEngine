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
	using Math;

	public static class Box
    {
        public const int Top = 0;
        public const int Bottom = 1;
        public const int Left = 2;
        public const int Right = 3;

        public static void Clear(Fixed[] box)
        {
            box[Box.Top] = box[Box.Right] = Fixed.MinValue;
            box[Box.Bottom] = box[Box.Left] = Fixed.MaxValue;
        }

        public static void AddPoint(Fixed[] box, Fixed x, Fixed y)
        {
            if (x < box[Box.Left])
            {
                box[Box.Left] = x;
            }
            else if (x > box[Box.Right])
            {
                box[Box.Right] = x;
            }

            if (y < box[Box.Bottom])
            {
                box[Box.Bottom] = y;
            }
            else if (y > box[Box.Top])
            {
                box[Box.Top] = y;
            }
        }
    }
}
