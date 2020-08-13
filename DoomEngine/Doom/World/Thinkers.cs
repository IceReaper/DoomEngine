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
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public sealed class Thinkers
    {
        private World world;

        public Thinkers(World world)
        {
            this.world = world;

            this.InitThinkers();
        }


        private Thinker cap;

        private void InitThinkers()
        {
            this.cap = new Thinker();
            this.cap.Prev = this.cap.Next = this.cap;
        }

        public void Add(Thinker thinker)
        {
            this.cap.Prev.Next = thinker;
            thinker.Next = this.cap;
            thinker.Prev = this.cap.Prev;
            this.cap.Prev = thinker;
        }

        public void Remove(Thinker thinker)
        {
            thinker.ThinkerState = ThinkerState.Removed;
        }

        public void Run()
        {
            var current = this.cap.Next;
            while (current != this.cap)
            {
                if (current.ThinkerState == ThinkerState.Removed)
                {
                    // Time to remove it.
                    current.Next.Prev = current.Prev;
                    current.Prev.Next = current.Next;
                }
                else
                {
                    if (current.ThinkerState == ThinkerState.Active)
                    {
                        current.Run();
                    }
                }
                current = current.Next;
            }
        }

        public void Reset()
        {
            this.cap.Prev = this.cap.Next = this.cap;
        }

        public ThinkerEnumerator GetEnumerator()
        {
            return new ThinkerEnumerator(this);
        }



        public struct ThinkerEnumerator : IEnumerator<Thinker>
        {
            private Thinkers thinkers;
            private Thinker current;

            public ThinkerEnumerator(Thinkers thinkers)
            {
                this.thinkers = thinkers;
                this.current = thinkers.cap;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    this.current = this.current.Next;
                    if (this.current == this.thinkers.cap)
                    {
                        return false;
                    }
                    else if (this.current.ThinkerState != ThinkerState.Removed)
                    {
                        return true;
                    }
                }
            }

            public void Reset()
            {
                this.current = this.thinkers.cap;
            }

            public void Dispose()
            {
            }

            public Thinker Current => this.current;

            object IEnumerator.Current => throw new NotImplementedException();
        }
    }
}
