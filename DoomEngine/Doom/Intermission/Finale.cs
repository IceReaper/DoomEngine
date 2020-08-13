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

namespace DoomEngine.Doom.Intermission
{
	using Audio;
	using Event;
	using Game;
	using Info;
	using World;

	public sealed class Finale
	{
		public static readonly int TextSpeed = 3;
		public static readonly int TextWait = 250;

		private GameOptions options;

		// Stage of animation:
		// 0 = text, 1 = art screen, 2 = character cast.
		private int stage;
		private int count;

		private string flat;
		private string text;

		// For bunny scroll.
		private int scrolled;
		private bool showTheEnd;
		private int theEndIndex;

		private UpdateResult updateResult;

		public Finale(GameOptions options)
		{
			this.options = options;

			string c1Text;
			string c2Text;
			string c3Text;
			string c4Text;
			string c5Text;
			string c6Text;
			switch (options.MissionPack)
			{
				case MissionPack.Plutonia:
					c1Text = DoomInfo.Strings.P1TEXT;
					c2Text = DoomInfo.Strings.P2TEXT;
					c3Text = DoomInfo.Strings.P3TEXT;
					c4Text = DoomInfo.Strings.P4TEXT;
					c5Text = DoomInfo.Strings.P5TEXT;
					c6Text = DoomInfo.Strings.P6TEXT;
					break;

				case MissionPack.Tnt:
					c1Text = DoomInfo.Strings.T1TEXT;
					c2Text = DoomInfo.Strings.T2TEXT;
					c3Text = DoomInfo.Strings.T3TEXT;
					c4Text = DoomInfo.Strings.T4TEXT;
					c5Text = DoomInfo.Strings.T5TEXT;
					c6Text = DoomInfo.Strings.T6TEXT;
					break;

				default:
					c1Text = DoomInfo.Strings.C1TEXT;
					c2Text = DoomInfo.Strings.C2TEXT;
					c3Text = DoomInfo.Strings.C3TEXT;
					c4Text = DoomInfo.Strings.C4TEXT;
					c5Text = DoomInfo.Strings.C5TEXT;
					c6Text = DoomInfo.Strings.C6TEXT;
					break;
			}

			switch (options.GameMode)
			{
				case GameMode.Shareware:
				case GameMode.Registered:
				case GameMode.Retail:
					options.Music.StartMusic(Bgm.VICTOR, true);
					switch (options.Episode)
					{
						case 1:
							this.flat = "FLOOR4_8";
							this.text = DoomInfo.Strings.E1TEXT;
							break;

						case 2:
							this.flat = "SFLR6_1";
							this.text = DoomInfo.Strings.E2TEXT;
							break;

						case 3:
							this.flat = "MFLR8_4";
							this.text = DoomInfo.Strings.E3TEXT;
							break;

						case 4:
							this.flat = "MFLR8_3";
							this.text = DoomInfo.Strings.E4TEXT;
							break;

						default:
							break;
					}
					break;

				case GameMode.Commercial:
					options.Music.StartMusic(Bgm.READ_M, true);
					switch (options.Map)
					{
						case 6:
							this.flat = "SLIME16";
							this.text = c1Text;
							break;

						case 11:
							this.flat = "RROCK14";
							this.text = c2Text;
							break;

						case 20:
							this.flat = "RROCK07";
							this.text = c3Text;
							break;

						case 30:
							this.flat = "RROCK17";
							this.text = c4Text;
							break;

						case 15:
							this.flat = "RROCK13";
							this.text = c5Text;
							break;

						case 31:
							this.flat = "RROCK19";
							this.text = c6Text;
							break;

						default:
							break;
					}
					break;

				default:
					options.Music.StartMusic(Bgm.READ_M, true);
					this.flat = "F_SKY1";
					this.text = DoomInfo.Strings.C1TEXT;
					break;
			}

			this.stage = 0;
			this.count = 0;

			this.scrolled = 0;
			this.showTheEnd = false;
			this.theEndIndex = 0;
		}

