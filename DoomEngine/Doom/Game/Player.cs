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

namespace DoomEngine.Doom.Game
{
	using DoomEngine.Game;
	using DoomEngine.Game.Components;
	using DoomEngine.Game.Entities.Ammos;
	using DoomEngine.Game.Entities.Weapons;
	using Info;
	using Math;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using World;

	public sealed class Player
	{
		public static readonly int MaxPlayerCount = 4;

		public static readonly int MaxHealth = 100;
		public static readonly Fixed NormalViewHeight = Fixed.FromInt(41);

		private static readonly string[] defaultPlayerNames = new string[] {"Green", "Indigo", "Brown", "Red"};

		private int number;
		private string name;
		private bool inGame;

		private Mobj mobj;
		private PlayerState playerState;
		private TicCmd cmd;

		// Determine POV, including viewpoint bobbing during movement.
		// Focal origin above mobj.Z.
		private Fixed viewZ;

		// Base height above floor for viewz.
		private Fixed viewHeight;

		// Bob / squat speed.
		private Fixed deltaViewHeight;

		// Bounded / scaled total momentum.
		private Fixed bob;

		// This is only used between levels,
		// mobj.Health is used during levels.
		private int health;
		private int armorPoints;

		// Armor type is 0-2.
		private int armorType;

		// Power ups. invinc and invis are tic counters.
		private int[] powers;
		private bool[] cards;
		private bool backpack;

		// Frags, kills of other players.
		private int[] frags;

		public Entity ReadyWeapon;
		public Entity PendingWeapon;
		public List<Entity> Inventory = new List<Entity>();

		// True if button down last tic.
		private bool attackDown;
		private bool useDown;

		// Bit flags, for cheats and debug.
		private CheatFlags cheats;

		// Refired shots are less accurate.
		private int refire;

		// For intermission stats.
		private int killCount;
		private int itemCount;
		private int secretCount;

		// Hint messages.
		private string message;
		private int messageTime;

		// For screen flashing (red or bright).
		private int damageCount;
		private int bonusCount;

		// Who did damage (null for floors / ceilings).
		private Mobj attacker;

		// So gun flashes light up areas.
		private int extraLight;

		// Current PLAYPAL, ???
		// can be set to REDCOLORMAP for pain, etc.
		private int fixedColorMap;

		// Player skin colorshift,
		// 0-3 for which color to draw player.
		private int colorMap;

		// Overlay view sprites (gun, etc).
		private PlayerSpriteDef[] playerSprites;

		// True if secret level has been done.
		private bool didSecret;

		public Player(int number)
		{
			this.number = number;

			this.name = Player.defaultPlayerNames[number];

			this.cmd = new TicCmd();

			this.powers = new int[(int) PowerType.Count];
			this.cards = new bool[(int) CardType.Count];

			this.frags = new int[Player.MaxPlayerCount];

			this.playerSprites = new PlayerSpriteDef[(int) PlayerSprite.Count];

			for (var i = 0; i < this.playerSprites.Length; i++)
			{
				this.playerSprites[i] = new PlayerSpriteDef();
			}
		}

		public void Clear()
		{
			this.mobj = null;
			this.playerState = 0;
			this.cmd.Clear();

			this.viewZ = Fixed.Zero;
			this.viewHeight = Fixed.Zero;
			this.deltaViewHeight = Fixed.Zero;
			this.bob = Fixed.Zero;

			this.health = 0;
			this.armorPoints = 0;
			this.armorType = 0;

			Array.Clear(this.powers, 0, this.powers.Length);
			Array.Clear(this.cards, 0, this.cards.Length);
			this.backpack = false;

			Array.Clear(this.frags, 0, this.frags.Length);

			this.ReadyWeapon = null;
			this.PendingWeapon = null;

			this.Inventory.Clear();

			this.useDown = false;
			this.attackDown = false;

			this.cheats = 0;

			this.refire = 0;

			this.killCount = 0;
			this.itemCount = 0;
			this.secretCount = 0;

			this.message = null;
			this.messageTime = 0;

			this.damageCount = 0;
			this.bonusCount = 0;

			this.attacker = null;

			this.extraLight = 0;

			this.fixedColorMap = 0;

			this.colorMap = 0;

			foreach (var psp in this.playerSprites)
			{
				psp.Clear();
			}

			this.didSecret = false;
		}

