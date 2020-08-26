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
	using Event;
	using Info;
	using Map;
	using Math;
	using System.Collections.Generic;
	using UserInput;

	public sealed class AutoMap
	{
		private World world;

		private Fixed minX;
		private Fixed maxX;
		private Fixed minY;
		private Fixed maxY;

		private Fixed viewX;
		private Fixed viewY;

		private bool visible;
		private AutoMapState state;

		private Fixed zoom;
		private bool follow;

		private bool zoomIn;
		private bool zoomOut;

		private bool left;
		private bool right;
		private bool up;
		private bool down;

		private List<Vertex> marks;
		private int nextMarkNumber;

		public AutoMap(World world)
		{
			this.world = world;

			this.minX = Fixed.MaxValue;
			this.maxX = Fixed.MinValue;
			this.minY = Fixed.MaxValue;
			this.maxY = Fixed.MinValue;

			foreach (var vertex in world.Map.Vertices)
			{
				if (vertex.X < this.minX)
				{
					this.minX = vertex.X;
				}

				if (vertex.X > this.maxX)
				{
					this.maxX = vertex.X;
				}

				if (vertex.Y < this.minY)
				{
					this.minY = vertex.Y;
				}

				if (vertex.Y > this.maxY)
				{
					this.maxY = vertex.Y;
				}
			}

			this.viewX = this.minX + (this.maxX - this.minX) / 2;
			this.viewY = this.minY + (this.maxY - this.minY) / 2;

			this.visible = false;
			this.state = AutoMapState.None;

			this.zoom = Fixed.One;
			this.follow = true;

			this.zoomIn = false;
			this.zoomOut = false;
			this.left = false;
			this.right = false;
			this.up = false;
			this.down = false;

			this.marks = new List<Vertex>();
			this.nextMarkNumber = 0;
		}

		public void Update()
		{
			if (this.zoomIn)
			{
				this.zoom += this.zoom / 16;
			}

			if (this.zoomOut)
			{
				this.zoom -= this.zoom / 16;
			}

			if (this.zoom < Fixed.One / 2)
			{
				this.zoom = Fixed.One / 2;
			}
			else if (this.zoom > Fixed.One * 32)
			{
				this.zoom = Fixed.One * 32;
			}

			if (this.left)
			{
				this.viewX -= 64 / this.zoom;
			}

			if (this.right)
			{
				this.viewX += 64 / this.zoom;
			}

			if (this.up)
			{
				this.viewY += 64 / this.zoom;
			}

			if (this.down)
			{
				this.viewY -= 64 / this.zoom;
			}

			if (this.viewX < this.minX)
			{
				this.viewX = this.minX;
			}
			else if (this.viewX > this.maxX)
			{
				this.viewX = this.maxX;
			}

			if (this.viewY < this.minY)
			{
				this.viewY = this.minY;
			}
			else if (this.viewY > this.maxY)
			{
				this.viewY = this.maxY;
			}

			if (this.follow)
			{
				var player = this.world.Options.Player.Mobj;
				this.viewX = player.X;
				this.viewY = player.Y;
			}
		}

		public bool DoEvent(DoomEvent e)
		{
			if (e.Key == DoomKey.Add || e.Key == DoomKey.Quote)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.zoomIn = true;
				}
				else if (e.Type == EventType.KeyUp)
				{
					this.zoomIn = false;
				}

				return true;
			}
			else if (e.Key == DoomKey.Subtract || e.Key == DoomKey.Hyphen)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.zoomOut = true;
				}
				else if (e.Type == EventType.KeyUp)
				{
					this.zoomOut = false;
				}

				return true;
			}
			else if (e.Key == DoomKey.Left)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.left = true;
				}
				else if (e.Type == EventType.KeyUp)
				{
					this.left = false;
				}

				return true;
			}
			else if (e.Key == DoomKey.Right)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.right = true;
				}
				else if (e.Type == EventType.KeyUp)
				{
					this.right = false;
				}

				return true;
			}
			else if (e.Key == DoomKey.Up)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.up = true;
				}
				else if (e.Type == EventType.KeyUp)
				{
					this.up = false;
				}

				return true;
			}
			else if (e.Key == DoomKey.Down)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.down = true;
				}
				else if (e.Type == EventType.KeyUp)
				{
					this.down = false;
				}

				return true;
			}
			else if (e.Key == DoomKey.F)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.follow = !this.follow;

					if (this.follow)
					{
						this.world.Options.Player.SendMessage(DoomInfo.Strings.AMSTR_FOLLOWON);
					}
					else
					{
						this.world.Options.Player.SendMessage(DoomInfo.Strings.AMSTR_FOLLOWOFF);
					}

					return true;
				}
			}
			else if (e.Key == DoomKey.M)
			{
				if (e.Type == EventType.KeyDown)
				{
					if (this.marks.Count < 10)
					{
						this.marks.Add(new Vertex(this.viewX, this.viewY));
					}
					else
					{
						this.marks[this.nextMarkNumber] = new Vertex(this.viewX, this.viewY);
					}

					this.nextMarkNumber++;

					if (this.nextMarkNumber == 10)
					{
						this.nextMarkNumber = 0;
					}

					this.world.Options.Player.SendMessage(DoomInfo.Strings.AMSTR_MARKEDSPOT);

					return true;
				}
			}
			else if (e.Key == DoomKey.C)
			{
				if (e.Type == EventType.KeyDown)
				{
					this.marks.Clear();
					this.nextMarkNumber = 0;
					this.world.Options.Player.SendMessage(DoomInfo.Strings.AMSTR_MARKSCLEARED);

					return true;
				}
			}

			return false;
		}

		public void Open()
		{
			this.visible = true;
		}

		public void Close()
		{
			this.visible = false;
			this.zoomIn = false;
			this.zoomOut = false;
			this.left = false;
			this.right = false;
			this.up = false;
			this.down = false;
		}

		public void ToggleCheat()
		{
			this.state++;

			if ((int) this.state == 3)
			{
				this.state = AutoMapState.None;
			}
		}

		public Fixed MinX => this.minX;
		public Fixed MaxX => this.maxX;
		public Fixed MinY => this.minY;
		public Fixed MaxY => this.maxY;
		public Fixed ViewX => this.viewX;
		public Fixed ViewY => this.viewY;
		public Fixed Zoom => this.zoom;
		public bool Follow => this.follow;
		public bool Visible => this.visible;
		public AutoMapState State => this.state;
		public IReadOnlyList<Vertex> Marks => this.marks;
	}
}
