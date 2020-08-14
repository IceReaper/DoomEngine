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
	using Doom.Intermission;
	using System;
	using System.Collections.Generic;

	public sealed class IntermissionRenderer
	{
		// GLOBAL LOCATIONS
		private static readonly int titleY = 2;
		private static readonly int spacingY = 33;

		// SINGPLE-PLAYER STUFF
		private static readonly int spStatsX = 50;
		private static readonly int spStatsY = 50;
		private static readonly int spTimeX = 16;
		private static readonly int spTimeY = 200 - 32;

		// NET GAME STUFF
		private static readonly int ngStatsY = 50;
		private static readonly int ngSpacingX = 64;

		// DEATHMATCH STUFF
		private static readonly int dmMatrixX = 42;
		private static readonly int dmMatrixY = 68;
		private static readonly int dmSpacingX = 40;
		private static readonly int dmTotalsX = 269;
		private static readonly int dmKillersX = 10;
		private static readonly int dmKillersY = 100;
		private static readonly int dmVictimsX = 5;
		private static readonly int dmVictimsY = 50;

		private static readonly string[] mapPictures = new string[] {"WIMAP0", "WIMAP1", "WIMAP2"};

		private static readonly string[] playerBoxes = new string[] {"STPB0", "STPB1", "STPB2", "STPB3"};

		private static readonly string[] youAreHere = new string[] {"WIURH0", "WIURH1"};

		private static readonly string[][] doomLevels;
		private static readonly string[] doom2Levels;

		static IntermissionRenderer()
		{
			IntermissionRenderer.doomLevels = new string[4][];

			for (var e = 0; e < 4; e++)
			{
				IntermissionRenderer.doomLevels[e] = new string[9];

				for (var m = 0; m < 9; m++)
				{
					IntermissionRenderer.doomLevels[e][m] = "WILV" + e + m;
				}
			}

			IntermissionRenderer.doom2Levels = new string[32];

			for (var m = 0; m < 32; m++)
			{
				IntermissionRenderer.doom2Levels[m] = "CWILV" + m.ToString("00");
			}
		}

		private DrawScreen screen;

		private PatchCache cache;

		private Patch minus;
		private Patch[] numbers;
		private Patch percent;
		private Patch colon;

		private int scale;

		public IntermissionRenderer(DrawScreen screen)
		{
			this.screen = screen;

			this.cache = new PatchCache();

			this.minus = Patch.FromWad("WIMINUS");
			this.numbers = new Patch[10];

			for (var i = 0; i < 10; i++)
			{
				this.numbers[i] = Patch.FromWad("WINUM" + i);
			}

			this.percent = Patch.FromWad("WIPCNT");
			this.colon = Patch.FromWad("WICOLON");

			this.scale = screen.Width / 320;
		}

		private void DrawPatch(Patch patch, int x, int y)
		{
			this.screen.DrawPatch(patch, this.scale * x, this.scale * y, this.scale);
		}

		private void DrawPatch(string name, int x, int y)
		{
			var scale = this.screen.Width / 320;
			this.screen.DrawPatch(this.cache[name], scale * x, scale * y, scale);
		}

		private int GetWidth(string name)
		{
			return this.cache.GetWidth(name);
		}

		private int GetHeight(string name)
		{
			return this.cache.GetHeight(name);
		}

		public void Render(Intermission im)
		{
			switch (im.State)
			{
				case IntermissionState.StatCount:
					if (im.Options.Deathmatch != 0)
					{
						this.DrawDeathmatchStats(im);
					}
					else if (im.Options.NetGame)
					{
						this.DrawNetGameStats(im);
					}
					else
					{
						this.DrawSinglePlayerStats(im);
					}

					break;

				case IntermissionState.ShowNextLoc:
					this.DrawShowNextLoc(im);

					break;

				case IntermissionState.NoState:
					this.DrawNoState(im);

					break;
			}
		}

		private void DrawBackground(Intermission im)
		{
			if (DoomApplication.Instance.IWad == "doom2" || DoomApplication.Instance.IWad == "plutonia" || DoomApplication.Instance.IWad == "tnt")
			{
				this.DrawPatch("INTERPIC", 0, 0);
			}
			else
			{
				var e = im.Options.Episode - 1;

				if (e < IntermissionRenderer.mapPictures.Length)
				{
					this.DrawPatch(IntermissionRenderer.mapPictures[e], 0, 0);
				}
				else
				{
					this.DrawPatch("INTERPIC", 0, 0);
				}
			}
		}

		private void DrawSinglePlayerStats(Intermission im)
		{
			this.DrawBackground(im);

			// Draw animated background.
			this.DrawBackgroundAnimation(im);

			// Draw level name.
			this.DrawFinishedLevelName(im);

			// Line height.
			var lineHeight = (3 * this.numbers[0].Height) / 2;

			this.DrawPatch(
				"WIOSTK", // KILLS
				IntermissionRenderer.spStatsX,
				IntermissionRenderer.spStatsY
			);

			this.DrawPercent(320 - IntermissionRenderer.spStatsX, IntermissionRenderer.spStatsY, im.KillCount[0]);

			this.DrawPatch(
				"WIOSTI", // ITEMS
				IntermissionRenderer.spStatsX,
				IntermissionRenderer.spStatsY + lineHeight
			);

			this.DrawPercent(320 - IntermissionRenderer.spStatsX, IntermissionRenderer.spStatsY + lineHeight, im.ItemCount[0]);

			this.DrawPatch(
				"WISCRT2", // SECRET
				IntermissionRenderer.spStatsX,
				IntermissionRenderer.spStatsY + 2 * lineHeight
			);

			this.DrawPercent(320 - IntermissionRenderer.spStatsX, IntermissionRenderer.spStatsY + 2 * lineHeight, im.SecretCount[0]);

			this.DrawPatch(
				"WITIME", // TIME
				IntermissionRenderer.spTimeX,
				IntermissionRenderer.spTimeY
			);

			this.DrawTime(320 / 2 - IntermissionRenderer.spTimeX, IntermissionRenderer.spTimeY, im.TimeCount);

			if (im.Info.Episode < 3)
			{
				this.DrawPatch(
					"WIPAR", // PAR
					320 / 2 + IntermissionRenderer.spTimeX,
					IntermissionRenderer.spTimeY
				);

				this.DrawTime(320 - IntermissionRenderer.spTimeX, IntermissionRenderer.spTimeY, im.ParCount);
			}
		}

		private void DrawNetGameStats(Intermission im)
		{
			this.DrawBackground(im);

			// Draw animated background.
			this.DrawBackgroundAnimation(im);

			// Draw level name.
			this.DrawFinishedLevelName(im);

			var ngStatsX = 32 + this.GetWidth("STFST01") / 2;

			if (!im.DoFrags)
			{
				ngStatsX += 32;
			}

			// Draw stat titles (top line).
			this.DrawPatch(
				"WIOSTK", // KILLS
				ngStatsX + IntermissionRenderer.ngSpacingX - this.GetWidth("WIOSTK"),
				IntermissionRenderer.ngStatsY
			);

			this.DrawPatch(
				"WIOSTI", // ITEMS
				ngStatsX + 2 * IntermissionRenderer.ngSpacingX - this.GetWidth("WIOSTI"),
				IntermissionRenderer.ngStatsY
			);

			this.DrawPatch(
				"WIOSTS", // SCRT
				ngStatsX + 3 * IntermissionRenderer.ngSpacingX - this.GetWidth("WIOSTS"),
				IntermissionRenderer.ngStatsY
			);

			if (im.DoFrags)
			{
				this.DrawPatch(
					"WIFRGS", // FRAGS
					ngStatsX + 4 * IntermissionRenderer.ngSpacingX - this.GetWidth("WIFRGS"),
					IntermissionRenderer.ngStatsY
				);
			}

			// Draw stats.
			var y = IntermissionRenderer.ngStatsY + this.GetHeight("WIOSTK");

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (!im.Options.Players[i].InGame)
				{
					continue;
				}

				var x = ngStatsX;

				this.DrawPatch(IntermissionRenderer.playerBoxes[i], x - this.GetWidth(IntermissionRenderer.playerBoxes[i]), y);

				if (i == im.Options.ConsolePlayer)
				{
					this.DrawPatch(
						"STFST01", // Player face
						x - this.GetWidth(IntermissionRenderer.playerBoxes[i]),
						y
					);
				}

				x += IntermissionRenderer.ngSpacingX;

				this.DrawPercent(x - this.percent.Width, y + 10, im.KillCount[i]);
				x += IntermissionRenderer.ngSpacingX;

				this.DrawPercent(x - this.percent.Width, y + 10, im.ItemCount[i]);
				x += IntermissionRenderer.ngSpacingX;

				this.DrawPercent(x - this.percent.Width, y + 10, im.SecretCount[i]);
				x += IntermissionRenderer.ngSpacingX;

				if (im.DoFrags)
				{
					this.DrawNumber(x, y + 10, im.FragCount[i], -1);
				}

				y += IntermissionRenderer.spacingY;
			}
		}

		private void DrawDeathmatchStats(Intermission im)
		{
			this.DrawBackground(im);

			// Draw animated background.
			this.DrawBackgroundAnimation(im);

			// Draw level name.
			this.DrawFinishedLevelName(im);

			// Draw stat titles (top line).
			this.DrawPatch(
				"WIMSTT", // TOTAL
				IntermissionRenderer.dmTotalsX - this.GetWidth("WIMSTT") / 2,
				IntermissionRenderer.dmMatrixY - IntermissionRenderer.spacingY + 10
			);

			this.DrawPatch(
				"WIKILRS", // KILLERS
				IntermissionRenderer.dmKillersX,
				IntermissionRenderer.dmKillersY
			);

			this.DrawPatch(
				"WIVCTMS", // VICTIMS
				IntermissionRenderer.dmVictimsX,
				IntermissionRenderer.dmVictimsY
			);

			// Draw player boxes.
			var x = IntermissionRenderer.dmMatrixX + IntermissionRenderer.dmSpacingX;
			var y = IntermissionRenderer.dmMatrixY;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				if (im.Options.Players[i].InGame)
				{
					this.DrawPatch(
						IntermissionRenderer.playerBoxes[i],
						x - this.GetWidth(IntermissionRenderer.playerBoxes[i]) / 2,
						IntermissionRenderer.dmMatrixY - IntermissionRenderer.spacingY
					);

					this.DrawPatch(
						IntermissionRenderer.playerBoxes[i],
						IntermissionRenderer.dmMatrixX - this.GetWidth(IntermissionRenderer.playerBoxes[i]) / 2,
						y
					);

					if (i == im.Options.ConsolePlayer)
					{
						this.DrawPatch(
							"STFDEAD0", // Player face (dead)
							x - this.GetWidth(IntermissionRenderer.playerBoxes[i]) / 2,
							IntermissionRenderer.dmMatrixY - IntermissionRenderer.spacingY
						);

						this.DrawPatch(
							"STFST01", // Player face
							IntermissionRenderer.dmMatrixX - this.GetWidth(IntermissionRenderer.playerBoxes[i]) / 2,
							y
						);
					}
				}
				else
				{
					// V_DrawPatch(x-SHORT(bp[i]->width)/2,
					//   DM_MATRIXY - WI_SPACINGY, FB, bp[i]);
					// V_DrawPatch(DM_MATRIXX-SHORT(bp[i]->width)/2,
					//   y, FB, bp[i]);
				}

				x += IntermissionRenderer.dmSpacingX;
				y += IntermissionRenderer.spacingY;
			}

			// Draw stats.
			y = IntermissionRenderer.dmMatrixY + 10;
			var w = this.numbers[0].Width;

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				x = IntermissionRenderer.dmMatrixX + IntermissionRenderer.dmSpacingX;

				if (im.Options.Players[i].InGame)
				{
					for (var j = 0; j < Player.MaxPlayerCount; j++)
					{
						if (im.Options.Players[j].InGame)
						{
							this.DrawNumber(x + w, y, im.DeathmatchFrags[i][j], 2);
						}

						x += IntermissionRenderer.dmSpacingX;
					}

					this.DrawNumber(IntermissionRenderer.dmTotalsX + w, y, im.DeathmatchTotals[i], 2);
				}

				y += IntermissionRenderer.spacingY;
			}
		}

		private void DrawNoState(Intermission im)
		{
			this.DrawShowNextLoc(im);
		}

		private void DrawShowNextLoc(Intermission im)
		{
			this.DrawBackground(im);

			// Draw animated background.
			this.DrawBackgroundAnimation(im);

			if (DoomApplication.Instance.IWad != "doom2" && DoomApplication.Instance.IWad != "plutonia" && DoomApplication.Instance.IWad != "tnt")
			{
				if (im.Info.Episode > 2)
				{
					this.DrawEnteringLevelName(im);

					return;
				}

				var last = (im.Info.LastLevel == 8) ? im.Info.NextLevel - 1 : im.Info.LastLevel;

				// Draw a splat on taken cities.
				for (var i = 0; i <= last; i++)
				{
					var x = WorldMap.Locations[im.Info.Episode][i].X;
					var y = WorldMap.Locations[im.Info.Episode][i].Y;
					this.DrawPatch("WISPLAT", x, y);
				}

				// Splat the secret level?
				if (im.Info.DidSecret)
				{
					var x = WorldMap.Locations[im.Info.Episode][8].X;
					var y = WorldMap.Locations[im.Info.Episode][8].Y;
					this.DrawPatch("WISPLAT", x, y);
				}

				// Draw "you are here".
				if (im.ShowYouAreHere)
				{
					var x = WorldMap.Locations[im.Info.Episode][im.Info.NextLevel].X;
					var y = WorldMap.Locations[im.Info.Episode][im.Info.NextLevel].Y;
					this.DrawSuitablePatch(IntermissionRenderer.youAreHere, x, y);
				}
			}

			// Draw next level name.
			if ((DoomApplication.Instance.IWad != "doom2" && DoomApplication.Instance.IWad != "plutonia" && DoomApplication.Instance.IWad != "tnt")
				|| im.Info.NextLevel != 30)
			{
				this.DrawEnteringLevelName(im);
			}
		}

		private void DrawFinishedLevelName(Intermission intermission)
		{
			var wbs = intermission.Info;
			var y = IntermissionRenderer.titleY;

			string levelName;

			if (DoomApplication.Instance.IWad != "doom2" && DoomApplication.Instance.IWad != "plutonia" && DoomApplication.Instance.IWad != "tnt")
			{
				var e = intermission.Options.Episode - 1;
				levelName = IntermissionRenderer.doomLevels[e][wbs.LastLevel];
			}
			else
			{
				levelName = IntermissionRenderer.doom2Levels[wbs.LastLevel];
			}

			// Draw level name. 
			this.DrawPatch(levelName, (320 - this.GetWidth(levelName)) / 2, y);

			// Draw "Finished!".
			y += (5 * this.GetHeight(levelName)) / 4;

			this.DrawPatch("WIF", (320 - this.GetWidth("WIF")) / 2, y);
		}

		private void DrawEnteringLevelName(Intermission im)
		{
			var wbs = im.Info;
			int y = IntermissionRenderer.titleY;

			string levelName;

			if (DoomApplication.Instance.IWad != "doom2" && DoomApplication.Instance.IWad != "plutonia" && DoomApplication.Instance.IWad != "tnt")
			{
				var e = im.Options.Episode - 1;
				levelName = IntermissionRenderer.doomLevels[e][wbs.NextLevel];
			}
			else
			{
				levelName = IntermissionRenderer.doom2Levels[wbs.NextLevel];
			}

			// Draw "Entering".
			this.DrawPatch("WIENTER", (320 - this.GetWidth("WIENTER")) / 2, y);

			// Draw level name.
			y += (5 * this.GetHeight(levelName)) / 4;

			this.DrawPatch(levelName, (320 - this.GetWidth(levelName)) / 2, y);
		}

		private int DrawNumber(int x, int y, int n, int digits)
		{
			if (digits < 0)
			{
				if (n == 0)
				{
					// Make variable-length zeros 1 digit long.
					digits = 1;
				}
				else
				{
					// Figure out number of digits.
					digits = 0;
					var temp = n;

					while (temp != 0)
					{
						temp /= 10;
						digits++;
					}
				}
			}

			var neg = n < 0;

			if (neg)
			{
				n = -n;
			}

			// If non-number, do not draw it.
			if (n == 1994)
			{
				return 0;
			}

			var fontWidth = this.numbers[0].Width;

			// Draw the new number.
			while (digits-- != 0)
			{
				x -= fontWidth;
				this.DrawPatch(this.numbers[n % 10], x, y);
				n /= 10;
			}

			// Draw a minus sign if necessary.
			if (neg)
			{
				this.DrawPatch(this.minus, x -= 8, y);
			}

			return x;
		}

		private void DrawPercent(int x, int y, int p)
		{
			if (p < 0)
			{
				return;
			}

			this.DrawPatch(this.percent, x, y);
			this.DrawNumber(x, y, p, -1);
		}

		private void DrawTime(int x, int y, int t)
		{
			if (t < 0)
			{
				return;
			}

			if (t <= 61 * 59)
			{
				var div = 1;

				do
				{
					var n = (t / div) % 60;
					x = this.DrawNumber(x, y, n, 2) - this.colon.Width;
					div *= 60;

					// Draw.
					if (div == 60 || t / div != 0)
					{
						this.DrawPatch(this.colon, x, y);
					}
				}
				while (t / div != 0);
			}
			else
			{
				this.DrawPatch(
					"WISUCKS", // SUCKS
					x - this.GetWidth("WISUCKS"),
					y
				);
			}
		}

		private void DrawBackgroundAnimation(Intermission im)
		{
			if (DoomApplication.Instance.IWad == "doom2" || DoomApplication.Instance.IWad == "plutonia" || DoomApplication.Instance.IWad == "tnt")
			{
				return;
			}

			if (im.Info.Episode > 2)
			{
				return;
			}

			for (var i = 0; i < im.Animations.Length; i++)
			{
				var a = im.Animations[i];

				if (a.PatchNumber >= 0)
				{
					this.DrawPatch(a.Patches[a.PatchNumber], a.LocationX, a.LocationY);
				}
			}
		}

		private void DrawSuitablePatch(IReadOnlyList<string> candidates, int x, int y)
		{
			var fits = false;
			var i = 0;

			do
			{
				var patch = this.cache[candidates[i]];

				var left = x - patch.LeftOffset;
				var top = y - patch.TopOffset;
				var right = left + patch.Width;
				var bottom = top + patch.Height;

				if (left >= 0 && right < 320 && top >= 0 && bottom < 320)
				{
					fits = true;
				}
				else
				{
					i++;
				}
			}
			while (!fits && i != 2);

			if (fits && i < 2)
			{
				this.DrawPatch(candidates[i], x, y);
			}
			else
			{
				throw new Exception("Could not place patch!");
			}
		}
	}
}
