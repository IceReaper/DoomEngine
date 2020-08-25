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
	using DoomEngine.Game.Components.Items;
	using DoomEngine.Game.Components.Weapons;
	using DoomEngine.Game.Entities.Weapons;
	using Game;
	using Info;
	using Map;
	using Math;
	using System.Linq;

	public sealed class WeaponBehavior
	{
		public static readonly Fixed MeleeRange = Fixed.FromInt(64);
		public static readonly Fixed MissileRange = Fixed.FromInt(32 * 64);

		public static readonly Fixed WeaponTop = Fixed.FromInt(32);
		public static readonly Fixed WeaponBottom = Fixed.FromInt(128);

		private static readonly Fixed RaiseSpeed = Fixed.FromInt(6);
		private static readonly Fixed LowerSpeed = Fixed.FromInt(6);

		private World world;

		public WeaponBehavior(World world)
		{
			this.world = world;
		}

		public void Light0(Player player)
		{
			player.ExtraLight = 0;
		}

		public void WeaponReady(Player player, PlayerSpriteDef psp)
		{
			var pb = this.world.PlayerBehavior;

			// Get out of attack state.
			if (player.Mobj.State == DoomInfo.States[(int) MobjState.PlayAtk1] || player.Mobj.State == DoomInfo.States[(int) MobjState.PlayAtk2])
			{
				player.Mobj.SetState(MobjState.Play);
			}

			if (player.ReadyWeapon.Info is WeaponChainsaw && psp.State == DoomInfo.States[(int) MobjState.Saw])
			{
				this.world.StartSound(player.Mobj, Sfx.SAWIDL, SfxType.Weapon);
			}

			// Check for weapon change.
			// If player is dead, put the weapon away.
			if (player.PendingWeapon != null || player.Health == 0)
			{
				// Change weapon.
				// Pending weapon should allready be validated.
				var newState = player.ReadyWeapon.GetComponent<WeaponComponent>().Info.DownState;
				pb.SetPlayerSprite(player, PlayerSprite.Weapon, newState);

				return;
			}

			// Check for fire.
			// The missile launcher and bfg do not auto fire.
			if ((player.Cmd.Buttons & TicCmdButtons.Attack) != 0)
			{
				if (!player.AttackDown || (!(player.ReadyWeapon.Info is WeaponRocketLauncher) && !(player.ReadyWeapon.Info is WeaponBfg)))
				{
					player.AttackDown = true;
					this.FireWeapon(player);

					return;
				}
			}
			else
			{
				player.AttackDown = false;
			}

			// Bob the weapon based on movement speed.
			var angle = (128 * player.Mobj.World.LevelTime) & Trig.FineMask;
			psp.Sx = Fixed.One + player.Bob * Trig.Cos(angle);

			angle &= Trig.FineAngleCount / 2 - 1;
			psp.Sy = WeaponBehavior.WeaponTop + player.Bob * Trig.Sin(angle);
		}

		private bool CheckAmmo(Player player)
		{
			var requiresAmmoComponent = player.ReadyWeapon.GetComponent<RequiresAmmoComponent>();

			if (requiresAmmoComponent == null)
				return true;

			var inventory = player.Entity.GetComponent<InventoryComponent>();

			var itemComponent = inventory.Items.FirstOrDefault(entity => entity.Info.Name == requiresAmmoComponent.Info.Ammo)?.GetComponent<ItemComponent>();

			if (itemComponent != null && requiresAmmoComponent.Info.AmmoPerShot <= itemComponent.Amount)
			{
				return true;
			}

			do
			{
				player.PendingWeapon = inventory.Items.Where(
							weapon =>
							{
								if (weapon == player.ReadyWeapon)
									return false;

								var requiresAmmoComponent = weapon.GetComponent<RequiresAmmoComponent>();

								if (requiresAmmoComponent == null)
									return false;

								var itemComponent = inventory.Items.FirstOrDefault(entity => entity.Info.Name == requiresAmmoComponent.Info.Ammo)
									?.GetComponent<ItemComponent>();

								if (itemComponent == null || itemComponent.Amount < requiresAmmoComponent.Info.AmmoPerShot)
									return false;

								return true;
							}
						)
						.OrderBy(weapon => -weapon.GetComponent<WeaponComponent>().Info.Slot)
						.FirstOrDefault()
					?? inventory.Items.FirstOrDefault(weapon => weapon.Info is WeaponChainsaw) ?? inventory.Items.First(weapon => weapon.Info is WeaponFists);
			}
			while (player.PendingWeapon == null);

			// Now set appropriate weapon overlay.
			this.world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Weapon, player.ReadyWeapon.GetComponent<WeaponComponent>().Info.DownState);

			return false;
		}

		private void RecursiveSound(Sector sec, int soundblocks, Mobj soundtarget, int validCount)
		{
			// Wake up all monsters in this sector.
			if (sec.ValidCount == validCount && sec.SoundTraversed <= soundblocks + 1)
			{
				// Already flooded.
				return;
			}

			sec.ValidCount = validCount;
			sec.SoundTraversed = soundblocks + 1;
			sec.SoundTarget = soundtarget;

			var mc = this.world.MapCollision;

			for (var i = 0; i < sec.Lines.Length; i++)
			{
				var check = sec.Lines[i];

				if ((check.Flags & LineFlags.TwoSided) == 0)
				{
					continue;
				}

				mc.LineOpening(check);

				if (mc.OpenRange <= Fixed.Zero)
				{
					// Closed door.
					continue;
				}

				Sector other;

				if (check.FrontSide.Sector == sec)
				{
					other = check.BackSide.Sector;
				}
				else
				{
					other = check.FrontSide.Sector;
				}

				if ((check.Flags & LineFlags.SoundBlock) != 0)
				{
					if (soundblocks == 0)
					{
						this.RecursiveSound(other, 1, soundtarget, validCount);
					}
				}
				else
				{
					this.RecursiveSound(other, soundblocks, soundtarget, validCount);
				}
			}
		}

		private void NoiseAlert(Mobj target, Mobj emmiter)
		{
			this.RecursiveSound(emmiter.Subsector.Sector, 0, target, this.world.GetNewValidCount());
		}

		private void FireWeapon(Player player)
		{
			if (!this.CheckAmmo(player))
			{
				return;
			}

			player.Mobj.SetState(MobjState.PlayAtk1);

			var newState = player.ReadyWeapon.GetComponent<WeaponComponent>().Info.AttackState;
			this.world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Weapon, newState);

			this.NoiseAlert(player.Mobj, player.Mobj);
		}

		public void Lower(Player player, PlayerSpriteDef psp)
		{
			psp.Sy += WeaponBehavior.LowerSpeed;

			// Is already down.
			if (psp.Sy < WeaponBehavior.WeaponBottom)
			{
				return;
			}

			// Player is dead.
			if (player.PlayerState == PlayerState.Dead)
			{
				psp.Sy = WeaponBehavior.WeaponBottom;

				// don't bring weapon back up
				return;
			}

			var pb = this.world.PlayerBehavior;

			// The old weapon has been lowered off the screen,
			// so change the weapon and start raising it.
			if (player.Health == 0)
			{
				// Player is dead, so keep the weapon off screen.
				pb.SetPlayerSprite(player, PlayerSprite.Weapon, MobjState.Null);

				return;
			}

			player.ReadyWeapon = player.PendingWeapon;

			pb.BringUpWeapon(player);
		}

		public void Raise(Player player, PlayerSpriteDef psp)
		{
			psp.Sy -= WeaponBehavior.RaiseSpeed;

			if (psp.Sy > WeaponBehavior.WeaponTop)
			{
				return;
			}

			psp.Sy = WeaponBehavior.WeaponTop;

			// The weapon has been raised all the way, so change to the ready state.
			var newState = player.ReadyWeapon.GetComponent<WeaponComponent>().Info.ReadyState;

			this.world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Weapon, newState);
		}

		public void ReFire(Player player)
		{
			// Check for fire.
			// If a weaponchange is pending, let it go through instead.
			if ((player.Cmd.Buttons & TicCmdButtons.Attack) != 0 && player.PendingWeapon == null && player.Health != 0)
			{
				player.Refire++;
				this.FireWeapon(player);
			}
			else
			{
				player.Refire = 0;
				this.CheckAmmo(player);
			}
		}

		public void Light1(Player player)
		{
			player.ExtraLight = 1;
		}

		public void Light2(Player player)
		{
			player.ExtraLight = 2;
		}

		public void CheckReload(Player player)
		{
			this.CheckAmmo(player);
		}

		public void OpenShotgun2(Player player)
		{
			this.world.StartSound(player.Mobj, Sfx.DBOPN, SfxType.Weapon);
		}

		public void LoadShotgun2(Player player)
		{
			this.world.StartSound(player.Mobj, Sfx.DBLOAD, SfxType.Weapon);
		}

		public void CloseShotgun2(Player player)
		{
			this.world.StartSound(player.Mobj, Sfx.DBCLS, SfxType.Weapon);
			this.ReFire(player);
		}

		public void GunFlash(Player player)
		{
			player.Mobj.SetState(MobjState.PlayAtk2);

			this.world.PlayerBehavior.SetPlayerSprite(player, PlayerSprite.Flash, player.ReadyWeapon.GetComponent<WeaponComponent>().Info.FlashState);
		}

		public void A_BFGsound(Player player)
		{
			this.world.StartSound(player.Mobj, Sfx.BFG, SfxType.Weapon);
		}

		public void BFGSpray(Mobj bfgBall)
		{
			var hs = this.world.Hitscan;
			var random = this.world.Random;

			// Offset angles from its attack angle.
			for (var i = 0; i < 40; i++)
			{
				var an = bfgBall.Angle - Angle.Ang90 / 2 + Angle.Ang90 / 40 * (uint) i;

				// bfgBall.Target is the originator (player) of the missile.
				hs.AimLineAttack(bfgBall.Target, an, Fixed.FromInt(16 * 64));

				if (hs.LineTarget == null)
				{
					continue;
				}

				this.world.ThingAllocation.SpawnMobj(hs.LineTarget.X, hs.LineTarget.Y, hs.LineTarget.Z + (hs.LineTarget.Height >> 2), MobjType.Extrabfg);

				var damage = 0;

				for (var j = 0; j < 15; j++)
				{
					damage += (random.Next() & 7) + 1;
				}

				this.world.ThingInteraction.DamageMobj(hs.LineTarget, bfgBall.Target, bfgBall.Target, damage);
			}
		}
	}
}