		public UpdateResult Update()
		{
			this.updateResult = UpdateResult.None;

			// Check for skipping.
			if (this.options.GameMode == GameMode.Commercial && this.count > 50)
			{
				int i;

				// Go on to the next level.
				for (i = 0; i < Player.MaxPlayerCount; i++)
				{
					if (this.options.Players[i].Cmd.Buttons != 0)
					{
						break;
					}
				}

				if (i < Player.MaxPlayerCount && this.stage != 2)
				{
					if (this.options.Map == 30)
					{
						this.StartCast();
					}
					else
					{
						return UpdateResult.Completed;
					}
				}
			}

			// Advance animation.
			this.count++;

			if (this.stage == 2)
			{
				this.UpdateCast();
				return this.updateResult;
			}

			if (this.options.GameMode == GameMode.Commercial)
			{
				return this.updateResult;
			}

			if (this.stage == 0 && this.count > this.text.Length * Finale.TextSpeed + Finale.TextWait)
			{
				this.count = 0;
				this.stage = 1;
				this.updateResult = UpdateResult.NeedWipe;
				if (this.options.Episode == 3)
				{
					this.options.Music.StartMusic(Bgm.BUNNY, true);
				}
			}

			if (this.stage == 1 && this.options.Episode == 3)
			{
				this.BunnyScroll();
			}

			return this.updateResult;
		}

		private void BunnyScroll()
		{
			this.scrolled = 320 - (this.count - 230) / 2;
			if (this.scrolled > 320)
			{
				this.scrolled = 320;
			}
			if (this.scrolled < 0)
			{
				this.scrolled = 0;
			}

			if (this.count < 1130)
			{
				return;
			}

			this.showTheEnd = true;

			if (this.count < 1180)
			{
				this.theEndIndex = 0;
				return;
			}

			var stage = (this.count - 1180) / 5;
			if (stage > 6)
			{
				stage = 6;
			}
			if (stage > this.theEndIndex)
			{
				this.StartSound(Sfx.PISTOL);
				this.theEndIndex = stage;
			}
		}



		private static readonly CastInfo[] castorder = new CastInfo[]
		{
			new CastInfo(DoomInfo.Strings.CC_ZOMBIE, MobjType.Possessed),
			new CastInfo(DoomInfo.Strings.CC_SHOTGUN, MobjType.Shotguy),
			new CastInfo(DoomInfo.Strings.CC_HEAVY, MobjType.Chainguy),
			new CastInfo(DoomInfo.Strings.CC_IMP, MobjType.Troop),
			new CastInfo(DoomInfo.Strings.CC_DEMON, MobjType.Sergeant),
			new CastInfo(DoomInfo.Strings.CC_LOST, MobjType.Skull),
			new CastInfo(DoomInfo.Strings.CC_CACO, MobjType.Head),
			new CastInfo(DoomInfo.Strings.CC_HELL, MobjType.Knight),
			new CastInfo(DoomInfo.Strings.CC_BARON, MobjType.Bruiser),
			new CastInfo(DoomInfo.Strings.CC_ARACH, MobjType.Baby),
			new CastInfo(DoomInfo.Strings.CC_PAIN, MobjType.Pain),
			new CastInfo(DoomInfo.Strings.CC_REVEN, MobjType.Undead),
			new CastInfo(DoomInfo.Strings.CC_MANCU, MobjType.Fatso),
			new CastInfo(DoomInfo.Strings.CC_ARCH, MobjType.Vile),
			new CastInfo(DoomInfo.Strings.CC_SPIDER, MobjType.Spider),
			new CastInfo(DoomInfo.Strings.CC_CYBER, MobjType.Cyborg),
			new CastInfo(DoomInfo.Strings.CC_HERO, MobjType.Player)
		};

		private int castNumber;
		private MobjStateDef castState;
		private int castTics;
		private int castFrames;
		private bool castDeath;
		private bool castOnMelee;
		private bool castAttacking;

		private void StartCast()
		{
			this.stage = 2;

			this.castNumber = 0;
			this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeState];
			this.castTics = this.castState.Tics;
			this.castFrames = 0;
			this.castDeath = false;
			this.castOnMelee = false;
			this.castAttacking = false;

			this.updateResult = UpdateResult.NeedWipe;

			this.options.Music.StartMusic(Bgm.EVIL, true);
		}

		private void UpdateCast()
		{
			if (--this.castTics > 0)
			{
				// Not time to change state yet.
				return;
			}

			if (this.castState.Tics == -1 || this.castState.Next == MobjState.Null)
			{
				// Switch from deathstate to next monster.
				this.castNumber++;
				this.castDeath = false;
				if (this.castNumber == Finale.castorder.Length)
				{
					this.castNumber = 0;
				}
				if (DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeSound != 0)
				{
					this.StartSound(DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeSound);
				}
				this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeState];
				this.castFrames = 0;
			}
			else
			{
				// Just advance to next state in animation.
				if (this.castState == DoomInfo.States[(int)MobjState.PlayAtk1])
				{
					// Oh, gross hack!
					this.castAttacking = false;
					this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeState];
					this.castFrames = 0;
					goto stopAttack;
				}
				var st = this.castState.Next;
				this.castState = DoomInfo.States[(int)st];
				this.castFrames++;

