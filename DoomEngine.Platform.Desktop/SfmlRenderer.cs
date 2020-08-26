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

namespace DoomEngine.Platform.Desktop
{
	using Doom.Common;
	using Doom.Game;
	using Doom.Graphics;
	using Doom.Info;
	using Doom.World;
	using SFML.Graphics;
	using SFML.System;
	using SoftwareRendering;
	using System;
	using System.Runtime.ExceptionServices;
	using System.Runtime.InteropServices;
	using Sprite = SFML.Graphics.Sprite;
	using Texture = SFML.Graphics.Texture;

	public sealed class SfmlRenderer : IRenderer
	{
		private static double[] gammaCorrectionParameters = new double[] {1.00, 0.95, 0.90, 0.85, 0.80, 0.75, 0.70, 0.65, 0.60, 0.55, 0.50};

		private Config config;

		private RenderWindow sfmlWindow;
		private Palette palette;

		private int sfmlWindowWidth;
		private int sfmlWindowHeight;

		private DrawScreen screen;

		private int sfmlTextureWidth;
		private int sfmlTextureHeight;

		private byte[] sfmlTextureData;
		private Texture sfmlTexture;
		private Sprite sfmlSprite;
		private RenderStates sfmlStates;

		private MenuRenderer menu;
		private ThreeDRenderer threeD;
		private StatusBarRenderer statusBar;
		private IntermissionRenderer intermission;
		private OpeningSequenceRenderer openingSequence;
		private AutoMapRenderer autoMap;
		private FinaleRenderer finale;

		private Patch pause;

		private int wipeBandWidth;
		private int wipeBandCount;
		private int wipeHeight;
		private byte[] wipeBuffer;

