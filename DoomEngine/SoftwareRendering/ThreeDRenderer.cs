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
	using Doom.Graphics;
	using Doom.Map;
	using Doom.Math;
	using Doom.Wad;
	using Doom.World;
	using System;
	using System.Linq;

	public sealed class ThreeDRenderer
	{
		public static readonly int MaxScreenSize = 9;

		private ColorMap colorMap;
		private TextureLookup textures;
		private FlatLookup flats;
		private SpriteLookup sprites;

		private DrawScreen screen;
		private int screenWidth;
		private int screenHeight;
		private byte[] screenData;
		private int drawScale;

		private int windowSize;

		public ThreeDRenderer(CommonResource resource, DrawScreen screen, int windowSize)
		{
			this.colorMap = resource.ColorMap;
			this.textures = resource.Textures;
			this.flats = resource.Flats;
			this.sprites = resource.Sprites;

			this.screen = screen;
			this.screenWidth = screen.Width;
			this.screenHeight = screen.Height;
			this.screenData = screen.Data;
			this.drawScale = this.screenWidth / 320;

			this.windowSize = windowSize;

			this.InitWallRendering();
			this.InitPlaneRendering();
			this.InitSkyRendering();
			this.InitLighting();
			this.InitRenderingHistory();
			this.InitSpriteRendering();
			this.InitWeaponRendering();
			this.InitFuzzEffect();
			this.InitColorTranslation();
			this.InitWindowBorder(resource.Wad);

			this.SetWindowSize(windowSize);
		}

		private void SetWindowSize(int size)
		{
			var scale = this.screenWidth / 320;

			if (size < 7)
			{
				var width = scale * (96 + 32 * size);
				var height = scale * (48 + 16 * size);
				var x = (this.screenWidth - width) / 2;
				var y = (this.screenHeight - StatusBarRenderer.Height * scale - height) / 2;
				this.ResetWindow(x, y, width, height);
			}
			else if (size == 7)
			{
				var width = this.screenWidth;
				var height = this.screenHeight - StatusBarRenderer.Height * scale;
				this.ResetWindow(0, 0, width, height);
			}
			else
			{
				var width = this.screenWidth;
				var height = this.screenHeight;
				this.ResetWindow(0, 0, width, height);
			}

			this.ResetWallRendering();
			this.ResetPlaneRendering();
			this.ResetSkyRendering();
			this.ResetLighting();
			this.ResetRenderingHistory();
			this.ResetWeaponRendering();
		}

		////////////////////////////////////////////////////////////
		// Window settings
		////////////////////////////////////////////////////////////

		private int windowX;
		private int windowY;
		private int windowWidth;
		private int windowHeight;
		private int centerX;
		private int centerY;
		private Fixed centerXFrac;
		private Fixed centerYFrac;
		private Fixed projection;

		private void ResetWindow(int x, int y, int width, int height)
		{
			this.windowX = x;
			this.windowY = y;
			this.windowWidth = width;
			this.windowHeight = height;
			this.centerX = this.windowWidth / 2;
			this.centerY = this.windowHeight / 2;
			this.centerXFrac = Fixed.FromInt(this.centerX);
			this.centerYFrac = Fixed.FromInt(this.centerY);
			this.projection = this.centerXFrac;
		}

		////////////////////////////////////////////////////////////
		// Wall rendering
		////////////////////////////////////////////////////////////

		private const int FineFov = 2048;

		private int[] angleToX;
		private Angle[] xToAngle;
		private Angle clipAngle;
		private Angle clipAngle2;

		private void InitWallRendering()
		{
			this.angleToX = new int[Trig.FineAngleCount / 2];
			this.xToAngle = new Angle[this.screenWidth];
		}

		private void ResetWallRendering()
		{
			var focalLength = this.centerXFrac / Trig.Tan(Trig.FineAngleCount / 4 + ThreeDRenderer.FineFov / 2);

			for (var i = 0; i < Trig.FineAngleCount / 2; i++)
			{
				int t;

				if (Trig.Tan(i) > Fixed.FromInt(2))
				{
					t = -1;
				}
				else if (Trig.Tan(i) < Fixed.FromInt(-2))
				{
					t = this.windowWidth + 1;
				}
				else
				{
					t = (this.centerXFrac - Trig.Tan(i) * focalLength).ToIntCeiling();

					if (t < -1)
					{
						t = -1;
					}
					else if (t > this.windowWidth + 1)
					{
						t = this.windowWidth + 1;
					}
				}

				this.angleToX[i] = t;
			}

			for (var x = 0; x < this.windowWidth; x++)
			{
				var i = 0;

				while (this.angleToX[i] > x)
				{
					i++;
				}

				this.xToAngle[x] = new Angle((uint) (i << Trig.AngleToFineShift)) - Angle.Ang90;
			}

			for (var i = 0; i < Trig.FineAngleCount / 2; i++)
			{
				if (this.angleToX[i] == -1)
				{
					this.angleToX[i] = 0;
				}
				else if (this.angleToX[i] == this.windowWidth + 1)
				{
					this.angleToX[i] = this.windowWidth;
				}
			}

			this.clipAngle = this.xToAngle[0];
			this.clipAngle2 = new Angle(2 * this.clipAngle.Data);
		}

		////////////////////////////////////////////////////////////
		// Plane rendering
		////////////////////////////////////////////////////////////

		private Fixed[] planeYSlope;
		private Fixed[] planeDistScale;
		private Fixed planeBaseXScale;
		private Fixed planeBaseYScale;

		private Sector ceilingPrevSector;
		private int ceilingPrevX;
		private int ceilingPrevY1;
		private int ceilingPrevY2;
		private Fixed[] ceilingXFrac;
		private Fixed[] ceilingYFrac;
		private Fixed[] ceilingXStep;
		private Fixed[] ceilingYStep;
		private byte[][] ceilingLights;

		private Sector floorPrevSector;
		private int floorPrevX;
		private int floorPrevY1;
		private int floorPrevY2;
		private Fixed[] floorXFrac;
		private Fixed[] floorYFrac;
		private Fixed[] floorXStep;
		private Fixed[] floorYStep;
		private byte[][] floorLights;

		private void InitPlaneRendering()
		{
			this.planeYSlope = new Fixed[this.screenHeight];
			this.planeDistScale = new Fixed[this.screenWidth];
			this.ceilingXFrac = new Fixed[this.screenHeight];
			this.ceilingYFrac = new Fixed[this.screenHeight];
			this.ceilingXStep = new Fixed[this.screenHeight];
			this.ceilingYStep = new Fixed[this.screenHeight];
			this.ceilingLights = new byte[this.screenHeight][];
			this.floorXFrac = new Fixed[this.screenHeight];
			this.floorYFrac = new Fixed[this.screenHeight];
			this.floorXStep = new Fixed[this.screenHeight];
			this.floorYStep = new Fixed[this.screenHeight];
			this.floorLights = new byte[this.screenHeight][];
		}

		private void ResetPlaneRendering()
		{
			for (int i = 0; i < this.windowHeight; i++)
			{
				var dy = Fixed.FromInt(i - this.windowHeight / 2) + Fixed.One / 2;
				dy = Fixed.Abs(dy);
				this.planeYSlope[i] = Fixed.FromInt(this.windowWidth / 2) / dy;
			}

			for (var i = 0; i < this.windowWidth; i++)
			{
				var cos = Fixed.Abs(Trig.Cos(this.xToAngle[i]));
				this.planeDistScale[i] = Fixed.One / cos;
			}
		}

		private void ClearPlaneRendering()
		{
			var angle = this.viewAngle - Angle.Ang90;
			this.planeBaseXScale = Trig.Cos(angle) / this.centerXFrac;
			this.planeBaseYScale = -(Trig.Sin(angle) / this.centerXFrac);

			this.ceilingPrevSector = null;
			this.ceilingPrevX = int.MaxValue;

			this.floorPrevSector = null;
			this.floorPrevX = int.MaxValue;
		}

		////////////////////////////////////////////////////////////
		// Sky rendering
		////////////////////////////////////////////////////////////

		private const int angleToSkyShift = 22;
		private Fixed skyTextureAlt;
		private Fixed skyInvScale;

		private void InitSkyRendering()
		{
			this.skyTextureAlt = Fixed.FromInt(100);
		}

		private void ResetSkyRendering()
		{
			// The code below is based on PrBoom+' sky rendering implementation.
			var num = (long) Fixed.FracUnit * this.screenWidth * 200;
			var den = this.windowWidth * this.screenHeight;
			this.skyInvScale = new Fixed((int) (num / den));
		}

		////////////////////////////////////////////////////////////
		// Lighting
		////////////////////////////////////////////////////////////

		private const int lightLevelCount = 16;
		private const int lightSegShift = 4;
		private const int scaleLightShift = 12;
		private const int zLightShift = 20;
		private const int colorMapCount = 32;

		private int maxScaleLight;
		private const int maxZLight = 128;

		private byte[][][] diminishingScaleLight;
		private byte[][][] diminishingZLight;
		private byte[][][] fixedLight;

		private byte[][][] scaleLight;
		private byte[][][] zLight;

		private int extraLight;
		private int fixedColorMap;

		private void InitLighting()
		{
			this.maxScaleLight = 48 * (this.screenWidth / 320);

			this.diminishingScaleLight = new byte[ThreeDRenderer.lightLevelCount][][];
			this.diminishingZLight = new byte[ThreeDRenderer.lightLevelCount][][];
			this.fixedLight = new byte[ThreeDRenderer.lightLevelCount][][];

			for (var i = 0; i < ThreeDRenderer.lightLevelCount; i++)
			{
				this.diminishingScaleLight[i] = new byte[this.maxScaleLight][];
				this.diminishingZLight[i] = new byte[ThreeDRenderer.maxZLight][];
				this.fixedLight[i] = new byte[Math.Max(this.maxScaleLight, ThreeDRenderer.maxZLight)][];
			}

			var distMap = 2;

			// Calculate the light levels to use for each level / distance combination.
			for (var i = 0; i < ThreeDRenderer.lightLevelCount; i++)
			{
				var start = ((ThreeDRenderer.lightLevelCount - 1 - i) * 2) * ThreeDRenderer.colorMapCount / ThreeDRenderer.lightLevelCount;

				for (var j = 0; j < ThreeDRenderer.maxZLight; j++)
				{
					var scale = Fixed.FromInt(320 / 2) / new Fixed((j + 1) << ThreeDRenderer.zLightShift);
					scale = new Fixed(scale.Data >> ThreeDRenderer.scaleLightShift);

					var level = start - scale.Data / distMap;

					if (level < 0)
					{
						level = 0;
					}

					if (level >= ThreeDRenderer.colorMapCount)
					{
						level = ThreeDRenderer.colorMapCount - 1;
					}

					this.diminishingZLight[i][j] = this.colorMap[level];
				}
			}
		}

		private void ResetLighting()
		{
			var distMap = 2;

			// Calculate the light levels to use for each level / scale combination.
			for (var i = 0; i < ThreeDRenderer.lightLevelCount; i++)
			{
				var start = ((ThreeDRenderer.lightLevelCount - 1 - i) * 2) * ThreeDRenderer.colorMapCount / ThreeDRenderer.lightLevelCount;

				for (var j = 0; j < this.maxScaleLight; j++)
				{
					var level = start - j * 320 / this.windowWidth / distMap;

					if (level < 0)
					{
						level = 0;
					}

					if (level >= ThreeDRenderer.colorMapCount)
					{
						level = ThreeDRenderer.colorMapCount - 1;
					}

					this.diminishingScaleLight[i][j] = this.colorMap[level];
				}
			}
		}

		private void ClearLighting()
		{
			if (this.fixedColorMap == 0)
			{
				this.scaleLight = this.diminishingScaleLight;
				this.zLight = this.diminishingZLight;
				this.fixedLight[0][0] = null;
			}
			else if (this.fixedLight[0][0] != this.colorMap[this.fixedColorMap])
			{
				for (var i = 0; i < ThreeDRenderer.lightLevelCount; i++)
				{
					for (var j = 0; j < this.fixedLight[i].Length; j++)
					{
						this.fixedLight[i][j] = this.colorMap[this.fixedColorMap];
					}
				}

				this.scaleLight = this.fixedLight;
				this.zLight = this.fixedLight;
			}
		}

		////////////////////////////////////////////////////////////
		// Rendering history
		////////////////////////////////////////////////////////////

		private short[] upperClip;
		private short[] lowerClip;

		private int negOneArray;
		private int windowHeightArray;

		private int clipRangeCount;
		private ClipRange[] clipRanges;

		private int clipDataLength;
		private short[] clipData;

		private int visWallRangeCount;
		private VisWallRange[] visWallRanges;

		private void InitRenderingHistory()
		{
			this.upperClip = new short[this.screenWidth];
			this.lowerClip = new short[this.screenWidth];

			this.clipRanges = new ClipRange[256];

			for (var i = 0; i < this.clipRanges.Length; i++)
			{
				this.clipRanges[i] = new ClipRange();
			}

			this.clipData = new short[128 * this.screenWidth];

			this.visWallRanges = new VisWallRange[512];

			for (var i = 0; i < this.visWallRanges.Length; i++)
			{
				this.visWallRanges[i] = new VisWallRange();
			}
		}

		private void ResetRenderingHistory()
		{
			for (var i = 0; i < this.windowWidth; i++)
			{
				this.clipData[i] = -1;
			}

			this.negOneArray = 0;

			for (var i = this.windowWidth; i < 2 * this.windowWidth; i++)
			{
				this.clipData[i] = (short) this.windowHeight;
			}

			this.windowHeightArray = this.windowWidth;
		}

		private void ClearRenderingHistory()
		{
			for (var x = 0; x < this.windowWidth; x++)
			{
				this.upperClip[x] = -1;
			}

			for (var x = 0; x < this.windowWidth; x++)
			{
				this.lowerClip[x] = (short) this.windowHeight;
			}

			this.clipRanges[0].First = -0x7fffffff;
			this.clipRanges[0].Last = -1;
			this.clipRanges[1].First = this.windowWidth;
			this.clipRanges[1].Last = 0x7fffffff;
			this.clipRangeCount = 2;

			this.clipDataLength = 2 * this.windowWidth;

			this.visWallRangeCount = 0;
		}

		////////////////////////////////////////////////////////////
		// Sprite rendering
		////////////////////////////////////////////////////////////

		private static readonly Fixed minZ = Fixed.FromInt(4);

		private int visSpriteCount;
		private VisSprite[] visSprites;

		private void InitSpriteRendering()
		{
			this.visSprites = new VisSprite[256];

			for (var i = 0; i < this.visSprites.Length; i++)
			{
				this.visSprites[i] = new VisSprite();
			}
		}

		private void ClearSpriteRendering()
		{
			this.visSpriteCount = 0;
		}

		////////////////////////////////////////////////////////////
		// Weapon rendering
		////////////////////////////////////////////////////////////

		private VisSprite weaponSprite;
		private Fixed weaponScale;
		private Fixed weaponInvScale;

		private void InitWeaponRendering()
		{
			this.weaponSprite = new VisSprite();
		}

		private void ResetWeaponRendering()
		{
			this.weaponScale = new Fixed(Fixed.FracUnit * this.windowWidth / 320);
			this.weaponInvScale = new Fixed(Fixed.FracUnit * 320 / this.windowWidth);
		}

		////////////////////////////////////////////////////////////
		// Fuzz effect
		////////////////////////////////////////////////////////////

		private static sbyte[] fuzzTable = new sbyte[]
		{
			1, -1, 1, -1, 1, 1, -1, 1, 1, -1, 1, 1, 1, -1, 1, 1, 1, -1, -1, -1, -1, 1, -1, -1, 1, 1, 1, 1, -1, 1, -1, 1, 1, -1, -1, 1, 1, -1, -1, -1, -1, 1,
			1, 1, 1, -1, 1, 1, -1, 1
		};

		private int fuzzPos;

		private void InitFuzzEffect()
		{
			this.fuzzPos = 0;
		}

		////////////////////////////////////////////////////////////
		// Color translation
		////////////////////////////////////////////////////////////

		private byte[] greenToGray;
		private byte[] greenToBrown;
		private byte[] greenToRed;

		private void InitColorTranslation()
		{
			this.greenToGray = new byte[256];
			this.greenToBrown = new byte[256];
			this.greenToRed = new byte[256];

			for (var i = 0; i < 256; i++)
			{
				this.greenToGray[i] = (byte) i;
				this.greenToBrown[i] = (byte) i;
				this.greenToRed[i] = (byte) i;
			}

			for (var i = 112; i < 128; i++)
			{
				this.greenToGray[i] -= 16;
				this.greenToBrown[i] -= 48;
				this.greenToRed[i] -= 80;
			}
		}

		////////////////////////////////////////////////////////////
		// Window border
		////////////////////////////////////////////////////////////

		private Patch borderTopLeft;
		private Patch borderTopRight;
		private Patch borderBottomLeft;
		private Patch borderBottomRight;
		private Patch borderTop;
		private Patch borderBottom;
		private Patch borderLeft;
		private Patch borderRight;
		private Flat backFlat;

		private void InitWindowBorder(Wad wad)
		{
			this.borderTopLeft = Patch.FromWad(wad, "BRDR_TL");
			this.borderTopRight = Patch.FromWad(wad, "BRDR_TR");
			this.borderBottomLeft = Patch.FromWad(wad, "BRDR_BL");
			this.borderBottomRight = Patch.FromWad(wad, "BRDR_BR");
			this.borderTop = Patch.FromWad(wad, "BRDR_T");
			this.borderBottom = Patch.FromWad(wad, "BRDR_B");
			this.borderLeft = Patch.FromWad(wad, "BRDR_L");
			this.borderRight = Patch.FromWad(wad, "BRDR_R");

			if (DoomApplication.Instance.Resource.Wad.Names.Contains("doom2") || DoomApplication.Instance.Resource.Wad.Names.Contains("plutonia") || DoomApplication.Instance.Resource.Wad.Names.Contains("tnt"))
			{
				this.backFlat = this.flats["GRNROCK"];
			}
			else
			{
				this.backFlat = this.flats["FLOOR7_2"];
			}
		}

		private void FillBackScreen()
		{
			var fillHeight = this.screenHeight - this.drawScale * StatusBarRenderer.Height;
			this.FillRect(0, 0, this.windowX, fillHeight);
			this.FillRect(this.screenWidth - this.windowX, 0, this.windowX, fillHeight);
			this.FillRect(this.windowX, 0, this.screenWidth - 2 * this.windowX, this.windowY);
			this.FillRect(this.windowX, fillHeight - this.windowY, this.screenWidth - 2 * this.windowX, this.windowY);

			var step = 8 * this.drawScale;

			for (var x = this.windowX; x < this.screenWidth - this.windowX; x += step)
			{
				this.screen.DrawPatch(this.borderTop, x, this.windowY - step, this.drawScale);
				this.screen.DrawPatch(this.borderBottom, x, fillHeight - this.windowY, this.drawScale);
			}

			for (var y = this.windowY; y < fillHeight - this.windowY; y += step)
			{
				this.screen.DrawPatch(this.borderLeft, this.windowX - step, y, this.drawScale);
				this.screen.DrawPatch(this.borderRight, this.screenWidth - this.windowX, y, this.drawScale);
			}

			this.screen.DrawPatch(this.borderTopLeft, this.windowX - step, this.windowY - step, this.drawScale);
			this.screen.DrawPatch(this.borderTopRight, this.screenWidth - this.windowX, this.windowY - step, this.drawScale);
			this.screen.DrawPatch(this.borderBottomLeft, this.windowX - step, fillHeight - this.windowY, this.drawScale);
			this.screen.DrawPatch(this.borderBottomRight, this.screenWidth - this.windowX, fillHeight - this.windowY, this.drawScale);
		}

		private void FillRect(int x, int y, int width, int height)
		{
			var data = this.backFlat.Data;

			var srcX = x / this.drawScale;
			var srcY = y / this.drawScale;

			var invScale = Fixed.One / this.drawScale;
			var xFrac = invScale - Fixed.Epsilon;

			for (var i = 0; i < width; i++)
			{
				var src = ((srcX + xFrac.ToIntFloor()) & 63) << 6;
				var dst = this.screenHeight * (x + i) + y;
				var yFrac = invScale - Fixed.Epsilon;

				for (var j = 0; j < height; j++)
				{
					this.screenData[dst + j] = data[src | ((srcY + yFrac.ToIntFloor()) & 63)];
					yFrac += invScale;
				}

				xFrac += invScale;
			}
		}

		////////////////////////////////////////////////////////////
		// Camera view
		////////////////////////////////////////////////////////////

		private World world;

		private Fixed viewX;
		private Fixed viewY;
		private Fixed viewZ;
		private Angle viewAngle;

		private Fixed viewSin;
		private Fixed viewCos;

		private int validCount;

		public void Render(Player player)
		{
			this.world = player.Mobj.World;

			this.viewX = player.Mobj.X;
			this.viewY = player.Mobj.Y;
			this.viewZ = player.ViewZ;
			this.viewAngle = player.Mobj.Angle;

			this.viewSin = Trig.Sin(this.viewAngle);
			this.viewCos = Trig.Cos(this.viewAngle);

			this.validCount = this.world.GetNewValidCount();

			this.extraLight = player.ExtraLight;
			this.fixedColorMap = player.FixedColorMap;

			this.ClearPlaneRendering();
			this.ClearLighting();
			this.ClearRenderingHistory();
			this.ClearSpriteRendering();

			this.RenderBspNode(this.world.Map.Nodes.Length - 1);
			this.RenderSprites();
			this.RenderMaskedTextures();
			this.DrawPlayerSprites(player);

			if (this.windowSize < 7)
			{
				this.FillBackScreen();
			}
		}

		private void RenderBspNode(int node)
		{
			if (Node.IsSubsector(node))
			{
				if (node == -1)
				{
					this.DrawSubsector(0);
				}
				else
				{
					this.DrawSubsector(Node.GetSubsector(node));
				}

				return;
			}

			var bsp = this.world.Map.Nodes[node];

			// Decide which side the view point is on.
			var side = Geometry.PointOnSide(this.viewX, this.viewY, bsp);

			// Recursively divide front space.
			this.RenderBspNode(bsp.Children[side]);

			// Possibly divide back space.
			if (this.IsPotentiallyVisible(bsp.BoundingBox[side ^ 1]))
			{
				this.RenderBspNode(bsp.Children[side ^ 1]);
			}
		}

		private void DrawSubsector(int subsector)
		{
			var target = this.world.Map.Subsectors[subsector];

			this.AddSprites(target.Sector, this.validCount);

			for (var i = 0; i < target.SegCount; i++)
			{
				this.DrawSeg(this.world.Map.Segs[target.FirstSeg + i]);
			}
		}

		private static readonly int[][] viewPosToFrustumTangent =
		{
			new[] {3, 0, 2, 1}, new[] {3, 0, 2, 0}, new[] {3, 1, 2, 0}, new[] {0}, new[] {2, 0, 2, 1}, new[] {0, 0, 0, 0}, new[] {3, 1, 3, 0}, new[] {0},
			new[] {2, 0, 3, 1}, new[] {2, 1, 3, 1}, new[] {2, 1, 3, 0}
		};

		private bool IsPotentiallyVisible(Fixed[] bbox)
		{
			int bx;
			int by;

			// Find the corners of the box that define the edges from
			// current viewpoint.
			if (this.viewX <= bbox[Box.Left])
			{
				bx = 0;
			}
			else if (this.viewX < bbox[Box.Right])
			{
				bx = 1;
			}
			else
			{
				bx = 2;
			}

			if (this.viewY >= bbox[Box.Top])
			{
				by = 0;
			}
			else if (this.viewY > bbox[Box.Bottom])
			{
				by = 1;
			}
			else
			{
				by = 2;
			}

			var viewPos = (by << 2) + bx;

			if (viewPos == 5)
			{
				return true;
			}

			var x1 = bbox[ThreeDRenderer.viewPosToFrustumTangent[viewPos][0]];
			var y1 = bbox[ThreeDRenderer.viewPosToFrustumTangent[viewPos][1]];
			var x2 = bbox[ThreeDRenderer.viewPosToFrustumTangent[viewPos][2]];
			var y2 = bbox[ThreeDRenderer.viewPosToFrustumTangent[viewPos][3]];

			// Check clip list for an open space.
			var angle1 = Geometry.PointToAngle(this.viewX, this.viewY, x1, y1) - this.viewAngle;
			var angle2 = Geometry.PointToAngle(this.viewX, this.viewY, x2, y2) - this.viewAngle;

			var span = angle1 - angle2;

			// Sitting on a line?
			if (span >= Angle.Ang180)
			{
				return true;
			}

			var tSpan1 = angle1 + this.clipAngle;

			if (tSpan1 > this.clipAngle2)
			{
				tSpan1 -= this.clipAngle2;

				// Totally off the left edge?
				if (tSpan1 >= span)
				{
					return false;
				}

				angle1 = this.clipAngle;
			}

			var tSpan2 = this.clipAngle - angle2;

			if (tSpan2 > this.clipAngle2)
			{
				tSpan2 -= this.clipAngle2;

				// Totally off the left edge?
				if (tSpan2 >= span)
				{
					return false;
				}

				angle2 = -this.clipAngle;
			}

			// Find the first clippost that touches the source post
			// (adjacent pixels are touching).
			var sx1 = this.angleToX[(angle1 + Angle.Ang90).Data >> Trig.AngleToFineShift];
			var sx2 = this.angleToX[(angle2 + Angle.Ang90).Data >> Trig.AngleToFineShift];

			// Does not cross a pixel.
			if (sx1 == sx2)
			{
				return false;
			}

			sx2--;

			var start = 0;

			while (this.clipRanges[start].Last < sx2)
			{
				start++;
			}

			if (sx1 >= this.clipRanges[start].First && sx2 <= this.clipRanges[start].Last)
			{
				// The clippost contains the new span.
				return false;
			}

			return true;
		}

		private void DrawSeg(Seg seg)
		{
			// OPTIMIZE: quickly reject orthogonal back sides.
			var angle1 = Geometry.PointToAngle(this.viewX, this.viewY, seg.Vertex1.X, seg.Vertex1.Y);
			var angle2 = Geometry.PointToAngle(this.viewX, this.viewY, seg.Vertex2.X, seg.Vertex2.Y);

			// Clip to view edges.
			// OPTIMIZE: make constant out of 2 * clipangle (FIELDOFVIEW).
			var span = angle1 - angle2;

			// Back side? I.e. backface culling?
			if (span >= Angle.Ang180)
			{
				return;
			}

			// Global angle needed by segcalc.
			var rwAngle1 = angle1;

			angle1 -= this.viewAngle;
			angle2 -= this.viewAngle;

			var tSpan1 = angle1 + this.clipAngle;

			if (tSpan1 > this.clipAngle2)
			{
				tSpan1 -= this.clipAngle2;

				// Totally off the left edge?
				if (tSpan1 >= span)
				{
					return;
				}

				angle1 = this.clipAngle;
			}

			var tSpan2 = this.clipAngle - angle2;

			if (tSpan2 > this.clipAngle2)
			{
				tSpan2 -= this.clipAngle2;

				// Totally off the left edge?
				if (tSpan2 >= span)
				{
					return;
				}

				angle2 = -this.clipAngle;
			}

			// The seg is in the view range, but not necessarily visible.
			var x1 = this.angleToX[(angle1 + Angle.Ang90).Data >> Trig.AngleToFineShift];
			var x2 = this.angleToX[(angle2 + Angle.Ang90).Data >> Trig.AngleToFineShift];

			// Does not cross a pixel?
			if (x1 == x2)
			{
				return;
			}

			var frontSector = seg.FrontSector;
			var backSector = seg.BackSector;

			// Single sided line?
			if (backSector == null)
			{
				this.DrawSolidWall(seg, rwAngle1, x1, x2 - 1);

				return;
			}

			// Closed door.
			if (backSector.CeilingHeight <= frontSector.FloorHeight || backSector.FloorHeight >= frontSector.CeilingHeight)
			{
				this.DrawSolidWall(seg, rwAngle1, x1, x2 - 1);

				return;
			}

			// Window.
			if (backSector.CeilingHeight != frontSector.CeilingHeight || backSector.FloorHeight != frontSector.FloorHeight)
			{
				this.DrawPassWall(seg, rwAngle1, x1, x2 - 1);

				return;
			}

			// Reject empty lines used for triggers and special events.
			// Identical floor and ceiling on both sides, identical
			// light levels on both sides, and no middle texture.
			if (backSector.CeilingFlat == frontSector.CeilingFlat
				&& backSector.FloorFlat == frontSector.FloorFlat
				&& backSector.LightLevel == frontSector.LightLevel
				&& seg.SideDef.MiddleTexture == 0)
			{
				return;
			}

			this.DrawPassWall(seg, rwAngle1, x1, x2 - 1);
		}

		private void DrawSolidWall(Seg seg, Angle rwAngle1, int x1, int x2)
		{
			int next;
			int start;

			// Find the first range that touches the range
			// (adjacent pixels are touching).
			start = 0;

			while (this.clipRanges[start].Last < x1 - 1)
			{
				start++;
			}

			if (x1 < this.clipRanges[start].First)
			{
				if (x2 < this.clipRanges[start].First - 1)
				{
					// Post is entirely visible (above start),
					// so insert a new clippost.
					this.DrawSolidWallRange(seg, rwAngle1, x1, x2);
					next = this.clipRangeCount;
					this.clipRangeCount++;

					while (next != start)
					{
						this.clipRanges[next].CopyFrom(this.clipRanges[next - 1]);
						next--;
					}

					this.clipRanges[next].First = x1;
					this.clipRanges[next].Last = x2;

					return;
				}

				// There is a fragment above *start.
				this.DrawSolidWallRange(seg, rwAngle1, x1, this.clipRanges[start].First - 1);

				// Now adjust the clip size.
				this.clipRanges[start].First = x1;
			}

			// Bottom contained in start?
			if (x2 <= this.clipRanges[start].Last)
			{
				return;
			}

			next = start;

			while (x2 >= this.clipRanges[next + 1].First - 1)
			{
				// There is a fragment between two posts.
				this.DrawSolidWallRange(seg, rwAngle1, this.clipRanges[next].Last + 1, this.clipRanges[next + 1].First - 1);
				next++;

				if (x2 <= this.clipRanges[next].Last)
				{
					// Bottom is contained in next.
					// Adjust the clip size.
					this.clipRanges[start].Last = this.clipRanges[next].Last;

					goto crunch;
				}
			}

			// There is a fragment after *next.
			this.DrawSolidWallRange(seg, rwAngle1, this.clipRanges[next].Last + 1, x2);

			// Adjust the clip size.
			this.clipRanges[start].Last = x2;

			// Remove start + 1 to next from the clip list,
			// because start now covers their area.
			crunch:

			if (next == start)
			{
				// Post just extended past the bottom of one post.
				return;
			}

			while (next++ != this.clipRangeCount)
			{
				// Remove a post.
				this.clipRanges[++start].CopyFrom(this.clipRanges[next]);
			}

			this.clipRangeCount = start + 1;
		}

		private void DrawPassWall(Seg seg, Angle rwAngle1, int x1, int x2)
		{
			int start;

			// Find the first range that touches the range
			// (adjacent pixels are touching).
			start = 0;

			while (this.clipRanges[start].Last < x1 - 1)
			{
				start++;
			}

			if (x1 < this.clipRanges[start].First)
			{
				if (x2 < this.clipRanges[start].First - 1)
				{
					// Post is entirely visible (above start).
					this.DrawPassWallRange(seg, rwAngle1, x1, x2, false);

					return;
				}

				// There is a fragment above *start.
				this.DrawPassWallRange(seg, rwAngle1, x1, this.clipRanges[start].First - 1, false);
			}

			// Bottom contained in start?
			if (x2 <= this.clipRanges[start].Last)
			{
				return;
			}

			while (x2 >= this.clipRanges[start + 1].First - 1)
			{
				// There is a fragment between two posts.
				this.DrawPassWallRange(seg, rwAngle1, this.clipRanges[start].Last + 1, this.clipRanges[start + 1].First - 1, false);
				start++;

				if (x2 <= this.clipRanges[start].Last)
				{
					return;
				}
			}

			// There is a fragment after *next.
			this.DrawPassWallRange(seg, rwAngle1, this.clipRanges[start].Last + 1, x2, false);
		}

		private Fixed ScaleFromGlobalAngle(Angle visAngle, Angle viewAngle, Angle rwNormal, Fixed rwDistance)
		{
			var num = this.projection * Trig.Sin(Angle.Ang90 + (visAngle - rwNormal));
			var den = rwDistance * Trig.Sin(Angle.Ang90 + (visAngle - viewAngle));

			Fixed scale;

			if (den.Data > num.Data >> 16)
			{
				scale = num / den;

				if (scale > Fixed.FromInt(64))
				{
					scale = Fixed.FromInt(64);
				}
				else if (scale.Data < 256)
				{
					scale = new Fixed(256);
				}
			}
			else
			{
				scale = Fixed.FromInt(64);
			}

			return scale;
		}

		private const int heightBits = 12;
		private const int heightUnit = 1 << ThreeDRenderer.heightBits;

		private void DrawSolidWallRange(Seg seg, Angle rwAngle1, int x1, int x2)
		{
			if (seg.BackSector != null)
			{
				this.DrawPassWallRange(seg, rwAngle1, x1, x2, true);

				return;
			}

			if (this.visWallRangeCount == this.visWallRanges.Length)
			{
				// Too many visible walls.
				return;
			}

			// Make some aliases to shorten the following code.
			var line = seg.LineDef;
			var side = seg.SideDef;
			var frontSector = seg.FrontSector;

			// Mark the segment as visible for auto map.
			line.Flags |= LineFlags.Mapped;

			// Calculate the relative plane heights of front and back sector.
			var worldFrontZ1 = frontSector.CeilingHeight - this.viewZ;
			var worldFrontZ2 = frontSector.FloorHeight - this.viewZ;

			// Check which parts must be rendered.
			var drawWall = side.MiddleTexture != 0;
			var drawCeiling = worldFrontZ1 > Fixed.Zero || frontSector.CeilingFlat == this.flats.SkyFlatNumber;
			var drawFloor = worldFrontZ2 < Fixed.Zero;

			//
			// Determine how the wall textures are vertically aligned.
			//

			var wallTexture = this.textures[this.world.Specials.TextureTranslation[side.MiddleTexture]];
			var wallWidthMask = wallTexture.Width - 1;

			Fixed middleTextureAlt;

			if ((line.Flags & LineFlags.DontPegBottom) != 0)
			{
				var vTop = frontSector.FloorHeight + Fixed.FromInt(wallTexture.Height);
				middleTextureAlt = vTop - this.viewZ;
			}
			else
			{
				middleTextureAlt = worldFrontZ1;
			}

			middleTextureAlt += side.RowOffset;

			//
			// Calculate the scaling factors of the left and right edges of the wall range.
			//

			var rwNormalAngle = seg.Angle + Angle.Ang90;

			var offsetAngle = Angle.Abs(rwNormalAngle - rwAngle1);

			if (offsetAngle > Angle.Ang90)
			{
				offsetAngle = Angle.Ang90;
			}

			var distAngle = Angle.Ang90 - offsetAngle;

			var hypotenuse = Geometry.PointToDist(this.viewX, this.viewY, seg.Vertex1.X, seg.Vertex1.Y);

			var rwDistance = hypotenuse * Trig.Sin(distAngle);

			var rwScale = this.ScaleFromGlobalAngle(this.viewAngle + this.xToAngle[x1], this.viewAngle, rwNormalAngle, rwDistance);

			Fixed scale1 = rwScale;
			Fixed scale2;
			Fixed rwScaleStep;

			if (x2 > x1)
			{
				scale2 = this.ScaleFromGlobalAngle(this.viewAngle + this.xToAngle[x2], this.viewAngle, rwNormalAngle, rwDistance);
				rwScaleStep = (scale2 - rwScale) / (x2 - x1);
			}
			else
			{
				scale2 = scale1;
				rwScaleStep = Fixed.Zero;
			}

			//
			// Determine how the wall textures are horizontally aligned
			// and which color map is used according to the light level (if necessary).
			//

			var textureOffsetAngle = rwNormalAngle - rwAngle1;

			if (textureOffsetAngle > Angle.Ang180)
			{
				textureOffsetAngle = -textureOffsetAngle;
			}

			if (textureOffsetAngle > Angle.Ang90)
			{
				textureOffsetAngle = Angle.Ang90;
			}

			var rwOffset = hypotenuse * Trig.Sin(textureOffsetAngle);

			if (rwNormalAngle - rwAngle1 < Angle.Ang180)
			{
				rwOffset = -rwOffset;
			}

			rwOffset += seg.Offset + side.TextureOffset;

			var rwCenterAngle = Angle.Ang90 + this.viewAngle - rwNormalAngle;

			var wallLightLevel = (frontSector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;

			if (seg.Vertex1.Y == seg.Vertex2.Y)
			{
				wallLightLevel--;
			}
			else if (seg.Vertex1.X == seg.Vertex2.X)
			{
				wallLightLevel++;
			}

			var wallLights = this.scaleLight[Math.Clamp(wallLightLevel, 0, ThreeDRenderer.lightLevelCount - 1)];

			//
			// Determine where on the screen the wall is drawn.
			//

			// These values are right shifted to avoid overflow in the following process (maybe).
			worldFrontZ1 >>= 4;
			worldFrontZ2 >>= 4;

			// The Y positions of the top / bottom edges of the wall on the screen.
			var wallY1Frac = (this.centerYFrac >> 4) - worldFrontZ1 * rwScale;
			var wallY1Step = -(rwScaleStep * worldFrontZ1);
			var wallY2Frac = (this.centerYFrac >> 4) - worldFrontZ2 * rwScale;
			var wallY2Step = -(rwScaleStep * worldFrontZ2);

			//
			// Determine which color map is used for the plane according to the light level.
			//

			var planeLightLevel = (frontSector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;

			if (planeLightLevel >= ThreeDRenderer.lightLevelCount)
			{
				planeLightLevel = ThreeDRenderer.lightLevelCount - 1;
			}

			var planeLights = this.zLight[planeLightLevel];

			//
			// Prepare to record the rendering history.
			//

			var visWallRange = this.visWallRanges[this.visWallRangeCount];
			this.visWallRangeCount++;

			visWallRange.Seg = seg;
			visWallRange.X1 = x1;
			visWallRange.X2 = x2;
			visWallRange.Scale1 = scale1;
			visWallRange.Scale2 = scale2;
			visWallRange.ScaleStep = rwScaleStep;
			visWallRange.Silhouette = Silhouette.Both;
			visWallRange.LowerSilHeight = Fixed.MaxValue;
			visWallRange.UpperSilHeight = Fixed.MinValue;
			visWallRange.MaskedTextureColumn = -1;
			visWallRange.UpperClip = this.windowHeightArray;
			visWallRange.LowerClip = this.negOneArray;

			//
			// Floor and ceiling.
			//

			var ceilingFlat = this.flats[this.world.Specials.FlatTranslation[frontSector.CeilingFlat]];
			var floorFlat = this.flats[this.world.Specials.FlatTranslation[frontSector.FloorFlat]];

			//
			// Now the rendering is carried out.
			//

			for (var x = x1; x <= x2; x++)
			{
				var drawWallY1 = (wallY1Frac.Data + ThreeDRenderer.heightUnit - 1) >> ThreeDRenderer.heightBits;
				var drawWallY2 = wallY2Frac.Data >> ThreeDRenderer.heightBits;

				if (drawCeiling)
				{
					var cy1 = this.upperClip[x] + 1;
					var cy2 = Math.Min(drawWallY1 - 1, this.lowerClip[x] - 1);
					this.DrawCeilingColumn(frontSector, ceilingFlat, planeLights, x, cy1, cy2);
				}

				if (drawWall)
				{
					var wy1 = Math.Max(drawWallY1, this.upperClip[x] + 1);
					var wy2 = Math.Min(drawWallY2, this.lowerClip[x] - 1);

					var angle = rwCenterAngle + this.xToAngle[x];
					angle = new Angle(angle.Data & 0x7FFFFFFF);

					var textureColumn = (rwOffset - Trig.Tan(angle) * rwDistance).ToIntFloor();
					var source = wallTexture.Composite.Columns[textureColumn & wallWidthMask][0];

					var lightIndex = rwScale.Data >> ThreeDRenderer.scaleLightShift;

					if (lightIndex >= this.maxScaleLight)
					{
						lightIndex = this.maxScaleLight - 1;
					}

					var invScale = new Fixed((int) (0xffffffffu / (uint) rwScale.Data));
					this.DrawColumn(source, wallLights[lightIndex], x, wy1, wy2, invScale, middleTextureAlt);
				}

				if (drawFloor)
				{
					var fy1 = Math.Max(drawWallY2 + 1, this.upperClip[x] + 1);
					var fy2 = this.lowerClip[x] - 1;
					this.DrawFloorColumn(frontSector, floorFlat, planeLights, x, fy1, fy2);
				}

				rwScale += rwScaleStep;
				wallY1Frac += wallY1Step;
				wallY2Frac += wallY2Step;
			}
		}

		private void DrawPassWallRange(Seg seg, Angle rwAngle1, int x1, int x2, bool drawAsSolidWall)
		{
			if (this.visWallRangeCount == this.visWallRanges.Length)
			{
				// Too many visible walls.
				return;
			}

			var range = x2 - x1 + 1;

			if (this.clipDataLength + 3 * range >= this.clipData.Length)
			{
				// Clip info buffer is not sufficient.
				return;
			}

			// Make some aliases to shorten the following code.
			var line = seg.LineDef;
			var side = seg.SideDef;
			var frontSector = seg.FrontSector;
			var backSector = seg.BackSector;

			// Mark the segment as visible for auto map.
			line.Flags |= LineFlags.Mapped;

			// Calculate the relative plane heights of front and back sector.
			// These values are later 4 bits right shifted to calculate the rendering area.
			var worldFrontZ1 = frontSector.CeilingHeight - this.viewZ;
			var worldFrontZ2 = frontSector.FloorHeight - this.viewZ;
			var worldBackZ1 = backSector.CeilingHeight - this.viewZ;
			var worldBackZ2 = backSector.FloorHeight - this.viewZ;

			// The hack below enables ceiling height change in outdoor area without showing the upper wall.
			if (frontSector.CeilingFlat == this.flats.SkyFlatNumber && backSector.CeilingFlat == this.flats.SkyFlatNumber)
			{
				worldFrontZ1 = worldBackZ1;
			}

			//
			// Check which parts must be rendered.
			//

			bool drawUpperWall;
			bool drawCeiling;

			if (drawAsSolidWall
				|| worldFrontZ1 != worldBackZ1
				|| frontSector.CeilingFlat != backSector.CeilingFlat
				|| frontSector.LightLevel != backSector.LightLevel)
			{
				drawUpperWall = side.TopTexture != 0 && worldBackZ1 < worldFrontZ1;
				drawCeiling = worldFrontZ1 >= Fixed.Zero || frontSector.CeilingFlat == this.flats.SkyFlatNumber;
			}
			else
			{
				drawUpperWall = false;
				drawCeiling = false;
			}

			bool drawLowerWall;
			bool drawFloor;

			if (drawAsSolidWall
				|| worldFrontZ2 != worldBackZ2
				|| frontSector.FloorFlat != backSector.FloorFlat
				|| frontSector.LightLevel != backSector.LightLevel)
			{
				drawLowerWall = side.BottomTexture != 0 && worldBackZ2 > worldFrontZ2;
				drawFloor = worldFrontZ2 <= Fixed.Zero;
			}
			else
			{
				drawLowerWall = false;
				drawFloor = false;
			}

			var drawMaskedTexture = side.MiddleTexture != 0;

			// If nothing must be rendered, we can skip this seg.
			if (!drawUpperWall && !drawCeiling && !drawLowerWall && !drawFloor && !drawMaskedTexture)
			{
				return;
			}

			var segTextured = drawUpperWall || drawLowerWall || drawMaskedTexture;

			//
			// Determine how the wall textures are vertically aligned (if necessary).
			//

			var upperWallTexture = default(Texture);
			var upperWallWidthMask = default(int);
			var uperTextureAlt = default(Fixed);

			if (drawUpperWall)
			{
				upperWallTexture = this.textures[this.world.Specials.TextureTranslation[side.TopTexture]];
				upperWallWidthMask = upperWallTexture.Width - 1;

				if ((line.Flags & LineFlags.DontPegTop) != 0)
				{
					uperTextureAlt = worldFrontZ1;
				}
				else
				{
					var vTop = backSector.CeilingHeight + Fixed.FromInt(upperWallTexture.Height);
					uperTextureAlt = vTop - this.viewZ;
				}

				uperTextureAlt += side.RowOffset;
			}

			var lowerWallTexture = default(Texture);
			var lowerWallWidthMask = default(int);
			var lowerTextureAlt = default(Fixed);

			if (drawLowerWall)
			{
				lowerWallTexture = this.textures[this.world.Specials.TextureTranslation[side.BottomTexture]];
				lowerWallWidthMask = lowerWallTexture.Width - 1;

				if ((line.Flags & LineFlags.DontPegBottom) != 0)
				{
					lowerTextureAlt = worldFrontZ1;
				}
				else
				{
					lowerTextureAlt = worldBackZ2;
				}

				lowerTextureAlt += side.RowOffset;
			}

			//
			// Calculate the scaling factors of the left and right edges of the wall range.
			//

			var rwNormalAngle = seg.Angle + Angle.Ang90;

			var offsetAngle = Angle.Abs(rwNormalAngle - rwAngle1);

			if (offsetAngle > Angle.Ang90)
			{
				offsetAngle = Angle.Ang90;
			}

			var distAngle = Angle.Ang90 - offsetAngle;

			var hypotenuse = Geometry.PointToDist(this.viewX, this.viewY, seg.Vertex1.X, seg.Vertex1.Y);

			var rwDistance = hypotenuse * Trig.Sin(distAngle);

			var rwScale = this.ScaleFromGlobalAngle(this.viewAngle + this.xToAngle[x1], this.viewAngle, rwNormalAngle, rwDistance);

			Fixed scale1 = rwScale;
			Fixed scale2;
			Fixed rwScaleStep;

			if (x2 > x1)
			{
				scale2 = this.ScaleFromGlobalAngle(this.viewAngle + this.xToAngle[x2], this.viewAngle, rwNormalAngle, rwDistance);
				rwScaleStep = (scale2 - rwScale) / (x2 - x1);
			}
			else
			{
				scale2 = scale1;
				rwScaleStep = Fixed.Zero;
			}

			//
			// Determine how the wall textures are horizontally aligned
			// and which color map is used according to the light level (if necessary).
			//

			var rwOffset = default(Fixed);
			var rwCenterAngle = default(Angle);
			var wallLights = default(byte[][]);

			if (segTextured)
			{
				var textureOffsetAngle = rwNormalAngle - rwAngle1;

				if (textureOffsetAngle > Angle.Ang180)
				{
					textureOffsetAngle = -textureOffsetAngle;
				}

				if (textureOffsetAngle > Angle.Ang90)
				{
					textureOffsetAngle = Angle.Ang90;
				}

				rwOffset = hypotenuse * Trig.Sin(textureOffsetAngle);

				if (rwNormalAngle - rwAngle1 < Angle.Ang180)
				{
					rwOffset = -rwOffset;
				}

				rwOffset += seg.Offset + side.TextureOffset;

				rwCenterAngle = Angle.Ang90 + this.viewAngle - rwNormalAngle;

				var wallLightLevel = (frontSector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;

				if (seg.Vertex1.Y == seg.Vertex2.Y)
				{
					wallLightLevel--;
				}
				else if (seg.Vertex1.X == seg.Vertex2.X)
				{
					wallLightLevel++;
				}

				wallLights = this.scaleLight[Math.Clamp(wallLightLevel, 0, ThreeDRenderer.lightLevelCount - 1)];
			}

			//
			// Determine where on the screen the wall is drawn.
			//

			// These values are right shifted to avoid overflow in the following process.
			worldFrontZ1 >>= 4;
			worldFrontZ2 >>= 4;
			worldBackZ1 >>= 4;
			worldBackZ2 >>= 4;

			// The Y positions of the top / bottom edges of the wall on the screen..
			var wallY1Frac = (this.centerYFrac >> 4) - worldFrontZ1 * rwScale;
			var wallY1Step = -(rwScaleStep * worldFrontZ1);
			var wallY2Frac = (this.centerYFrac >> 4) - worldFrontZ2 * rwScale;
			var wallY2Step = -(rwScaleStep * worldFrontZ2);

			// The Y position of the top edge of the portal (if visible).
			var portalY1Frac = default(Fixed);
			var portalY1Step = default(Fixed);

			if (drawUpperWall)
			{
				if (worldBackZ1 > worldFrontZ2)
				{
					portalY1Frac = (this.centerYFrac >> 4) - worldBackZ1 * rwScale;
					portalY1Step = -(rwScaleStep * worldBackZ1);
				}
				else
				{
					portalY1Frac = (this.centerYFrac >> 4) - worldFrontZ2 * rwScale;
					portalY1Step = -(rwScaleStep * worldFrontZ2);
				}
			}

			// The Y position of the bottom edge of the portal (if visible).
			var portalY2Frac = default(Fixed);
			var portalY2Step = default(Fixed);

			if (drawLowerWall)
			{
				if (worldBackZ2 < worldFrontZ1)
				{
					portalY2Frac = (this.centerYFrac >> 4) - worldBackZ2 * rwScale;
					portalY2Step = -(rwScaleStep * worldBackZ2);
				}
				else
				{
					portalY2Frac = (this.centerYFrac >> 4) - worldFrontZ1 * rwScale;
					portalY2Step = -(rwScaleStep * worldFrontZ1);
				}
			}

			//
			// Determine which color map is used for the plane according to the light level.
			//

			var planeLightLevel = (frontSector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;

			if (planeLightLevel >= ThreeDRenderer.lightLevelCount)
			{
				planeLightLevel = ThreeDRenderer.lightLevelCount - 1;
			}

			var planeLights = this.zLight[planeLightLevel];

			//
			// Prepare to record the rendering history.
			//

			var visWallRange = this.visWallRanges[this.visWallRangeCount];
			this.visWallRangeCount++;

			visWallRange.Seg = seg;
			visWallRange.X1 = x1;
			visWallRange.X2 = x2;
			visWallRange.Scale1 = scale1;
			visWallRange.Scale2 = scale2;
			visWallRange.ScaleStep = rwScaleStep;

			visWallRange.UpperClip = -1;
			visWallRange.LowerClip = -1;
			visWallRange.Silhouette = 0;

			if (frontSector.FloorHeight > backSector.FloorHeight)
			{
				visWallRange.Silhouette = Silhouette.Lower;
				visWallRange.LowerSilHeight = frontSector.FloorHeight;
			}
			else if (backSector.FloorHeight > this.viewZ)
			{
				visWallRange.Silhouette = Silhouette.Lower;
				visWallRange.LowerSilHeight = Fixed.MaxValue;
			}

			if (frontSector.CeilingHeight < backSector.CeilingHeight)
			{
				visWallRange.Silhouette |= Silhouette.Upper;
				visWallRange.UpperSilHeight = frontSector.CeilingHeight;
			}
			else if (backSector.CeilingHeight < this.viewZ)
			{
				visWallRange.Silhouette |= Silhouette.Upper;
				visWallRange.UpperSilHeight = Fixed.MinValue;
			}

			if (backSector.CeilingHeight <= frontSector.FloorHeight)
			{
				visWallRange.LowerClip = this.negOneArray;
				visWallRange.LowerSilHeight = Fixed.MaxValue;
				visWallRange.Silhouette |= Silhouette.Lower;
			}

			if (backSector.FloorHeight >= frontSector.CeilingHeight)
			{
				visWallRange.UpperClip = this.windowHeightArray;
				visWallRange.UpperSilHeight = Fixed.MinValue;
				visWallRange.Silhouette |= Silhouette.Upper;
			}

			var maskedTextureColumn = default(int);

			if (drawMaskedTexture)
			{
				maskedTextureColumn = this.clipDataLength - x1;
				visWallRange.MaskedTextureColumn = maskedTextureColumn;
				this.clipDataLength += range;
			}
			else
			{
				visWallRange.MaskedTextureColumn = -1;
			}

			//
			// Floor and ceiling.
			//

			var ceilingFlat = this.flats[this.world.Specials.FlatTranslation[frontSector.CeilingFlat]];
			var floorFlat = this.flats[this.world.Specials.FlatTranslation[frontSector.FloorFlat]];

			//
			// Now the rendering is carried out.
			//

			for (var x = x1; x <= x2; x++)
			{
				var drawWallY1 = (wallY1Frac.Data + ThreeDRenderer.heightUnit - 1) >> ThreeDRenderer.heightBits;
				var drawWallY2 = wallY2Frac.Data >> ThreeDRenderer.heightBits;

				var textureColumn = default(int);
				var lightIndex = default(int);
				var invScale = default(Fixed);

				if (segTextured)
				{
					var angle = rwCenterAngle + this.xToAngle[x];
					angle = new Angle(angle.Data & 0x7FFFFFFF);
					textureColumn = (rwOffset - Trig.Tan(angle) * rwDistance).ToIntFloor();

					lightIndex = rwScale.Data >> ThreeDRenderer.scaleLightShift;

					if (lightIndex >= this.maxScaleLight)
					{
						lightIndex = this.maxScaleLight - 1;
					}

					invScale = new Fixed((int) (0xffffffffu / (uint) rwScale.Data));
				}

				if (drawUpperWall)
				{
					var drawUpperWallY1 = (wallY1Frac.Data + ThreeDRenderer.heightUnit - 1) >> ThreeDRenderer.heightBits;
					var drawUpperWallY2 = portalY1Frac.Data >> ThreeDRenderer.heightBits;

					if (drawCeiling)
					{
						var cy1 = this.upperClip[x] + 1;
						var cy2 = Math.Min(drawWallY1 - 1, this.lowerClip[x] - 1);
						this.DrawCeilingColumn(frontSector, ceilingFlat, planeLights, x, cy1, cy2);
					}

					var wy1 = Math.Max(drawUpperWallY1, this.upperClip[x] + 1);
					var wy2 = Math.Min(drawUpperWallY2, this.lowerClip[x] - 1);
					var source = upperWallTexture.Composite.Columns[textureColumn & upperWallWidthMask];

					if (source.Length > 0)
					{
						this.DrawColumn(source[0], wallLights[lightIndex], x, wy1, wy2, invScale, uperTextureAlt);
					}

					if (this.upperClip[x] < wy2)
					{
						this.upperClip[x] = (short) wy2;
					}

					portalY1Frac += portalY1Step;
				}
				else if (drawCeiling)
				{
					var cy1 = this.upperClip[x] + 1;
					var cy2 = Math.Min(drawWallY1 - 1, this.lowerClip[x] - 1);
					this.DrawCeilingColumn(frontSector, ceilingFlat, planeLights, x, cy1, cy2);

					if (this.upperClip[x] < cy2)
					{
						this.upperClip[x] = (short) cy2;
					}
				}

				if (drawLowerWall)
				{
					var drawLowerWallY1 = (portalY2Frac.Data + ThreeDRenderer.heightUnit - 1) >> ThreeDRenderer.heightBits;
					var drawLowerWallY2 = wallY2Frac.Data >> ThreeDRenderer.heightBits;

					var wy1 = Math.Max(drawLowerWallY1, this.upperClip[x] + 1);
					var wy2 = Math.Min(drawLowerWallY2, this.lowerClip[x] - 1);
					var source = lowerWallTexture.Composite.Columns[textureColumn & lowerWallWidthMask];

					if (source.Length > 0)
					{
						this.DrawColumn(source[0], wallLights[lightIndex], x, wy1, wy2, invScale, lowerTextureAlt);
					}

					if (drawFloor)
					{
						var fy1 = Math.Max(drawWallY2 + 1, this.upperClip[x] + 1);
						var fy2 = this.lowerClip[x] - 1;
						this.DrawFloorColumn(frontSector, floorFlat, planeLights, x, fy1, fy2);
					}

					if (this.lowerClip[x] > wy1)
					{
						this.lowerClip[x] = (short) wy1;
					}

					portalY2Frac += portalY2Step;
				}
				else if (drawFloor)
				{
					var fy1 = Math.Max(drawWallY2 + 1, this.upperClip[x] + 1);
					var fy2 = this.lowerClip[x] - 1;
					this.DrawFloorColumn(frontSector, floorFlat, planeLights, x, fy1, fy2);

					if (this.lowerClip[x] > drawWallY2 + 1)
					{
						this.lowerClip[x] = (short) fy1;
					}
				}

				if (drawMaskedTexture)
				{
					this.clipData[maskedTextureColumn + x] = (short) textureColumn;
				}

				rwScale += rwScaleStep;
				wallY1Frac += wallY1Step;
				wallY2Frac += wallY2Step;
			}

			//
			// Save sprite clipping info.
			//

			if (((visWallRange.Silhouette & Silhouette.Upper) != 0 || drawMaskedTexture) && visWallRange.UpperClip == -1)
			{
				Array.Copy(this.upperClip, x1, this.clipData, this.clipDataLength, range);
				visWallRange.UpperClip = this.clipDataLength - x1;
				this.clipDataLength += range;
			}

			if (((visWallRange.Silhouette & Silhouette.Lower) != 0 || drawMaskedTexture) && visWallRange.LowerClip == -1)
			{
				Array.Copy(this.lowerClip, x1, this.clipData, this.clipDataLength, range);
				visWallRange.LowerClip = this.clipDataLength - x1;
				this.clipDataLength += range;
			}

			if (drawMaskedTexture && (visWallRange.Silhouette & Silhouette.Upper) == 0)
			{
				visWallRange.Silhouette |= Silhouette.Upper;
				visWallRange.UpperSilHeight = Fixed.MinValue;
			}

			if (drawMaskedTexture && (visWallRange.Silhouette & Silhouette.Lower) == 0)
			{
				visWallRange.Silhouette |= Silhouette.Lower;
				visWallRange.LowerSilHeight = Fixed.MaxValue;
			}
		}

		private void RenderMaskedTextures()
		{
			for (var i = this.visWallRangeCount - 1; i >= 0; i--)
			{
				var drawSeg = this.visWallRanges[i];

				if (drawSeg.MaskedTextureColumn != -1)
				{
					this.DrawMaskedRange(drawSeg, drawSeg.X1, drawSeg.X2);
				}
			}
		}

		private void DrawMaskedRange(VisWallRange drawSeg, int x1, int x2)
		{
			var seg = drawSeg.Seg;

			var wallLightLevel = (seg.FrontSector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;

			if (seg.Vertex1.Y == seg.Vertex2.Y)
			{
				wallLightLevel--;
			}
			else if (seg.Vertex1.X == seg.Vertex2.X)
			{
				wallLightLevel++;
			}

			var wallLights = this.scaleLight[Math.Clamp(wallLightLevel, 0, ThreeDRenderer.lightLevelCount - 1)];

			var wallTexture = this.textures[this.world.Specials.TextureTranslation[seg.SideDef.MiddleTexture]];
			var mask = wallTexture.Width - 1;

			Fixed midTextureAlt;

			if ((seg.LineDef.Flags & LineFlags.DontPegBottom) != 0)
			{
				midTextureAlt = seg.FrontSector.FloorHeight > seg.BackSector.FloorHeight ? seg.FrontSector.FloorHeight : seg.BackSector.FloorHeight;
				midTextureAlt = midTextureAlt + Fixed.FromInt(wallTexture.Height) - this.viewZ;
			}
			else
			{
				midTextureAlt = seg.FrontSector.CeilingHeight < seg.BackSector.CeilingHeight ? seg.FrontSector.CeilingHeight : seg.BackSector.CeilingHeight;
				midTextureAlt = midTextureAlt - this.viewZ;
			}

			midTextureAlt += seg.SideDef.RowOffset;

			var scaleStep = drawSeg.ScaleStep;
			var scale = drawSeg.Scale1 + (x1 - drawSeg.X1) * scaleStep;

			for (var x = x1; x <= x2; x++)
			{
				var index = Math.Min(scale.Data >> ThreeDRenderer.scaleLightShift, this.maxScaleLight - 1);

				var col = this.clipData[drawSeg.MaskedTextureColumn + x];

				if (col != short.MaxValue)
				{
					var topY = this.centerYFrac - midTextureAlt * scale;
					var invScale = new Fixed((int) (0xffffffffu / (uint) scale.Data));
					var ceilClip = this.clipData[drawSeg.UpperClip + x];
					var floorClip = this.clipData[drawSeg.LowerClip + x];

					this.DrawMaskedColumn(
						wallTexture.Composite.Columns[col & mask],
						wallLights[index],
						x,
						topY,
						scale,
						invScale,
						midTextureAlt,
						ceilClip,
						floorClip
					);

					this.clipData[drawSeg.MaskedTextureColumn + x] = short.MaxValue;
				}

				scale += scaleStep;
			}
		}

		private void DrawCeilingColumn(Sector sector, Flat flat, byte[][] planeLights, int x, int y1, int y2)
		{
			if (flat == this.flats.SkyFlat)
			{
				this.DrawSkyColumn(x, y1, y2);

				return;
			}

			if (y2 - y1 < 0)
			{
				return;
			}

			var height = Fixed.Abs(sector.CeilingHeight - this.viewZ);

			var flatData = flat.Data;

			if (sector == this.ceilingPrevSector && this.ceilingPrevX == x - 1)
			{
				var p1 = Math.Max(y1, this.ceilingPrevY1);
				var p2 = Math.Min(y2, this.ceilingPrevY2);

				var pos = this.screenHeight * (this.windowX + x) + this.windowY + y1;

				for (var y = y1; y < p1; y++)
				{
					var distance = height * this.planeYSlope[y];
					this.ceilingXStep[y] = distance * this.planeBaseXScale;
					this.ceilingYStep[y] = distance * this.planeBaseYScale;

					var length = distance * this.planeDistScale[x];
					var angle = this.viewAngle + this.xToAngle[x];
					var xFrac = this.viewX + Trig.Cos(angle) * length;
					var yFrac = -this.viewY - Trig.Sin(angle) * length;
					this.ceilingXFrac[y] = xFrac;
					this.ceilingYFrac[y] = yFrac;

					var colorMap = planeLights[Math.Min((uint) (distance.Data >> ThreeDRenderer.zLightShift), ThreeDRenderer.maxZLight - 1)];
					this.ceilingLights[y] = colorMap;

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = colorMap[flatData[spot]];
					pos++;
				}

				for (var y = p1; y <= p2; y++)
				{
					var xFrac = this.ceilingXFrac[y] + this.ceilingXStep[y];
					var yFrac = this.ceilingYFrac[y] + this.ceilingYStep[y];

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = this.ceilingLights[y][flatData[spot]];
					pos++;

					this.ceilingXFrac[y] = xFrac;
					this.ceilingYFrac[y] = yFrac;
				}

				for (var y = p2 + 1; y <= y2; y++)
				{
					var distance = height * this.planeYSlope[y];
					this.ceilingXStep[y] = distance * this.planeBaseXScale;
					this.ceilingYStep[y] = distance * this.planeBaseYScale;

					var length = distance * this.planeDistScale[x];
					var angle = this.viewAngle + this.xToAngle[x];
					var xFrac = this.viewX + Trig.Cos(angle) * length;
					var yFrac = -this.viewY - Trig.Sin(angle) * length;
					this.ceilingXFrac[y] = xFrac;
					this.ceilingYFrac[y] = yFrac;

					var colorMap = planeLights[Math.Min((uint) (distance.Data >> ThreeDRenderer.zLightShift), ThreeDRenderer.maxZLight - 1)];
					this.ceilingLights[y] = colorMap;

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = colorMap[flatData[spot]];
					pos++;
				}
			}
			else
			{
				var pos = this.screenHeight * (this.windowX + x) + this.windowY + y1;

				for (var y = y1; y <= y2; y++)
				{
					var distance = height * this.planeYSlope[y];
					this.ceilingXStep[y] = distance * this.planeBaseXScale;
					this.ceilingYStep[y] = distance * this.planeBaseYScale;

					var length = distance * this.planeDistScale[x];
					var angle = this.viewAngle + this.xToAngle[x];
					var xFrac = this.viewX + Trig.Cos(angle) * length;
					var yFrac = -this.viewY - Trig.Sin(angle) * length;
					this.ceilingXFrac[y] = xFrac;
					this.ceilingYFrac[y] = yFrac;

					var colorMap = planeLights[Math.Min((uint) (distance.Data >> ThreeDRenderer.zLightShift), ThreeDRenderer.maxZLight - 1)];
					this.ceilingLights[y] = colorMap;

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = colorMap[flatData[spot]];
					pos++;
				}
			}

			this.ceilingPrevSector = sector;
			this.ceilingPrevX = x;
			this.ceilingPrevY1 = y1;
			this.ceilingPrevY2 = y2;
		}

		private void DrawFloorColumn(Sector sector, Flat flat, byte[][] planeLights, int x, int y1, int y2)
		{
			if (flat == this.flats.SkyFlat)
			{
				this.DrawSkyColumn(x, y1, y2);

				return;
			}

			if (y2 - y1 < 0)
			{
				return;
			}

			var height = Fixed.Abs(sector.FloorHeight - this.viewZ);

			var flatData = flat.Data;

			if (sector == this.floorPrevSector && this.floorPrevX == x - 1)
			{
				var p1 = Math.Max(y1, this.floorPrevY1);
				var p2 = Math.Min(y2, this.floorPrevY2);

				var pos = this.screenHeight * (this.windowX + x) + this.windowY + y1;

				for (var y = y1; y < p1; y++)
				{
					var distance = height * this.planeYSlope[y];
					this.floorXStep[y] = distance * this.planeBaseXScale;
					this.floorYStep[y] = distance * this.planeBaseYScale;

					var length = distance * this.planeDistScale[x];
					var angle = this.viewAngle + this.xToAngle[x];
					var xFrac = this.viewX + Trig.Cos(angle) * length;
					var yFrac = -this.viewY - Trig.Sin(angle) * length;
					this.floorXFrac[y] = xFrac;
					this.floorYFrac[y] = yFrac;

					var colorMap = planeLights[Math.Min((uint) (distance.Data >> ThreeDRenderer.zLightShift), ThreeDRenderer.maxZLight - 1)];
					this.floorLights[y] = colorMap;

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = colorMap[flatData[spot]];
					pos++;
				}

				for (var y = p1; y <= p2; y++)
				{
					var xFrac = this.floorXFrac[y] + this.floorXStep[y];
					var yFrac = this.floorYFrac[y] + this.floorYStep[y];

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = this.floorLights[y][flatData[spot]];
					pos++;

					this.floorXFrac[y] = xFrac;
					this.floorYFrac[y] = yFrac;
				}

				for (var y = p2 + 1; y <= y2; y++)
				{
					var distance = height * this.planeYSlope[y];
					this.floorXStep[y] = distance * this.planeBaseXScale;
					this.floorYStep[y] = distance * this.planeBaseYScale;

					var length = distance * this.planeDistScale[x];
					var angle = this.viewAngle + this.xToAngle[x];
					var xFrac = this.viewX + Trig.Cos(angle) * length;
					var yFrac = -this.viewY - Trig.Sin(angle) * length;
					this.floorXFrac[y] = xFrac;
					this.floorYFrac[y] = yFrac;

					var colorMap = planeLights[Math.Min((uint) (distance.Data >> ThreeDRenderer.zLightShift), ThreeDRenderer.maxZLight - 1)];
					this.floorLights[y] = colorMap;

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = colorMap[flatData[spot]];
					pos++;
				}
			}
			else
			{
				var pos = this.screenHeight * (this.windowX + x) + this.windowY + y1;

				for (var y = y1; y <= y2; y++)
				{
					var distance = height * this.planeYSlope[y];
					this.floorXStep[y] = distance * this.planeBaseXScale;
					this.floorYStep[y] = distance * this.planeBaseYScale;

					var length = distance * this.planeDistScale[x];
					var angle = this.viewAngle + this.xToAngle[x];
					var xFrac = this.viewX + Trig.Cos(angle) * length;
					var yFrac = -this.viewY - Trig.Sin(angle) * length;
					this.floorXFrac[y] = xFrac;
					this.floorYFrac[y] = yFrac;

					var colorMap = planeLights[Math.Min((uint) (distance.Data >> ThreeDRenderer.zLightShift), ThreeDRenderer.maxZLight - 1)];
					this.floorLights[y] = colorMap;

					var spot = ((yFrac.Data >> (16 - 6)) & (63 * 64)) + ((xFrac.Data >> 16) & 63);
					this.screenData[pos] = colorMap[flatData[spot]];
					pos++;
				}
			}

			this.floorPrevSector = sector;
			this.floorPrevX = x;
			this.floorPrevY1 = y1;
			this.floorPrevY2 = y2;
		}

		private void DrawColumn(Column column, byte[] map, int x, int y1, int y2, Fixed invScale, Fixed textureAlt)
		{
			if (y2 - y1 < 0)
			{
				return;
			}

			// Framebuffer destination address.
			// Use ylookup LUT to avoid multiply with ScreenWidth.
			// Use columnofs LUT for subwindows? 
			var pos1 = this.screenHeight * (this.windowX + x) + this.windowY + y1;
			var pos2 = pos1 + (y2 - y1);

			// Determine scaling, which is the only mapping to be done.
			var fracStep = invScale;
			var frac = textureAlt + (y1 - this.centerY) * fracStep;

			// Inner loop that does the actual texture mapping,
			// e.g. a DDA-lile scaling.
			// This is as fast as it gets.
			var source = column.Data;
			var offset = column.Offset;

			for (var pos = pos1; pos <= pos2; pos++)
			{
				// Re-map color indices from wall texture column
				// using a lighting/special effects LUT.
				this.screenData[pos] = map[source[offset + ((frac.Data >> Fixed.FracBits) & 127)]];
				frac += fracStep;
			}
		}

		private void DrawColumnTranslation(Column column, byte[] translation, byte[] map, int x, int y1, int y2, Fixed invScale, Fixed textureAlt)
		{
			if (y2 - y1 < 0)
			{
				return;
			}

			// Framebuffer destination address.
			// Use ylookup LUT to avoid multiply with ScreenWidth.
			// Use columnofs LUT for subwindows? 
			var pos1 = this.screenHeight * (this.windowX + x) + this.windowY + y1;
			var pos2 = pos1 + (y2 - y1);

			// Determine scaling, which is the only mapping to be done.
			var fracStep = invScale;
			var frac = textureAlt + (y1 - this.centerY) * fracStep;

			// Inner loop that does the actual texture mapping,
			// e.g. a DDA-lile scaling.
			// This is as fast as it gets.
			var source = column.Data;
			var offset = column.Offset;

			for (var pos = pos1; pos <= pos2; pos++)
			{
				// Re-map color indices from wall texture column
				// using a lighting/special effects LUT.
				this.screenData[pos] = map[translation[source[offset + ((frac.Data >> Fixed.FracBits) & 127)]]];
				frac += fracStep;
			}
		}

		private void DrawFuzzColumn(Column column, int x, int y1, int y2)
		{
			if (y2 - y1 < 0)
			{
				return;
			}

			if (y1 == 0)
			{
				y1 = 1;
			}

			if (y2 == this.windowHeight - 1)
			{
				y2 = this.windowHeight - 2;
			}

			var pos1 = this.screenHeight * (this.windowX + x) + this.windowY + y1;
			var pos2 = pos1 + (y2 - y1);

			var map = this.colorMap[6];

			for (var pos = pos1; pos <= pos2; pos++)
			{
				this.screenData[pos] = map[this.screenData[pos + ThreeDRenderer.fuzzTable[this.fuzzPos]]];

				if (++this.fuzzPos == ThreeDRenderer.fuzzTable.Length)
				{
					this.fuzzPos = 0;
				}
			}
		}

		private void DrawSkyColumn(int x, int y1, int y2)
		{
			var angle = (this.viewAngle + this.xToAngle[x]).Data >> ThreeDRenderer.angleToSkyShift;
			var mask = this.world.Map.SkyTexture.Width - 1;
			var source = this.world.Map.SkyTexture.Composite.Columns[angle & mask];
			this.DrawColumn(source[0], this.colorMap[0], x, y1, y2, this.skyInvScale, this.skyTextureAlt);
		}

		private void DrawMaskedColumn(
			Column[] columns,
			byte[] map,
			int x,
			Fixed topY,
			Fixed scale,
			Fixed invScale,
			Fixed textureAlt,
			int upperClip,
			int lowerClip
		)
		{
			foreach (var column in columns)
			{
				var y1Frac = topY + scale * column.TopDelta;
				var y2Frac = y1Frac + scale * column.Length;
				var y1 = (y1Frac.Data + Fixed.FracUnit - 1) >> Fixed.FracBits;
				var y2 = (y2Frac.Data - 1) >> Fixed.FracBits;

				y1 = Math.Max(y1, upperClip + 1);
				y2 = Math.Min(y2, lowerClip - 1);

				if (y1 <= y2)
				{
					var alt = new Fixed(textureAlt.Data - (column.TopDelta << Fixed.FracBits));
					this.DrawColumn(column, map, x, y1, y2, invScale, alt);
				}
			}
		}

		private void DrawMaskedColumnTranslation(
			Column[] columns,
			byte[] translation,
			byte[] map,
			int x,
			Fixed topY,
			Fixed scale,
			Fixed invScale,
			Fixed textureAlt,
			int upperClip,
			int lowerClip
		)
		{
			foreach (var column in columns)
			{
				var y1Frac = topY + scale * column.TopDelta;
				var y2Frac = y1Frac + scale * column.Length;
				var y1 = (y1Frac.Data + Fixed.FracUnit - 1) >> Fixed.FracBits;
				var y2 = (y2Frac.Data - 1) >> Fixed.FracBits;

				y1 = Math.Max(y1, upperClip + 1);
				y2 = Math.Min(y2, lowerClip - 1);

				if (y1 <= y2)
				{
					var alt = new Fixed(textureAlt.Data - (column.TopDelta << Fixed.FracBits));
					this.DrawColumnTranslation(column, translation, map, x, y1, y2, invScale, alt);
				}
			}
		}

		private void DrawMaskedFuzzColumn(Column[] columns, int x, Fixed topY, Fixed scale, int upperClip, int lowerClip)
		{
			foreach (var column in columns)
			{
				var y1Frac = topY + scale * column.TopDelta;
				var y2Frac = y1Frac + scale * column.Length;
				var y1 = (y1Frac.Data + Fixed.FracUnit - 1) >> Fixed.FracBits;
				var y2 = (y2Frac.Data - 1) >> Fixed.FracBits;

				y1 = Math.Max(y1, upperClip + 1);
				y2 = Math.Min(y2, lowerClip - 1);

				if (y1 <= y2)
				{
					this.DrawFuzzColumn(column, x, y1, y2);
				}
			}
		}

		private void AddSprites(Sector sector, int validCount)
		{
			// BSP is traversed by subsector.
			// A sector might have been split into several subsectors during BSP building.
			// Thus we check whether its already added.
			if (sector.ValidCount == validCount)
			{
				return;
			}

			// Well, now it will be done.
			sector.ValidCount = validCount;

			var spriteLightLevel = (sector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;
			var spriteLights = this.scaleLight[Math.Clamp(spriteLightLevel, 0, ThreeDRenderer.lightLevelCount - 1)];

			// Handle all things in sector.
			foreach (var thing in sector)
			{
				this.ProjectSprite(thing, spriteLights);
			}
		}

		private void ProjectSprite(Mobj thing, byte[][] spriteLights)
		{
			if (this.visSpriteCount == this.visSprites.Length)
			{
				// Too many sprites.
				return;
			}

			// Transform the origin point.
			var trX = thing.X - this.viewX;
			var trY = thing.Y - this.viewY;

			var gxt = (trX * this.viewCos);
			var gyt = -(trY * this.viewSin);

			var tz = gxt - gyt;

			// Thing is behind view plane?
			if (tz < ThreeDRenderer.minZ)
			{
				return;
			}

			var xScale = this.projection / tz;

			gxt = -trX * this.viewSin;
			gyt = trY * this.viewCos;
			var tx = -(gyt + gxt);

			// Too far off the side?
			if (Fixed.Abs(tx) > (tz << 2))
			{
				return;
			}

			var spriteDef = this.sprites[thing.Sprite];
			var frameNumber = thing.Frame & 0x7F;
			var spriteFrame = spriteDef.Frames[frameNumber];

			Patch lump;
			bool flip;

			if (spriteFrame.Rotate)
			{
				// Choose a different rotation based on player view.
				var ang = Geometry.PointToAngle(this.viewX, this.viewY, thing.X, thing.Y);
				var rot = (ang.Data - thing.Angle.Data + (uint) (Angle.Ang45.Data / 2) * 9) >> 29;
				lump = spriteFrame.Patches[rot];
				flip = spriteFrame.Flip[rot];
			}
			else
			{
				// Use single rotation for all views.
				lump = spriteFrame.Patches[0];
				flip = spriteFrame.Flip[0];
			}

			// Calculate edges of the shape.
			tx -= Fixed.FromInt(lump.LeftOffset);
			var x1 = (this.centerXFrac + (tx * xScale)).Data >> Fixed.FracBits;

			// Off the right side?
			if (x1 > this.windowWidth)
			{
				return;
			}

			tx += Fixed.FromInt(lump.Width);
			var x2 = ((this.centerXFrac + (tx * xScale)).Data >> Fixed.FracBits) - 1;

			// Off the left side?
			if (x2 < 0)
			{
				return;
			}

			// Store information in a vissprite.
			var vis = this.visSprites[this.visSpriteCount];
			this.visSpriteCount++;

			vis.MobjFlags = thing.Flags;
			vis.Scale = xScale;
			vis.GlobalX = thing.X;
			vis.GlobalY = thing.Y;
			vis.GlobalBottomZ = thing.Z;
			vis.GlobalTopZ = thing.Z + Fixed.FromInt(lump.TopOffset);
			vis.TextureAlt = vis.GlobalTopZ - this.viewZ;
			vis.X1 = x1 < 0 ? 0 : x1;
			vis.X2 = x2 >= this.windowWidth ? this.windowWidth - 1 : x2;

			var invScale = Fixed.One / xScale;

			if (flip)
			{
				vis.StartFrac = new Fixed(Fixed.FromInt(lump.Width).Data - 1);
				vis.InvScale = -invScale;
			}
			else
			{
				vis.StartFrac = Fixed.Zero;
				vis.InvScale = invScale;
			}

			if (vis.X1 > x1)
			{
				vis.StartFrac += vis.InvScale * (vis.X1 - x1);
			}

			vis.Patch = lump;

			if (this.fixedColorMap == 0)
			{
				if ((thing.Frame & 0x8000) == 0)
				{
					vis.ColorMap = spriteLights[Math.Min(xScale.Data >> ThreeDRenderer.scaleLightShift, this.maxScaleLight - 1)];
				}
				else
				{
					vis.ColorMap = this.colorMap.FullBright;
				}
			}
			else
			{
				vis.ColorMap = this.colorMap[this.fixedColorMap];
			}
		}

		private void RenderSprites()
		{
			for (var i = 0; i < this.visSpriteCount - 1; i++)
			{
				for (var j = i + 1; j > 0; j--)
				{
					if (this.visSprites[j - 1].Scale < this.visSprites[j].Scale)
					{
						var temp = this.visSprites[j - 1];
						this.visSprites[j - 1] = this.visSprites[j];
						this.visSprites[j] = temp;
					}
				}
			}

			for (var i = this.visSpriteCount - 1; i >= 0; i--)
			{
				this.DrawSprite(this.visSprites[i]);
			}
		}

		private void DrawSprite(VisSprite sprite)
		{
			for (var x = sprite.X1; x <= sprite.X2; x++)
			{
				this.lowerClip[x] = -2;
				this.upperClip[x] = -2;
			}

			// Scan drawsegs from end to start for obscuring segs.
			// The first drawseg that has a greater scale is the clip seg.
			for (var i = this.visWallRangeCount - 1; i >= 0; i--)
			{
				var wall = this.visWallRanges[i];

				// Determine if the drawseg obscures the sprite.
				if (wall.X1 > sprite.X2 || wall.X2 < sprite.X1 || (wall.Silhouette == 0 && wall.MaskedTextureColumn == -1))
				{
					// Does not cover sprite.
					continue;
				}

				var r1 = wall.X1 < sprite.X1 ? sprite.X1 : wall.X1;
				var r2 = wall.X2 > sprite.X2 ? sprite.X2 : wall.X2;

				Fixed lowScale;
				Fixed scale;

				if (wall.Scale1 > wall.Scale2)
				{
					lowScale = wall.Scale2;
					scale = wall.Scale1;
				}
				else
				{
					lowScale = wall.Scale1;
					scale = wall.Scale2;
				}

				if (scale < sprite.Scale || (lowScale < sprite.Scale && Geometry.PointOnSegSide(sprite.GlobalX, sprite.GlobalY, wall.Seg) == 0))
				{
					// Masked mid texture?
					if (wall.MaskedTextureColumn != -1)
					{
						this.DrawMaskedRange(wall, r1, r2);
					}

					// Seg is behind sprite.
					continue;
				}

				// Clip this piece of the sprite.
				var silhouette = wall.Silhouette;

				if (sprite.GlobalBottomZ >= wall.LowerSilHeight)
				{
					silhouette &= ~Silhouette.Lower;
				}

				if (sprite.GlobalTopZ <= wall.UpperSilHeight)
				{
					silhouette &= ~Silhouette.Upper;
				}

				if (silhouette == Silhouette.Lower)
				{
					// Bottom sil.
					for (var x = r1; x <= r2; x++)
					{
						if (this.lowerClip[x] == -2)
						{
							this.lowerClip[x] = this.clipData[wall.LowerClip + x];
						}
					}
				}
				else if (silhouette == Silhouette.Upper)
				{
					// Top sil.
					for (var x = r1; x <= r2; x++)
					{
						if (this.upperClip[x] == -2)
						{
							this.upperClip[x] = this.clipData[wall.UpperClip + x];
						}
					}
				}
				else if (silhouette == Silhouette.Both)
				{
					// Both.
					for (var x = r1; x <= r2; x++)
					{
						if (this.lowerClip[x] == -2)
						{
							this.lowerClip[x] = this.clipData[wall.LowerClip + x];
						}

						if (this.upperClip[x] == -2)
						{
							this.upperClip[x] = this.clipData[wall.UpperClip + x];
						}
					}
				}
			}

			// All clipping has been performed, so draw the sprite.

			// Check for unclipped columns.
			for (var x = sprite.X1; x <= sprite.X2; x++)
			{
				if (this.lowerClip[x] == -2)
				{
					this.lowerClip[x] = (short) this.windowHeight;
				}

				if (this.upperClip[x] == -2)
				{
					this.upperClip[x] = -1;
				}
			}

			if ((sprite.MobjFlags & MobjFlags.Shadow) != 0)
			{
				var frac = sprite.StartFrac;

				for (var x = sprite.X1; x <= sprite.X2; x++)
				{
					var textureColumn = frac.ToIntFloor();

					this.DrawMaskedFuzzColumn(
						sprite.Patch.Columns[textureColumn],
						x,
						this.centerYFrac - (sprite.TextureAlt * sprite.Scale),
						sprite.Scale,
						this.upperClip[x],
						this.lowerClip[x]
					);

					frac += sprite.InvScale;
				}
			}
			else if (((int) (sprite.MobjFlags & MobjFlags.Translation) >> (int) MobjFlags.TransShift) != 0)
			{
				byte[] translation;

				switch (((int) (sprite.MobjFlags & MobjFlags.Translation) >> (int) MobjFlags.TransShift))
				{
					case 1:
						translation = this.greenToGray;

						break;

					case 2:
						translation = this.greenToBrown;

						break;

					default:
						translation = this.greenToRed;

						break;
				}

				var frac = sprite.StartFrac;

				for (var x = sprite.X1; x <= sprite.X2; x++)
				{
					var textureColumn = frac.ToIntFloor();

					this.DrawMaskedColumnTranslation(
						sprite.Patch.Columns[textureColumn],
						translation,
						sprite.ColorMap,
						x,
						this.centerYFrac - (sprite.TextureAlt * sprite.Scale),
						sprite.Scale,
						Fixed.Abs(sprite.InvScale),
						sprite.TextureAlt,
						this.upperClip[x],
						this.lowerClip[x]
					);

					frac += sprite.InvScale;
				}
			}
			else
			{
				var frac = sprite.StartFrac;

				for (var x = sprite.X1; x <= sprite.X2; x++)
				{
					var textureColumn = frac.ToIntFloor();

					this.DrawMaskedColumn(
						sprite.Patch.Columns[textureColumn],
						sprite.ColorMap,
						x,
						this.centerYFrac - (sprite.TextureAlt * sprite.Scale),
						sprite.Scale,
						Fixed.Abs(sprite.InvScale),
						sprite.TextureAlt,
						this.upperClip[x],
						this.lowerClip[x]
					);

					frac += sprite.InvScale;
				}
			}
		}

		private void DrawPlayerSprite(PlayerSpriteDef psp, byte[][] spriteLights, bool fuzz)
		{
			// Decide which patch to use.
			var spriteDef = this.sprites[psp.State.Sprite];

			var spriteFrame = spriteDef.Frames[psp.State.Frame & 0x7fff];

			var lump = spriteFrame.Patches[0];
			var flip = spriteFrame.Flip[0];

			// Calculate edges of the shape.
			var tx = psp.Sx - Fixed.FromInt(160);
			tx -= Fixed.FromInt(lump.LeftOffset);
			var x1 = (this.centerXFrac + tx * this.weaponScale).Data >> Fixed.FracBits;

			// Off the right side?
			if (x1 > this.windowWidth)
			{
				return;
			}

			tx += Fixed.FromInt(lump.Width);
			var x2 = ((this.centerXFrac + tx * this.weaponScale).Data >> Fixed.FracBits) - 1;

			// Off the left side?
			if (x2 < 0)
			{
				return;
			}

			// Store information in a vissprite.
			var vis = this.weaponSprite;
			vis.MobjFlags = 0;

			// The code below is based on Crispy Doom's weapon rendering code.
			vis.TextureAlt = Fixed.FromInt(100) + Fixed.One / 4 - (psp.Sy - Fixed.FromInt(lump.TopOffset));
			vis.X1 = x1 < 0 ? 0 : x1;
			vis.X2 = x2 >= this.windowWidth ? this.windowWidth - 1 : x2;
			vis.Scale = this.weaponScale;

			if (flip)
			{
				vis.InvScale = -this.weaponInvScale;
				vis.StartFrac = Fixed.FromInt(lump.Width) - new Fixed(1);
			}
			else
			{
				vis.InvScale = this.weaponInvScale;
				vis.StartFrac = Fixed.Zero;
			}

			if (vis.X1 > x1)
			{
				vis.StartFrac += vis.InvScale * (vis.X1 - x1);
			}

			vis.Patch = lump;

			if (this.fixedColorMap == 0)
			{
				if ((psp.State.Frame & 0x8000) == 0)
				{
					vis.ColorMap = spriteLights[this.maxScaleLight - 1];
				}
				else
				{
					vis.ColorMap = this.colorMap.FullBright;
				}
			}
			else
			{
				vis.ColorMap = this.colorMap[this.fixedColorMap];
			}

			if (fuzz)
			{
				var frac = vis.StartFrac;

				for (var x = vis.X1; x <= vis.X2; x++)
				{
					var texturecolumn = frac.Data >> Fixed.FracBits;

					this.DrawMaskedFuzzColumn(
						vis.Patch.Columns[texturecolumn],
						x,
						this.centerYFrac - (vis.TextureAlt * vis.Scale),
						vis.Scale,
						-1,
						this.windowHeight
					);

					frac += vis.InvScale;
				}
			}
			else
			{
				var frac = vis.StartFrac;

				for (var x = vis.X1; x <= vis.X2; x++)
				{
					var texturecolumn = frac.Data >> Fixed.FracBits;

					this.DrawMaskedColumn(
						vis.Patch.Columns[texturecolumn],
						vis.ColorMap,
						x,
						this.centerYFrac - (vis.TextureAlt * vis.Scale),
						vis.Scale,
						Fixed.Abs(vis.InvScale),
						vis.TextureAlt,
						-1,
						this.windowHeight
					);

					frac += vis.InvScale;
				}
			}
		}

		private void DrawPlayerSprites(Player player)
		{
			// Get light level.
			var spriteLightLevel = (player.Mobj.Subsector.Sector.LightLevel >> ThreeDRenderer.lightSegShift) + this.extraLight;

			byte[][] spriteLights;

			if (spriteLightLevel < 0)
			{
				spriteLights = this.scaleLight[0];
			}
			else if (spriteLightLevel >= ThreeDRenderer.lightLevelCount)
			{
				spriteLights = this.scaleLight[ThreeDRenderer.lightLevelCount - 1];
			}
			else
			{
				spriteLights = this.scaleLight[spriteLightLevel];
			}

			bool fuzz;

			if (player.Powers[(int) PowerType.Invisibility] > 4 * 32 || (player.Powers[(int) PowerType.Invisibility] & 8) != 0)
			{
				// Shadow draw.
				fuzz = true;
			}
			else
			{
				fuzz = false;
			}

			// Add all active psprites.
			for (var i = 0; i < (int) PlayerSprite.Count; i++)
			{
				var psp = player.PlayerSprites[i];

				if (psp.State != null)
				{
					this.DrawPlayerSprite(psp, spriteLights, fuzz);
				}
			}
		}

		public int WindowSize
		{
			get
			{
				return this.windowSize;
			}

			set
			{
				this.windowSize = value;
				this.SetWindowSize(this.windowSize);
			}
		}

		private class ClipRange
		{
			public int First;
			public int Last;

			public void CopyFrom(ClipRange range)
			{
				this.First = range.First;
				this.Last = range.Last;
			}
		}

		private class VisWallRange
		{
			public Seg Seg;

			public int X1;
			public int X2;

			public Fixed Scale1;
			public Fixed Scale2;
			public Fixed ScaleStep;

			public Silhouette Silhouette;
			public Fixed UpperSilHeight;
			public Fixed LowerSilHeight;

			public int UpperClip;
			public int LowerClip;
			public int MaskedTextureColumn;
		}

		[Flags]
		private enum Silhouette
		{
			Upper = 1,
			Lower = 2,
			Both = 3
		}

		private class VisSprite
		{
			public int X1;
			public int X2;

			// For line side calculation.
			public Fixed GlobalX;
			public Fixed GlobalY;

			// Global bottom / top for silhouette clipping.
			public Fixed GlobalBottomZ;
			public Fixed GlobalTopZ;

			// Horizontal position of x1.
			public Fixed StartFrac;

			public Fixed Scale;

			// Negative if flipped.
			public Fixed InvScale;

			public Fixed TextureAlt;
			public Patch Patch;

			// For color translation and shadow draw.
			public byte[] ColorMap;

			public MobjFlags MobjFlags;
		}
	}
}
