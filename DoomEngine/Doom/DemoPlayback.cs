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

namespace DoomEngine.Doom
{
	using Common;
	using Event;
	using Game;
	using System;
	using System.Diagnostics;

	public sealed class DemoPlayback
	{
		private Demo demo;
		private TicCmd[] cmds;
		private DoomGame game;

		private Stopwatch stopwatch;
		private int frameCount;

		public DemoPlayback(CommonResource resource, GameOptions options, string demoName)
		{
			if (DoomApplication.Instance.FileSystem.Exists(demoName))
			{
				this.demo = new Demo(DoomApplication.Instance.FileSystem.Read(demoName));
			}
			else if (DoomApplication.Instance.FileSystem.Exists(demoName + ".lmp"))
			{
				this.demo = new Demo(DoomApplication.Instance.FileSystem.Read(demoName + ".lmp"));
			}
			else
			{
				var lumpName = demoName.ToUpper();

				if (!DoomApplication.Instance.FileSystem.Exists(lumpName))
				{
					throw new Exception("Demo '" + demoName + "' was not found!");
				}

				this.demo = new Demo(DoomApplication.Instance.FileSystem.Read(lumpName));
			}

			this.demo.Options.Renderer = options.Renderer;
			this.demo.Options.Sound = options.Sound;
			this.demo.Options.Music = options.Music;

			this.cmds = new TicCmd[Player.MaxPlayerCount];

			for (var i = 0; i < Player.MaxPlayerCount; i++)
			{
				this.cmds[i] = new TicCmd();
			}

			this.game = new DoomGame(resource, this.demo.Options);
			this.game.DeferedInitNew();

			this.stopwatch = new Stopwatch();
		}

		public UpdateResult Update()
		{
			if (!this.stopwatch.IsRunning)
			{
				this.stopwatch.Start();
			}

			if (!this.demo.ReadCmd(this.cmds))
			{
				this.stopwatch.Stop();

				return UpdateResult.Completed;
			}
			else
			{
				this.frameCount++;

				return this.game.Update(this.cmds);
			}
		}

		public void DoEvent(DoomEvent e)
		{
			this.game.DoEvent(e);
		}

		public DoomGame Game => this.game;
		public double Fps => this.frameCount / this.stopwatch.Elapsed.TotalSeconds;
	}
}
