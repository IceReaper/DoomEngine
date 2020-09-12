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
	using DoomEngine.Game;
	using DoomEngine.Game.Components;
	using DoomEngine.Game.Components.Items;
	using DoomEngine.Game.Components.Weapons;
	using Event;
	using Game;
	using Info;
	using Map;
	using System;
	using System.Linq;
	using UserInput;

	public sealed class Cheat
	{
		private static Tuple<string, Action<Cheat, string>>[] list = new Tuple<string, Action<Cheat, string>>[]
		{
			Tuple.Create("idfa", (Action<Cheat, string>) ((cheat, typed) => cheat.FullAmmo())),
			Tuple.Create("idkfa", (Action<Cheat, string>) ((cheat, typed) => cheat.FullAmmoAndKeys())),
			Tuple.Create("iddqd", (Action<Cheat, string>) ((cheat, typed) => cheat.GodMode())),
			Tuple.Create("idclip", (Action<Cheat, string>) ((cheat, typed) => cheat.NoClip())),
			Tuple.Create("idspispopd", (Action<Cheat, string>) ((cheat, typed) => cheat.NoClip())),
			Tuple.Create("iddt", (Action<Cheat, string>) ((cheat, typed) => cheat.FullMap())),
			Tuple.Create("idbehold", (Action<Cheat, string>) ((cheat, typed) => cheat.ShowPowerUpList())),
			Tuple.Create("idbehold?", (Action<Cheat, string>) ((cheat, typed) => cheat.DoPowerUp(typed))),
			Tuple.Create("tntem", (Action<Cheat, string>) ((cheat, typed) => cheat.KillMonsters())),
			Tuple.Create("killem", (Action<Cheat, string>) ((cheat, typed) => cheat.KillMonsters())),
			Tuple.Create("fhhall", (Action<Cheat, string>) ((cheat, typed) => cheat.KillMonsters())),
			Tuple.Create("idclev??", (Action<Cheat, string>) ((cheat, typed) => cheat.ChangeLevel(typed))),
			Tuple.Create("idmus??", (Action<Cheat, string>) ((cheat, typed) => cheat.ChangeMusic(typed)))
		};

		private static readonly int maxLength = Cheat.list.Max(tuple => tuple.Item1.Length);

		private World world;

		private char[] buffer;
		private int p;

		public Cheat(World world)
		{
			this.world = world;

			this.buffer = new char[Cheat.maxLength];
			this.p = 0;
		}

		public bool DoEvent(DoomEvent e)
		{
			if (e.Type == EventType.KeyDown)
			{
				this.buffer[this.p] = e.Key.GetChar();

				this.p = (this.p + 1) % this.buffer.Length;

				this.CheckBuffer();
			}

			return true;
		}

		private void CheckBuffer()
		{
			for (var i = 0; i < Cheat.list.Length; i++)
			{
				var code = Cheat.list[i].Item1;
				var q = this.p;
				int j;

				for (j = 0; j < code.Length; j++)
				{
					q--;

					if (q == -1)
					{
						q = this.buffer.Length - 1;
					}

					var ch = code[code.Length - j - 1];

					if (this.buffer[q] != ch && ch != '?')
					{
						break;
					}
				}

				if (j == code.Length)
				{
					var typed = new char[code.Length];
					var k = code.Length;
					q = this.p;

					for (j = 0; j < code.Length; j++)
					{
						k--;
						q--;

						if (q == -1)
						{
							q = this.buffer.Length - 1;
						}

						typed[k] = this.buffer[q];
					}

					Cheat.list[i].Item2(this, new string(typed));
				}
			}
		}

		private void GiveWeapons()
		{
			var player = this.world.Options.Player;
			var inventory = player.Entity.GetComponent<InventoryComponent>();

			foreach (var entityInfo in EntityInfo.WithComponent<WeaponComponentInfo>())
				inventory.TryAdd(EntityInfo.Create(this.world, entityInfo));

			player.Backpack = true;

			foreach (var entityInfo in EntityInfo.WithComponent<AmmoComponentInfo>())
				inventory.TryAdd(EntityInfo.Create(this.world, entityInfo));

			foreach (var entity in inventory.Items.Where(item => item.GetComponent<AmmoComponent>() != null))
			{
				var itemComponent = entity.GetComponent<ItemComponent>();

				if (itemComponent != null)
					itemComponent.Amount = itemComponent.Info.StackSize;
			}
		}

		private void FullAmmo()
		{
			this.GiveWeapons();
			var player = this.world.Options.Player;
			player.ArmorType = ItemPickup.IdfaArmorClass;
			player.ArmorPoints = ItemPickup.IdfaArmor;
			player.SendMessage(DoomInfo.Strings.STSTR_FAADDED);
		}

		private void FullAmmoAndKeys()
		{
			this.GiveWeapons();
			var player = this.world.Options.Player;
			player.ArmorType = ItemPickup.IdkfaArmorClass;
			player.ArmorPoints = ItemPickup.IdkfaArmor;

			var inventory = player.Entity.GetComponent<InventoryComponent>();

			foreach (var entityInfo in EntityInfo.WithComponent<KeyComponentInfo>())
				inventory.TryAdd(EntityInfo.Create(this.world, entityInfo));

			player.SendMessage(DoomInfo.Strings.STSTR_KFAADDED);
		}

		private void GodMode()
		{
			var player = this.world.Options.Player;

			if ((player.Cheats & CheatFlags.GodMode) != 0)
			{
				player.Cheats &= ~CheatFlags.GodMode;
				player.SendMessage(DoomInfo.Strings.STSTR_DQDOFF);
			}
			else
			{
				player.Cheats |= CheatFlags.GodMode;
				var healthComponent = player.Entity.GetComponent<Health>();
				healthComponent.Current = Math.Max(ItemPickup.GodModeHealth, healthComponent.Current);
				player.Mobj.Health = healthComponent.Current;
				player.SendMessage(DoomInfo.Strings.STSTR_DQDON);
			}
		}

		private void NoClip()
		{
			var player = this.world.Options.Player;

			if ((player.Cheats & CheatFlags.NoClip) != 0)
			{
				player.Cheats &= ~CheatFlags.NoClip;
				player.SendMessage(DoomInfo.Strings.STSTR_NCOFF);
			}
			else
			{
				player.Cheats |= CheatFlags.NoClip;
				player.SendMessage(DoomInfo.Strings.STSTR_NCON);
			}
		}

		private void FullMap()
		{
			this.world.AutoMap.ToggleCheat();
		}

		private void ShowPowerUpList()
		{
			var player = this.world.Options.Player;
			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLD);
		}

		private void DoPowerUp(string typed)
		{
			switch (typed.Last())
			{
				case 'v':
					this.ToggleInvulnerability();

					break;

				case 's':
					this.ToggleStrength();

					break;

				case 'i':
					this.ToggleInvisibility();

					break;

				case 'r':
					this.ToggleIronFeet();

					break;

				case 'a':
					this.ToggleAllMap();

					break;

				case 'l':
					this.ToggleInfrared();

					break;
			}
		}

		private void ToggleInvulnerability()
		{
			var player = this.world.Options.Player;

			if (player.Powers[(int) PowerType.Invulnerability] > 0)
			{
				player.Powers[(int) PowerType.Invulnerability] = 0;
			}
			else
			{
				player.Powers[(int) PowerType.Invulnerability] = DoomInfo.PowerDuration.Invulnerability;
			}

			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
		}

		private void ToggleStrength()
		{
			var player = this.world.Options.Player;

			if (player.Powers[(int) PowerType.Strength] != 0)
			{
				player.Powers[(int) PowerType.Strength] = 0;
			}
			else
			{
				player.Powers[(int) PowerType.Strength] = 1;
			}

			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
		}

		private void ToggleInvisibility()
		{
			var player = this.world.Options.Player;

			if (player.Powers[(int) PowerType.Invisibility] > 0)
			{
				player.Powers[(int) PowerType.Invisibility] = 0;
				player.Mobj.Flags &= ~MobjFlags.Shadow;
			}
			else
			{
				player.Powers[(int) PowerType.Invisibility] = DoomInfo.PowerDuration.Invisibility;
				player.Mobj.Flags |= MobjFlags.Shadow;
			}

			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
		}

		private void ToggleIronFeet()
		{
			var player = this.world.Options.Player;

			if (player.Powers[(int) PowerType.IronFeet] > 0)
			{
				player.Powers[(int) PowerType.IronFeet] = 0;
			}
			else
			{
				player.Powers[(int) PowerType.IronFeet] = DoomInfo.PowerDuration.IronFeet;
			}

			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
		}

		private void ToggleAllMap()
		{
			var player = this.world.Options.Player;

			if (player.Powers[(int) PowerType.AllMap] != 0)
			{
				player.Powers[(int) PowerType.AllMap] = 0;
			}
			else
			{
				player.Powers[(int) PowerType.AllMap] = 1;
			}

			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
		}

		private void ToggleInfrared()
		{
			var player = this.world.Options.Player;

			if (player.Powers[(int) PowerType.Infrared] > 0)
			{
				player.Powers[(int) PowerType.Infrared] = 0;
			}
			else
			{
				player.Powers[(int) PowerType.Infrared] = DoomInfo.PowerDuration.Infrared;
			}

			player.SendMessage(DoomInfo.Strings.STSTR_BEHOLDX);
		}

		private void KillMonsters()
		{
			var player = this.world.Options.Player;
			var count = 0;

			foreach (var thinker in this.world.Thinkers)
			{
				var mobj = thinker as Mobj;

				if (mobj != null && mobj.Player == null && ((mobj.Flags & MobjFlags.CountKill) != 0 || mobj.Type == MobjType.Skull) && mobj.Health > 0)
				{
					this.world.ThingInteraction.DamageMobj(mobj, null, player.Mobj, 10000);
					count++;
				}
			}

			player.SendMessage(count + " monsters killed");
		}

		private void ChangeLevel(string typed)
		{
			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				int map;

				if (!int.TryParse(typed.Substring(typed.Length - 2, 2), out map))
				{
					return;
				}

				var skill = this.world.Options.Skill;
				this.world.Game.DeferedInitNew(skill, 1, map);
			}
			else
			{
				int episode;

				if (!int.TryParse(typed.Substring(typed.Length - 2, 1), out episode))
				{
					return;
				}

				int map;

				if (!int.TryParse(typed.Substring(typed.Length - 1, 1), out map))
				{
					return;
				}

				var skill = this.world.Options.Skill;
				this.world.Game.DeferedInitNew(skill, episode, map);
			}
		}

		private void ChangeMusic(string typed)
		{
			var options = new GameOptions();

			if (DoomApplication.Instance.IWad == "doom2"
				|| DoomApplication.Instance.IWad == "freedoom2"
				|| DoomApplication.Instance.IWad == "plutonia"
				|| DoomApplication.Instance.IWad == "tnt")
			{
				int map;

				if (!int.TryParse(typed.Substring(typed.Length - 2, 2), out map))
				{
					return;
				}

				options.Map = map;
			}
			else
			{
				int episode;

				if (!int.TryParse(typed.Substring(typed.Length - 2, 1), out episode))
				{
					return;
				}

				int map;

				if (!int.TryParse(typed.Substring(typed.Length - 1, 1), out map))
				{
					return;
				}

				options.Episode = episode;
				options.Map = map;
			}

			this.world.Options.Music.StartMusic(Map.GetMapBgm(options), true);
			this.world.Options.Player.SendMessage(DoomInfo.Strings.STSTR_MUS);
		}
	}
}
