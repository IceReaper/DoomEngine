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
	using Audio;
	using Game;

	public sealed class OpeningSequence
	{
		private readonly GameOptions options;

		private int currentStage;
		private int nextStage;

		private int count;
		private int timer;

		private bool reset;

		public OpeningSequenceState State { get; private set; }

		public OpeningSequence(GameOptions options)
		{
			this.options = options;

			this.currentStage = 0;
			this.nextStage = 0;
			this.reset = false;
			this.StartTitleScreen();
		}

		public void Reset()
		{
			this.currentStage = 0;
			this.nextStage = 0;
			this.reset = true;
			this.StartTitleScreen();
		}

		public UpdateResult Update()
		{
			var updateResult = UpdateResult.None;

			if (this.nextStage != this.currentStage)
			{
				if (this.nextStage == 0)
					this.StartTitleScreen();
				else if (this.nextStage == 1)
					this.StartCreditScreen();

				this.currentStage = this.nextStage;
				updateResult = UpdateResult.NeedWipe;
			}

			this.count++;

			if (this.count == this.timer)
				this.nextStage = (this.nextStage + 1) % 2;

			if (this.State == OpeningSequenceState.Title && this.count == 1)
			{
				this.options.Music.StartMusic(
					DoomApplication.Instance.IWad == "doom2"
					|| DoomApplication.Instance.IWad == "freedoom2"
					|| DoomApplication.Instance.IWad == "plutonia"
					|| DoomApplication.Instance.IWad == "tnt"
						? Bgm.DM2TTL
						: Bgm.INTRO,
					false
				);
			}

			if (!this.reset)
				return updateResult;

			this.reset = false;

			return UpdateResult.NeedWipe;
		}

		private void StartTitleScreen()
		{
			this.State = OpeningSequenceState.Title;
			this.count = 0;

			this.timer = DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt"
					? 385
					: 170;
		}

		private void StartCreditScreen()
		{
			this.State = OpeningSequenceState.Credit;
			this.count = 0;
			this.timer = 200;
		}
	}
}