				// Sound hacks....
				Sfx sfx;
				switch (st)
				{
					case MobjState.PlayAtk1:
						sfx = Sfx.DSHTGN;
						break;

					case MobjState.PossAtk2:
						sfx = Sfx.PISTOL;
						break;

					case MobjState.SposAtk2:
						sfx = Sfx.SHOTGN;
						break;

					case MobjState.VileAtk2:
						sfx = Sfx.VILATK;
						break;

					case MobjState.SkelFist2:
						sfx = Sfx.SKESWG;
						break;

					case MobjState.SkelFist4:
						sfx = Sfx.SKEPCH;
						break;

					case MobjState.SkelMiss2:
						sfx = Sfx.SKEATK;
						break;

					case MobjState.FattAtk8:
					case MobjState.FattAtk5:
					case MobjState.FattAtk2:
						sfx = Sfx.FIRSHT;
						break;

					case MobjState.CposAtk2:
					case MobjState.CposAtk3:
					case MobjState.CposAtk4:
						sfx = Sfx.SHOTGN;
						break;

					case MobjState.TrooAtk3:
						sfx = Sfx.CLAW;
						break;

					case MobjState.SargAtk2:
						sfx = Sfx.SGTATK;
						break;

					case MobjState.BossAtk2:
					case MobjState.Bos2Atk2:
					case MobjState.HeadAtk2:
						sfx = Sfx.FIRSHT;
						break;

					case MobjState.SkullAtk2:
						sfx = Sfx.SKLATK;
						break;

					case MobjState.SpidAtk2:
					case MobjState.SpidAtk3:
						sfx = Sfx.SHOTGN;
						break;

					case MobjState.BspiAtk2:
						sfx = Sfx.PLASMA;
						break;

					case MobjState.CyberAtk2:
					case MobjState.CyberAtk4:
					case MobjState.CyberAtk6:
						sfx = Sfx.RLAUNC;
						break;

					case MobjState.PainAtk3:
						sfx = Sfx.SKLATK;
						break;

					default:
						sfx = 0;
						break;
				}

				if (sfx != 0)
				{
					this.StartSound(sfx);
				}
			}

			if (this.castFrames == 12)
			{
				// Go into attack frame.
				this.castAttacking = true;
				if (this.castOnMelee)
				{
					this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].MeleeState];
				}
				else
				{
					this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].MissileState];
				}

				this.castOnMelee = !this.castOnMelee;
				if (this.castState == DoomInfo.States[(int)MobjState.Null])
				{
					if (this.castOnMelee)
					{
						this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].MeleeState];
					}
					else
					{
						this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].MissileState];
					}
				}
			}

			if (this.castAttacking)
			{
				if (this.castFrames == 24 ||
					this.castState == DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeState])
				{
					this.castAttacking = false;
					this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].SeeState];
					this.castFrames = 0;
				}
			}

			stopAttack:

			this.castTics = this.castState.Tics;
			if (this.castTics == -1)
			{
				this.castTics = 15;
			}
		}

		public bool DoEvent(DoomEvent e)
		{
			if (this.stage != 2)
			{
				return false;
			}

			if (e.Type == EventType.KeyDown)
			{
				if (this.castDeath)
				{
					// Already in dying frames.
					return true;
				}

				// Go into death frame.
				this.castDeath = true;
				this.castState = DoomInfo.States[(int)DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].DeathState];
				this.castTics = this.castState.Tics;
				this.castFrames = 0;
				this.castAttacking = false;
				if (DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].DeathSound != 0)
				{
					this.StartSound(DoomInfo.MobjInfos[(int)Finale.castorder[this.castNumber].Type].DeathSound);
				}

				return true;
			}

			return false;
		}

		private void StartSound(Sfx sfx)
		{
			this.options.Sound.StartSound(sfx);
		}



		public GameOptions Options => this.options;
		public string Flat => this.flat;
		public string Text => this.text;
		public int Count => this.count;
		public int Stage => this.stage;

		// For cast.
		public string CastName => Finale.castorder[this.castNumber].Name;
		public MobjStateDef CastState => this.castState;

		// For bunny scroll.
		public int Scrolled => this.scrolled;
		public int TheEndIndex => this.theEndIndex;
		public bool ShowTheEnd => this.showTheEnd;



		private class CastInfo
		{
			public string Name;
			public MobjType Type;

			public CastInfo(string name, MobjType type)
			{
				this.Name = name;
				this.Type = type;
			}
		}
	}
}
