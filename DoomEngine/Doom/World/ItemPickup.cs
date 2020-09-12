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
	using Audio;
	using DoomEngine.Game;
	using DoomEngine.Game.Components;
	using DoomEngine.Game.Components.Items;
	using DoomEngine.Game.Components.Weapons;
	using DoomEngine.Game.Entities.Ammos;
	using DoomEngine.Game.Entities.Keys;
	using DoomEngine.Game.Entities.Weapons;
	using Game;
	using Graphics;
	using Info;
	using Math;
	using System;
	using System.Linq;

	public sealed class ItemPickup
	{
		public static int GreenArmorClass { get; set; } = 1;
		public static int BlueArmorClass { get; set; } = 2;
		public static int SoulsphereHealth { get; set; } = 100;
		public static int MegasphereHealth { get; set; } = 200;
		public static int GodModeHealth { get; set; } = 100;
		public static int IdfaArmor { get; set; } = 200;
		public static int IdfaArmorClass { get; set; } = 2;
		public static int IdkfaArmor { get; set; } = 200;
		public static int IdkfaArmorClass { get; set; } = 2;

		private World world;

		public ItemPickup(World world)
		{
			this.world = world;
		}

		/// <summary>
		/// Give the player the ammo.
		/// </summary>
		/// <param name="amount">
		/// The number of clip loads, not the individual count (0 = 1/2 clip).
		/// </param>
		/// <returns>
		/// False if the ammo can't be picked up at all.
		/// </returns>
		public bool GiveAmmo(Player player, string ammoName, int amount)
		{
			var ammoEntity = EntityInfo.Create(this.world, EntityInfo.OfName(ammoName));
			var itemComponent = ammoEntity.GetComponent<ItemComponent>();

			if (amount != 0)
				amount *= itemComponent.Info.InitialAmount;
			else
				amount = itemComponent.Info.InitialAmount / 2;

			if (this.world.Options.Skill == GameSkill.Baby || this.world.Options.Skill == GameSkill.Nightmare)
				amount *= 2;

			itemComponent.Amount = amount;

			var inventory = player.Entity.GetComponent<InventoryComponent>();

			if (!inventory.TryAdd(ammoEntity))
				return false;

			if (ammoEntity.Info is AmmoBullets)
			{
				if (player.ReadyWeapon.Info is WeaponFists)
					player.PendingWeapon = inventory.Items.FirstOrDefault(weapon => weapon.Info is WeaponChaingun)
						?? inventory.Items.First(weapon => weapon.Info is WeaponPistol);
			}
			else if (ammoEntity.Info is AmmoShells)
			{
				if (player.ReadyWeapon.Info is WeaponFists || player.ReadyWeapon.Info is WeaponPistol)
					player.PendingWeapon = inventory.Items.FirstOrDefault(weapon => weapon.Info is WeaponShotgun)
						?? inventory.Items.FirstOrDefault(weapon => weapon.Info is WeaponSuperShotgun) ?? player.PendingWeapon;
			}
			else if (ammoEntity.Info is AmmoCells)
			{
				if (player.ReadyWeapon.Info is WeaponFists || player.ReadyWeapon.Info is WeaponPistol)
					player.PendingWeapon = inventory.Items.FirstOrDefault(weapon => weapon.Info is WeaponPlasmagun) ?? player.PendingWeapon;
			}
			else if (ammoEntity.Info is AmmoRockets)
			{
				if (player.ReadyWeapon.Info is WeaponFists)
					player.PendingWeapon = inventory.Items.FirstOrDefault(weapon => weapon.Info is WeaponRocketLauncher) ?? player.PendingWeapon;
			}

			return true;
		}

		private static readonly int bonusAdd = 6;

		/// <summary>
		/// Give the weapon to the player.
		/// </summary>
		/// <param name="dropped">
		/// True if the weapons is dropped by a monster.
		/// </param>
		public bool GiveWeapon(Player player, string weaponName, bool dropped)
		{
			var weaponEntity = EntityInfo.Create(this.world, EntityInfo.OfName(weaponName));
			var requiresAmmoComponent = weaponEntity.GetComponent<RequiresAmmoComponent>();

			var inventory = player.Entity.GetComponent<InventoryComponent>();
			var gaveWeapon = inventory.TryAdd(weaponEntity);
			var gaveAmmo = requiresAmmoComponent != null && this.GiveAmmo(player, requiresAmmoComponent.Info.Ammo, dropped ? 1 : 2);

			if (gaveWeapon)
				player.PendingWeapon = weaponEntity;

			return gaveWeapon || gaveAmmo;
		}

		/// <summary>
		/// Give the health point to the player.
		/// </summary>
		/// <returns>
		/// False if the health point isn't needed at all.
		/// </returns>
		private bool GiveHealth(Player player, int amount)
		{
			var healthComponent = player.Entity.GetComponent<Health>();

			if (healthComponent.Current >= healthComponent.Info.Full)
				return false;

			healthComponent.Current += amount;

			if (healthComponent.Current > healthComponent.Info.Full)
				healthComponent.Current = healthComponent.Info.Full;

			player.Mobj.Health = healthComponent.Current;

			return true;
		}

		/// <summary>
		/// Give the armor to the player.
		/// </summary>
		/// <returns>
		/// Returns false if the armor is worse than the current armor.
		/// </returns>
		private bool GiveArmor(Player player, int type)
		{
			var hits = type * 100;

			if (player.ArmorPoints >= hits)
			{
				// Don't pick up.
				return false;
			}

			player.ArmorType = type;
			player.ArmorPoints = hits;

			return true;
		}

		/// <summary>
		/// Give the power up to the player.
		/// </summary>
		/// <returns>
		/// False if the power up is not necessary.
		/// </returns>
		private bool GivePower(Player player, PowerType type)
		{
			if (type == PowerType.Invulnerability)
			{
				player.Powers[(int) type] = DoomInfo.PowerDuration.Invulnerability;

				return true;
			}

			if (type == PowerType.Invisibility)
			{
				player.Powers[(int) type] = DoomInfo.PowerDuration.Invisibility;
				player.Mobj.Flags |= MobjFlags.Shadow;

				return true;
			}

			if (type == PowerType.Infrared)
			{
				player.Powers[(int) type] = DoomInfo.PowerDuration.Infrared;

				return true;
			}

			if (type == PowerType.IronFeet)
			{
				player.Powers[(int) type] = DoomInfo.PowerDuration.IronFeet;

				return true;
			}

			if (type == PowerType.Strength)
			{
				this.GiveHealth(player, 100);
				player.Powers[(int) type] = 1;

				return true;
			}

			if (player.Powers[(int) type] != 0)
			{
				// Already got it.
				return false;
			}

			player.Powers[(int) type] = 1;

			return true;
		}

		/// <summary>
		/// Check for item pickup.
		/// </summary>
		public void TouchSpecialThing(Mobj special, Mobj toucher)
		{
			var delta = special.Z - toucher.Z;

			if (delta > toucher.Height || delta < Fixed.FromInt(-8))
			{
				// Out of reach.
				return;
			}

			var sound = Sfx.ITEMUP;
			var player = toucher.Player;

			// Dead thing touching.
			// Can happen with a sliding player corpse.
			if (toucher.Health <= 0)
			{
				return;
			}

			var healthComponent = player.Entity.GetComponent<Health>();

			// Identify by sprite.
			switch (special.Sprite)
			{
				// Armor.
				case Sprite.ARM1:
					if (!this.GiveArmor(player, ItemPickup.GreenArmorClass))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTARMOR);

					break;

				case Sprite.ARM2:
					if (!this.GiveArmor(player, ItemPickup.BlueArmorClass))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTMEGA);

					break;

				// Bonus items.
				case Sprite.BON1:
					healthComponent.Current++;

					if (healthComponent.Current > healthComponent.Info.Maximum)
						healthComponent.Current = healthComponent.Info.Maximum;

					player.Mobj.Health = healthComponent.Current;
					player.SendMessage(DoomInfo.Strings.GOTHTHBONUS);

					break;

				case Sprite.BON2:
					// Can go over 100%.
					player.ArmorPoints++;

					if (player.ArmorPoints > Player.MaxArmor)
					{
						player.ArmorPoints = Player.MaxArmor;
					}

					if (player.ArmorType == 0)
					{
						player.ArmorType = ItemPickup.GreenArmorClass;
					}

					player.SendMessage(DoomInfo.Strings.GOTARMBONUS);

					break;

				case Sprite.SOUL:
					healthComponent.Current += ItemPickup.SoulsphereHealth;

					if (healthComponent.Current > healthComponent.Info.Maximum)
						healthComponent.Current = healthComponent.Info.Maximum;

					player.Mobj.Health = healthComponent.Current;
					player.SendMessage(DoomInfo.Strings.GOTSUPER);
					sound = Sfx.GETPOW;

					break;

				case Sprite.MEGA:
					if (DoomApplication.Instance.IWad != "doom2"
						&& DoomApplication.Instance.IWad != "freedoom2"
						&& DoomApplication.Instance.IWad != "plutonia"
						&& DoomApplication.Instance.IWad != "tnt")
					{
						return;
					}

					healthComponent.Current = ItemPickup.MegasphereHealth;
					player.Mobj.Health = healthComponent.Current;
					this.GiveArmor(player, ItemPickup.BlueArmorClass);
					player.SendMessage(DoomInfo.Strings.GOTMSPHERE);
					sound = Sfx.GETPOW;

					break;

				// Cards.
				// Leave cards for everyone.
				case Sprite.BKEY:
					if (player.Entity.GetComponent<InventoryComponent>().TryAdd(EntityInfo.Create<BlueCard>(this.world)))
						player.SendMessage(DoomInfo.Strings.GOTBLUECARD);

					break;

				case Sprite.YKEY:
					if (player.Entity.GetComponent<InventoryComponent>().TryAdd(EntityInfo.Create<YellowCard>(this.world)))
						player.SendMessage(DoomInfo.Strings.GOTYELWCARD);

					break;

				case Sprite.RKEY:
					if (player.Entity.GetComponent<InventoryComponent>().TryAdd(EntityInfo.Create<RedCard>(this.world)))
						player.SendMessage(DoomInfo.Strings.GOTREDCARD);

					break;

				case Sprite.BSKU:
					if (player.Entity.GetComponent<InventoryComponent>().TryAdd(EntityInfo.Create<BlueSkull>(this.world)))
						player.SendMessage(DoomInfo.Strings.GOTBLUESKUL);

					break;

				case Sprite.YSKU:
					if (player.Entity.GetComponent<InventoryComponent>().TryAdd(EntityInfo.Create<YellowSkull>(this.world)))
						player.SendMessage(DoomInfo.Strings.GOTYELWSKUL);

					break;

				case Sprite.RSKU:
					if (player.Entity.GetComponent<InventoryComponent>().TryAdd(EntityInfo.Create<RedSkull>(this.world)))
						player.SendMessage(DoomInfo.Strings.GOTREDSKULL);

					break;

				// Medikits, heals.
				case Sprite.STIM:
					if (!this.GiveHealth(player, 10))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTSTIM);

					break;

				case Sprite.MEDI:
					if (!this.GiveHealth(player, 25))
					{
						return;
					}

					if (healthComponent.Current < 25)
					{
						player.SendMessage(DoomInfo.Strings.GOTMEDINEED);
					}
					else
					{
						player.SendMessage(DoomInfo.Strings.GOTMEDIKIT);
					}

					break;

				// Power ups.
				case Sprite.PINV:
					if (!this.GivePower(player, PowerType.Invulnerability))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTINVUL);
					sound = Sfx.GETPOW;

					break;

				case Sprite.PSTR:
					if (!this.GivePower(player, PowerType.Strength))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTBERSERK);

					var fists = player.Entity.GetComponent<InventoryComponent>().Items.FirstOrDefault(weapon => weapon.Info is WeaponFists);

					if (fists != null && player.ReadyWeapon != fists)
					{
						player.PendingWeapon = fists;
					}

					sound = Sfx.GETPOW;

					break;

				case Sprite.PINS:
					if (!this.GivePower(player, PowerType.Invisibility))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTINVIS);
					sound = Sfx.GETPOW;

					break;

				case Sprite.SUIT:
					if (!this.GivePower(player, PowerType.IronFeet))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTSUIT);
					sound = Sfx.GETPOW;

					break;

				case Sprite.PMAP:
					if (!this.GivePower(player, PowerType.AllMap))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTMAP);
					sound = Sfx.GETPOW;

					break;

				case Sprite.PVIS:
					if (!this.GivePower(player, PowerType.Infrared))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTVISOR);
					sound = Sfx.GETPOW;

					break;

				// Ammo.
				case Sprite.CLIP:
					if ((special.Flags & MobjFlags.Dropped) != 0)
					{
						if (!this.GiveAmmo(player, nameof(AmmoBullets), 0))
						{
							return;
						}
					}
					else
					{
						if (!this.GiveAmmo(player, nameof(AmmoBullets), 1))
						{
							return;
						}
					}

					player.SendMessage(DoomInfo.Strings.GOTCLIP);

					break;

				case Sprite.AMMO:
					if (!this.GiveAmmo(player, nameof(AmmoBullets), 5))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTCLIPBOX);

					break;

				case Sprite.ROCK:
					if (!this.GiveAmmo(player, nameof(AmmoRockets), 1))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTROCKET);

					break;

				case Sprite.BROK:
					if (!this.GiveAmmo(player, nameof(AmmoRockets), 5))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTROCKBOX);

					break;

				case Sprite.CELL:
					if (!this.GiveAmmo(player, nameof(AmmoCells), 1))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTCELL);

					break;

				case Sprite.CELP:
					if (!this.GiveAmmo(player, nameof(AmmoCells), 5))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTCELLBOX);

					break;

				case Sprite.SHEL:
					if (!this.GiveAmmo(player, nameof(AmmoShells), 1))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTSHELLS);

					break;

				case Sprite.SBOX:
					if (!this.GiveAmmo(player, nameof(AmmoShells), 5))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTSHELLBOX);

					break;

				case Sprite.BPAK:
					if (!player.Backpack)
					{
						// TODO Re-implement this!
						/*for (var i = 0; i < (int) AmmoType.Count; i++)
						{
							player.MaxAmmo[i] *= 2;
						}*/

						player.Backpack = true;
					}

					foreach (var entityInfo in EntityInfo.WithComponent<AmmoComponentInfo>())
						this.GiveAmmo(player, entityInfo.Name, 1);

					player.SendMessage(DoomInfo.Strings.GOTBACKPACK);

					break;

				// Weapons.
				case Sprite.BFUG:
					if (!this.GiveWeapon(player, nameof(WeaponBfg), false))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTBFG9000);
					sound = Sfx.WPNUP;

					break;

				case Sprite.MGUN:
					if (!this.GiveWeapon(player, nameof(WeaponChaingun), (special.Flags & MobjFlags.Dropped) != 0))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTCHAINGUN);
					sound = Sfx.WPNUP;

					break;

				case Sprite.CSAW:
					if (!this.GiveWeapon(player, nameof(WeaponChainsaw), false))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTCHAINSAW);
					sound = Sfx.WPNUP;

					break;

				case Sprite.LAUN:
					if (!this.GiveWeapon(player, nameof(WeaponRocketLauncher), false))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTLAUNCHER);
					sound = Sfx.WPNUP;

					break;

				case Sprite.PLAS:
					if (!this.GiveWeapon(player, nameof(WeaponPlasmagun), false))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTPLASMA);
					sound = Sfx.WPNUP;

					break;

				case Sprite.SHOT:
					if (!this.GiveWeapon(player, nameof(WeaponShotgun), (special.Flags & MobjFlags.Dropped) != 0))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTSHOTGUN);
					sound = Sfx.WPNUP;

					break;

				case Sprite.SGN2:
					if (!this.GiveWeapon(player, nameof(WeaponSuperShotgun), (special.Flags & MobjFlags.Dropped) != 0))
					{
						return;
					}

					player.SendMessage(DoomInfo.Strings.GOTSHOTGUN2);
					sound = Sfx.WPNUP;

					break;

				default:
					throw new Exception("Unknown gettable thing!");
			}

			if ((special.Flags & MobjFlags.CountItem) != 0)
			{
				player.ItemCount++;
			}

			this.world.ThingAllocation.RemoveMobj(special);

			player.BonusCount += ItemPickup.bonusAdd;

			this.world.StartSound(player.Mobj, sound, SfxType.Misc);
		}
	}
}