		public void Reborn()
		{
			this.mobj = null;
			this.playerState = PlayerState.Live;
			this.cmd.Clear();

			this.viewZ = Fixed.Zero;
			this.viewHeight = Fixed.Zero;
			this.deltaViewHeight = Fixed.Zero;
			this.bob = Fixed.Zero;

			this.health = Player.MaxHealth;
			this.armorPoints = 0;
			this.armorType = 0;

			Array.Clear(this.powers, 0, this.powers.Length);
			Array.Clear(this.cards, 0, this.cards.Length);
			this.backpack = false;

			this.Inventory.Clear();
			var fists = EntityInfo.Create<WeaponFists>();
			var pistol = EntityInfo.Create<WeaponPistol>();

			this.Inventory.Add(fists);
			this.Inventory.Add(pistol);
			this.ReadyWeapon = pistol;
			this.PendingWeapon = pistol;

			var bullets = EntityInfo.Create<AmmoBullets>();
			bullets.Components.OfType<AmmoComponent>().First().Amount = 50;
			this.Inventory.Add(bullets);

			// Don't do anything immediately.
			this.useDown = true;
			this.attackDown = true;

			this.cheats = 0;

			this.refire = 0;

			this.message = null;
			this.messageTime = 0;

			this.damageCount = 0;
			this.bonusCount = 0;

			this.attacker = null;

			this.extraLight = 0;

			this.fixedColorMap = 0;

			this.colorMap = 0;

			foreach (var psp in this.playerSprites)
			{
				psp.Clear();
			}

			this.didSecret = false;
		}

		public void FinishLevel()
		{
			Array.Clear(this.powers, 0, this.powers.Length);
			Array.Clear(this.cards, 0, this.cards.Length);

			// Cancel invisibility.
			this.mobj.Flags &= ~MobjFlags.Shadow;

			// Cancel gun flashes.
			this.extraLight = 0;

			// Cancel ir gogles.
			this.fixedColorMap = 0;

			// No palette changes.
			this.damageCount = 0;
			this.bonusCount = 0;
		}

		public void SendMessage(string message)
		{
			if (object.ReferenceEquals(this.message, (string) DoomInfo.Strings.MSGOFF) && !object.ReferenceEquals(message, (string) DoomInfo.Strings.MSGON))
			{
				return;
			}

			this.message = message;
			this.messageTime = 4 * GameConst.TicRate;
		}

		public int Number => this.number;

		public string Name => this.name;

		public bool InGame
		{
			get => this.inGame;
			set => this.inGame = value;
		}

		public Mobj Mobj
		{
			get => this.mobj;
			set => this.mobj = value;
		}

		public PlayerState PlayerState
		{
			get => this.playerState;
			set => this.playerState = value;
		}

		public TicCmd Cmd
		{
			get => this.cmd;
		}

		public Fixed ViewZ
		{
			get => this.viewZ;
			set => this.viewZ = value;
		}

		public Fixed ViewHeight
		{
			get => this.viewHeight;
			set => this.viewHeight = value;
		}

		public Fixed DeltaViewHeight
		{
			get => this.deltaViewHeight;
			set => this.deltaViewHeight = value;
		}

		public Fixed Bob
		{
			get => this.bob;
			set => this.bob = value;
		}

		public int Health
		{
			get => this.health;
			set => this.health = value;
		}

		public int ArmorPoints
		{
			get => this.armorPoints;
			set => this.armorPoints = value;
		}

		public int ArmorType
		{
			get => this.armorType;
			set => this.armorType = value;
		}

		public int[] Powers
		{
			get => this.powers;
		}

		public bool[] Cards
		{
			get => this.cards;
		}

		public bool Backpack
		{
			get => this.backpack;
			set => this.backpack = value;
		}

		public int[] Frags
		{
			get => this.frags;
		}

		public bool AttackDown
		{
			get => this.attackDown;
			set => this.attackDown = value;
		}

		public bool UseDown
		{
			get => this.useDown;
			set => this.useDown = value;
		}

		public CheatFlags Cheats
		{
			get => this.cheats;
			set => this.cheats = value;
		}

		public int Refire
		{
			get => this.refire;
			set => this.refire = value;
		}

		public int KillCount
		{
			get => this.killCount;
			set => this.killCount = value;
		}

		public int ItemCount
		{
			get => this.itemCount;
			set => this.itemCount = value;
		}

		public int SecretCount
		{
			get => this.secretCount;
			set => this.secretCount = value;
		}

		public string Message
		{
			get => this.message;
			set => this.message = value;
		}

		public int MessageTime
		{
			get => this.messageTime;
			set => this.messageTime = value;
		}

		public int DamageCount
		{
			get => this.damageCount;
			set => this.damageCount = value;
		}

		public int BonusCount
		{
			get => this.bonusCount;
			set => this.bonusCount = value;
		}

		public Mobj Attacker
		{
			get => this.attacker;
			set => this.attacker = value;
		}

		public int ExtraLight
		{
			get => this.extraLight;
			set => this.extraLight = value;
		}

		public int FixedColorMap
		{
			get => this.fixedColorMap;
			set => this.fixedColorMap = value;
		}

		public int ColorMap
		{
			get => this.colorMap;
			set => this.colorMap = value;
		}

		public PlayerSpriteDef[] PlayerSprites
		{
			get => this.playerSprites;
		}

		public bool DidSecret
		{
			get => this.didSecret;
			set => this.didSecret = value;
		}
	}
}
