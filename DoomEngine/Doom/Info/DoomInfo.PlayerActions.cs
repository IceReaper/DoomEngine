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

namespace DoomEngine.Doom.Info
{
	using DoomEngine.Game.Components;
	using Game;
	using System.Linq;
	using World;

	public static partial class DoomInfo
	{
		private class PlayerActions
		{
			public void Light0(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.Light0(player);
			}

			public void WeaponReady(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.WeaponReady(player, psp);
			}

			public void Lower(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.Lower(player, psp);
			}

			public void Raise(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.Raise(player, psp);
			}

			public void ReFire(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.ReFire(player);
			}

			public void Light1(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.Light1(player);
			}

			public void Light2(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.Light2(player);
			}

			public void CheckReload(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.CheckReload(player);
			}

			public void OpenShotgun2(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.OpenShotgun2(player);
			}

			public void LoadShotgun2(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.LoadShotgun2(player);
			}

			public void CloseShotgun2(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.CloseShotgun2(player);
			}

			public void GunFlash(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.GunFlash(player);
			}

			public static void Fire(World world, Player player, PlayerSpriteDef psp)
			{
				player.ReadyWeapon.GetComponents<WeaponComponent>().First().Fire(world, player, psp);
			}

			public void BFGsound(World world, Player player, PlayerSpriteDef psp)
			{
				world.WeaponBehavior.A_BFGsound(player);
			}
		}
	}
}