		public SfmlRenderer(Config config, RenderWindow window, CommonResource resource)
		{
			try
			{
				Console.Write("Initialize renderer: ");

				this.config = config;

				config.video_gamescreensize = Math.Clamp(config.video_gamescreensize, 0, this.MaxWindowSize);
				config.video_gammacorrection = Math.Clamp(config.video_gammacorrection, 0, this.MaxGammaCorrectionLevel);

				this.sfmlWindow = window;
				this.palette = resource.Palette;

				this.sfmlWindowWidth = (int) window.Size.X;
				this.sfmlWindowHeight = (int) window.Size.Y;

				if (config.video_highresolution)
				{
					this.screen = new DrawScreen(640, 400);
					this.sfmlTextureWidth = 512;
					this.sfmlTextureHeight = 1024;
				}
				else
				{
					this.screen = new DrawScreen(320, 200);
					this.sfmlTextureWidth = 256;
					this.sfmlTextureHeight = 512;
				}

				this.sfmlTextureData = new byte[4 * this.screen.Width * this.screen.Height];

				this.sfmlTexture = new Texture((uint) this.sfmlTextureWidth, (uint) this.sfmlTextureHeight);
				this.sfmlSprite = new Sprite(this.sfmlTexture);

				this.sfmlSprite.Position = new Vector2f(0, 0);
				this.sfmlSprite.Rotation = 90;
				var scaleX = (float) this.sfmlWindowWidth / this.screen.Width;
				var scaleY = (float) this.sfmlWindowHeight / this.screen.Height;
				this.sfmlSprite.Scale = new Vector2f(scaleY, -scaleX);

				this.sfmlStates = new RenderStates(BlendMode.None);

				this.menu = new MenuRenderer(this.screen);
				this.threeD = new ThreeDRenderer(resource, this.screen, config.video_gamescreensize);
				this.statusBar = new StatusBarRenderer(this.screen);
				this.intermission = new IntermissionRenderer(this.screen);
				this.openingSequence = new OpeningSequenceRenderer(this.screen, this);
				this.autoMap = new AutoMapRenderer(this.screen);
				this.finale = new FinaleRenderer(resource, this.screen);

				this.pause = Patch.FromWad("M_PAUSE");

				var scale = this.screen.Width / 320;
				this.wipeBandWidth = 2 * scale;
				this.wipeBandCount = this.screen.Width / this.wipeBandWidth + 1;
				this.wipeHeight = this.screen.Height / scale;
				this.wipeBuffer = new byte[this.screen.Data.Length];

				this.palette.ResetColors(SfmlRenderer.gammaCorrectionParameters[config.video_gammacorrection]);

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				this.Dispose();
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public void RenderApplication(DoomApplication app)
		{
			if (app.State == ApplicationState.Opening)
			{
				this.openingSequence.Render(app.Opening);
			}
			else if (app.State == ApplicationState.Game)
			{
				this.RenderGame(app.Game);
			}

			if (!app.Menu.Active)
			{
				if (app.State == ApplicationState.Game && app.Game.State == GameState.Level && app.Game.Paused)
				{
					var scale = this.screen.Width / 320;
					this.screen.DrawPatch(this.pause, (this.screen.Width - scale * this.pause.Width) / 2, 4 * scale, scale);
				}
			}
		}

		public void RenderMenu(DoomApplication app)
		{
			if (app.Menu.Active)
			{
				this.menu.Render(app.Menu);
			}
		}

		public void RenderGame(DoomGame game)
		{
			if (game.State == GameState.Level)
			{
				var consolePlayer = game.World.Options.Player;
				var displayPlayer = game.World.Options.Player;

				if (game.World.AutoMap.Visible)
				{
					this.autoMap.Render(consolePlayer);
					this.statusBar.Render(consolePlayer, true);
				}
				else
				{
					this.threeD.Render(displayPlayer);

					if (this.threeD.WindowSize < 8)
					{
						this.statusBar.Render(consolePlayer, true);
					}
					else if (this.threeD.WindowSize == ThreeDRenderer.MaxScreenSize)
					{
						this.statusBar.Render(consolePlayer, false);
					}
				}

				if (this.config.video_displaymessage || object.ReferenceEquals(consolePlayer.Message, (string) DoomInfo.Strings.MSGOFF))
				{
					if (consolePlayer.MessageTime > 0)
					{
						var scale = this.screen.Width / 320;
						this.screen.DrawText(consolePlayer.Message, 0, 7 * scale, scale);
					}
				}
			}
			else if (game.State == GameState.Intermission)
			{
				this.intermission.Render(game.Intermission);
			}
			else if (game.State == GameState.Finale)
			{
				this.finale.Render(game.Finale);
			}
		}

		public void Render(DoomApplication app)
		{
			this.RenderApplication(app);
			this.RenderMenu(app);

			var colors = this.palette[0];

			if (app.State == ApplicationState.Game && app.Game.State == GameState.Level)
			{
				colors = this.palette[SfmlRenderer.GetPaletteNumber(app.Game.World.Options.Player)];
			}

			this.Display(colors);
		}

		public void RenderWipe(DoomApplication app, WipeEffect wipe)
		{
			this.RenderApplication(app);

			var scale = this.screen.Width / 320;

			for (var i = 0; i < this.wipeBandCount - 1; i++)
			{
				var x1 = this.wipeBandWidth * i;
				var x2 = x1 + this.wipeBandWidth;
				var y1 = Math.Max(scale * wipe.Y[i], 0);
				var y2 = Math.Max(scale * wipe.Y[i + 1], 0);
				var dy = (float) (y2 - y1) / this.wipeBandWidth;

				for (var x = x1; x < x2; x++)
				{
					var y = (int) MathF.Round(y1 + dy * ((x - x1) / 2 * 2));
					var copyLength = this.screen.Height - y;

					if (copyLength > 0)
					{
						var srcPos = this.screen.Height * x;
						var dstPos = this.screen.Height * x + y;
						Array.Copy(this.wipeBuffer, srcPos, this.screen.Data, dstPos, copyLength);
					}
				}
			}

			this.RenderMenu(app);

			this.Display(this.palette[0]);
		}

		public void InitializeWipe()
		{
			Array.Copy(this.screen.Data, this.wipeBuffer, this.screen.Data.Length);
		}

		private void Display(uint[] colors)
		{
			var screenData = this.screen.Data;
			var p = MemoryMarshal.Cast<byte, uint>(this.sfmlTextureData);

			for (var i = 0; i < p.Length; i++)
			{
				p[i] = colors[screenData[i]];
			}

			this.sfmlTexture.Update(this.sfmlTextureData, (uint) this.screen.Height, (uint) this.screen.Width, 0, 0);
			this.sfmlWindow.Draw(this.sfmlSprite, this.sfmlStates);
			this.sfmlWindow.Display();
		}

		private static int GetPaletteNumber(Player player)
		{
			var count = player.DamageCount;

			if (player.Powers[(int) PowerType.Strength] != 0)
			{
				// Slowly fade the berzerk out.
				var bzc = 12 - (player.Powers[(int) PowerType.Strength] >> 6);

				if (bzc > count)
				{
					count = bzc;
				}
			}

			int palette;

			if (count != 0)
			{
				palette = (count + 7) >> 3;

				if (palette >= Palette.DamageCount)
				{
					palette = Palette.DamageCount - 1;
				}

				palette += Palette.DamageStart;
			}
			else if (player.BonusCount != 0)
			{
				palette = (player.BonusCount + 7) >> 3;

				if (palette >= Palette.BonusCount)
				{
					palette = Palette.BonusCount - 1;
				}

				palette += Palette.BonusStart;
			}
			else if (player.Powers[(int) PowerType.IronFeet] > 4 * 32 || (player.Powers[(int) PowerType.IronFeet] & 8) != 0)
			{
				palette = Palette.IronFeet;
			}
			else
			{
				palette = 0;
			}

			return palette;
		}

		public void Dispose()
		{
			Console.WriteLine("Shutdown renderer.");

			if (this.sfmlSprite != null)
			{
				this.sfmlSprite.Dispose();
				this.sfmlSprite = null;
			}

			if (this.sfmlTexture != null)
			{
				this.sfmlTexture.Dispose();
				this.sfmlTexture = null;
			}
		}

		public int WipeBandCount => this.wipeBandCount;
		public int WipeHeight => this.wipeHeight;

		public int MaxWindowSize
		{
			get
			{
				return ThreeDRenderer.MaxScreenSize;
			}
		}

		public int WindowSize
		{
			get
			{
				return this.threeD.WindowSize;
			}

			set
			{
				this.config.video_gamescreensize = value;
				this.threeD.WindowSize = value;
			}
		}

		public bool DisplayMessage
		{
			get
			{
				return this.config.video_displaymessage;
			}

			set
			{
				this.config.video_displaymessage = value;
			}
		}

		public int MaxGammaCorrectionLevel
		{
			get
			{
				return SfmlRenderer.gammaCorrectionParameters.Length - 1;
			}
		}

		public int GammaCorrectionLevel
		{
			get
			{
				return this.config.video_gammacorrection;
			}

			set
			{
				this.config.video_gammacorrection = value;
				this.palette.ResetColors(SfmlRenderer.gammaCorrectionParameters[this.config.video_gammacorrection]);
			}
		}
	}
}
