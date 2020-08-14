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
	using Doom.Game;
	using Doom.Graphics;
	using Doom.Info;
	using Doom.Map;
	using Doom.Math;
	using Doom.World;
	using System;

	public sealed class AutoMapRenderer
	{
		private static readonly float pr = 8 * DoomInfo.MobjInfos[(int) MobjType.Player].Radius.ToFloat() / 7;

		// The vector graphics for the automap.
		// A line drawing of the player pointing right, starting from the middle.
		private static readonly float[] playerArrow = new float[]
		{
			-AutoMapRenderer.pr + AutoMapRenderer.pr / 8, 0, AutoMapRenderer.pr, 0, // -----
			AutoMapRenderer.pr, 0, AutoMapRenderer.pr - AutoMapRenderer.pr / 2, AutoMapRenderer.pr / 4, // ----->
			AutoMapRenderer.pr, 0, AutoMapRenderer.pr - AutoMapRenderer.pr / 2, -AutoMapRenderer.pr / 4, -AutoMapRenderer.pr + AutoMapRenderer.pr / 8, 0,
			-AutoMapRenderer.pr - AutoMapRenderer.pr / 8, AutoMapRenderer.pr / 4, // >---->
			-AutoMapRenderer.pr + AutoMapRenderer.pr / 8, 0, -AutoMapRenderer.pr - AutoMapRenderer.pr / 8, -AutoMapRenderer.pr / 4,
			-AutoMapRenderer.pr + 3 * AutoMapRenderer.pr / 8, 0, -AutoMapRenderer.pr + AutoMapRenderer.pr / 8, AutoMapRenderer.pr / 4, // >>--->
			-AutoMapRenderer.pr + 3 * AutoMapRenderer.pr / 8, 0, -AutoMapRenderer.pr + AutoMapRenderer.pr / 8, -AutoMapRenderer.pr / 4
		};

		private static readonly float tr = 16;

		private static readonly float[] thingTriangle = new float[]
		{
			-0.5F * AutoMapRenderer.tr, -0.7F * AutoMapRenderer.tr, AutoMapRenderer.tr, 0F, AutoMapRenderer.tr, 0F, -0.5F * AutoMapRenderer.tr,
			0.7F * AutoMapRenderer.tr, -0.5F * AutoMapRenderer.tr, 0.7F * AutoMapRenderer.tr, -0.5F * AutoMapRenderer.tr, -0.7F * AutoMapRenderer.tr
		};

		// For use if I do walls with outsides / insides.
		private static readonly int reds = (256 - 5 * 16);
		private static readonly int redRange = 16;
		private static readonly int greens = (7 * 16);
		private static readonly int greenRange = 16;
		private static readonly int grays = (6 * 16);
		private static readonly int grayRange = 16;
		private static readonly int browns = (4 * 16);
		private static readonly int brownRange = 16;
		private static readonly int yellows = (256 - 32 + 7);
		private static readonly int yellowRange = 1;
		private static readonly int black = 0;
		private static readonly int white = (256 - 47);

		// Automap colors.
		private static readonly int background = AutoMapRenderer.black;
		private static readonly int wallColors = AutoMapRenderer.reds;
		private static readonly int wallRange = AutoMapRenderer.redRange;
		private static readonly int tsWallColors = AutoMapRenderer.grays;
		private static readonly int tsWallRange = AutoMapRenderer.grayRange;
		private static readonly int fdWallColors = AutoMapRenderer.browns;
		private static readonly int fdWallRange = AutoMapRenderer.brownRange;
		private static readonly int cdWallColors = AutoMapRenderer.yellows;
		private static readonly int cdWallRange = AutoMapRenderer.yellowRange;
		private static readonly int thingColors = AutoMapRenderer.greens;
		private static readonly int thingRange = AutoMapRenderer.greenRange;
		private static readonly int secretWallColors = AutoMapRenderer.wallColors;
		private static readonly int secretWallRange = AutoMapRenderer.wallRange;

		private static readonly int[] playerColors = new int[] {AutoMapRenderer.greens, AutoMapRenderer.grays, AutoMapRenderer.browns, AutoMapRenderer.reds};

		private DrawScreen screen;

		private int scale;
		private int amWidth;
		private int amHeight;
		private float ppu;

		private float minX;
		private float maxX;
		private float width;
		private float minY;
		private float maxY;
		private float height;

		private float viewX;
		private float viewY;
		private float zoom;

		private Patch[] markNumbers;

		public AutoMapRenderer(DrawScreen screen)
		{
			this.screen = screen;

			this.scale = screen.Width / 320;
			this.amWidth = screen.Width;
			this.amHeight = screen.Height - this.scale * StatusBarRenderer.Height;
			this.ppu = (float) this.scale / 16;

			this.markNumbers = new Patch[10];

			for (var i = 0; i < this.markNumbers.Length; i++)
			{
				this.markNumbers[i] = Patch.FromWad("AMMNUM" + i);
			}
		}

		public void Render(Player player)
		{
			this.screen.FillRect(0, 0, this.amWidth, this.amHeight, AutoMapRenderer.background);

			var world = player.Mobj.World;
			var am = world.AutoMap;

			this.minX = am.MinX.ToFloat();
			this.maxX = am.MaxX.ToFloat();
			this.width = this.maxX - this.minX;
			this.minY = am.MinY.ToFloat();
			this.maxY = am.MaxY.ToFloat();
			this.height = this.maxY - this.minY;

			this.viewX = am.ViewX.ToFloat();
			this.viewY = am.ViewY.ToFloat();
			this.zoom = am.Zoom.ToFloat();

			foreach (var line in world.Map.Lines)
			{
				var v1 = this.ToScreenPos(line.Vertex1);
				var v2 = this.ToScreenPos(line.Vertex2);

				var cheating = am.State != AutoMapState.None;

				if (cheating || (line.Flags & LineFlags.Mapped) != 0)
				{
					if ((line.Flags & LineFlags.DontDraw) != 0 && !cheating)
					{
						continue;
					}

					if (line.BackSector == null)
					{
						this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.wallColors);
					}
					else
					{
						if (line.Special == (LineSpecial) 39)
						{
							// Teleporters.
							this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.wallColors + AutoMapRenderer.wallRange / 2);
						}
						else if ((line.Flags & LineFlags.Secret) != 0)
						{
							// Secret door.
							if (cheating)
							{
								this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.secretWallColors);
							}
							else
							{
								this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.wallColors);
							}
						}
						else if (line.BackSector.FloorHeight != line.FrontSector.FloorHeight)
						{
							// Floor level change.
							this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.fdWallColors);
						}
						else if (line.BackSector.CeilingHeight != line.FrontSector.CeilingHeight)
						{
							// Ceiling level change.
							this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.cdWallColors);
						}
						else if (cheating)
						{
							this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.tsWallColors);
						}
					}
				}
				else if (player.Powers[(int) PowerType.AllMap] > 0)
				{
					if ((line.Flags & LineFlags.DontDraw) == 0)
					{
						this.screen.DrawLine(v1.X, v1.Y, v2.X, v2.Y, AutoMapRenderer.grays + 3);
					}
				}
			}

			for (var i = 0; i < am.Marks.Count; i++)
			{
				var pos = this.ToScreenPos(am.Marks[i]);
				this.screen.DrawPatch(this.markNumbers[i], (int) MathF.Round(pos.X), (int) MathF.Round(pos.Y), this.scale);
			}

			if (am.State == AutoMapState.AllThings)
			{
				this.DrawThings(world);
			}

			this.DrawPlayers(world);

			if (!am.Follow)
			{
				this.screen.DrawLine(
					this.amWidth / 2 - 2 * this.scale,
					this.amHeight / 2,
					this.amWidth / 2 + 2 * this.scale,
					this.amHeight / 2,
					AutoMapRenderer.grays
				);

				this.screen.DrawLine(
					this.amWidth / 2,
					this.amHeight / 2 - 2 * this.scale,
					this.amWidth / 2,
					this.amHeight / 2 + 2 * this.scale,
					AutoMapRenderer.grays
				);
			}

			this.screen.DrawText(world.Map.Title, 0, this.amHeight - this.scale, this.scale);
		}

		private void DrawPlayers(World world)
		{
			var options = world.Options;
			var players = options.Players;
			var consolePlayer = world.ConsolePlayer;
			var am = world.AutoMap;

			if (!options.NetGame)
			{
				this.DrawCharacter(consolePlayer.Mobj, AutoMapRenderer.playerArrow, AutoMapRenderer.white);

				return;
			}

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				var player = players[i];

				if (options.Deathmatch != 0 && !options.DemoPlayback && player != consolePlayer)
				{
					continue;
				}

				if (!player.InGame)
				{
					continue;
				}

				int color;

				if (player.Powers[(int) PowerType.Invisibility] > 0)
				{
					// Close to black.
					color = 246;
				}
				else
				{
					color = AutoMapRenderer.playerColors[i];
				}

				this.DrawCharacter(player.Mobj, AutoMapRenderer.playerArrow, color);
			}
		}

		private void DrawThings(World world)
		{
			foreach (var thinker in world.Thinkers)
			{
				var mobj = thinker as Mobj;

				if (mobj != null)
				{
					this.DrawCharacter(mobj, AutoMapRenderer.thingTriangle, AutoMapRenderer.greens);
				}
			}
		}

		private void DrawCharacter(Mobj mobj, float[] data, int color)
		{
			var pos = this.ToScreenPos(mobj.X, mobj.Y);
			var sin = (float) Math.Sin(mobj.Angle.ToRadian());
			var cos = (float) Math.Cos(mobj.Angle.ToRadian());

			for (var i = 0; i < data.Length; i += 4)
			{
				var x1 = pos.X + this.zoom * this.ppu * (cos * data[i + 0] - sin * data[i + 1]);
				var y1 = pos.Y - this.zoom * this.ppu * (sin * data[i + 0] + cos * data[i + 1]);
				var x2 = pos.X + this.zoom * this.ppu * (cos * data[i + 2] - sin * data[i + 3]);
				var y2 = pos.Y - this.zoom * this.ppu * (sin * data[i + 2] + cos * data[i + 3]);
				this.screen.DrawLine(x1, y1, x2, y2, color);
			}
		}

		private DrawPos ToScreenPos(Fixed x, Fixed y)
		{
			var posX = this.zoom * this.ppu * (x.ToFloat() - this.viewX) + this.amWidth / 2;
			var posY = -this.zoom * this.ppu * (y.ToFloat() - this.viewY) + this.amHeight / 2;

			return new DrawPos(posX, posY);
		}

		private DrawPos ToScreenPos(Vertex v)
		{
			var posX = this.zoom * this.ppu * (v.X.ToFloat() - this.viewX) + this.amWidth / 2;
			var posY = -this.zoom * this.ppu * (v.Y.ToFloat() - this.viewY) + this.amHeight / 2;

			return new DrawPos(posX, posY);
		}

		private struct DrawPos
		{
			public float X;
			public float Y;

			public DrawPos(float x, float y)
			{
				this.X = x;
				this.Y = y;
			}
		}
	}
}
