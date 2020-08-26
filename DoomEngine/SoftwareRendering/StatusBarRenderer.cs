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
	using Doom.World;
	using Game;
	using Game.Components;
	using Game.Components.Items;
	using Game.Components.Weapons;
	using Game.Entities.Ammos;
	using System.Linq;

	public sealed class StatusBarRenderer
	{
		public static readonly int Height = 32;

		// Ammo number pos.
		private static readonly int ammoWidth = 3;
		private static readonly int ammoX = 44;
		private static readonly int ammoY = 171;

		// Health number pos.
		private static readonly int healthX = 90;
		private static readonly int healthY = 171;

		// Weapon pos.
		private static readonly int armsX = 111;
		private static readonly int armsY = 172;
		private static readonly int armsBackgroundX = 104;
		private static readonly int armsBackgroundY = 168;
		private static readonly int armsSpaceX = 12;
		private static readonly int armsSpaceY = 10;

		// Frags pos.
		private static readonly int fragsWidth = 2;
		private static readonly int fragsX = 138;
		private static readonly int fragsY = 171;

		// Armor number pos.
		private static readonly int armorX = 221;
		private static readonly int armorY = 171;

		// Key icon positions.
		private static readonly int key0Width = 8;
		private static readonly int key0X = 239;
		private static readonly int key0Y = 171;
		private static readonly int key1Width = StatusBarRenderer.key0Width;
		private static readonly int key1X = 239;
		private static readonly int key1Y = 181;
		private static readonly int key2Width = StatusBarRenderer.key0Width;
		private static readonly int key2X = 239;
		private static readonly int key2Y = 191;

		// Ammunition counter.
		private static readonly int ammo0Width = 3;
		private static readonly int ammo0X = 288;
		private static readonly int ammo0Y = 173;
		private static readonly int ammo1Width = StatusBarRenderer.ammo0Width;
		private static readonly int ammo1X = 288;
		private static readonly int ammo1Y = 179;
		private static readonly int ammo2Width = StatusBarRenderer.ammo0Width;
		private static readonly int ammo2X = 288;
		private static readonly int ammo2Y = 191;
		private static readonly int ammo3Wdth = StatusBarRenderer.ammo0Width;
		private static readonly int ammo3X = 288;
		private static readonly int ammo3Y = 185;

		// Indicate maximum ammunition.
		// Only needed because backpack exists.
		private static readonly int maxAmmo0Width = 3;
		private static readonly int maxAmmo0X = 314;
		private static readonly int maxAmmo0Y = 173;
		private static readonly int maxAmmo1Width = StatusBarRenderer.maxAmmo0Width;
		private static readonly int maxAmmo1X = 314;
		private static readonly int maxAmmo1Y = 179;
		private static readonly int maxAmmo2Width = StatusBarRenderer.maxAmmo0Width;
		private static readonly int maxAmmo2X = 314;
		private static readonly int maxAmmo2Y = 191;
		private static readonly int maxAmmo3Width = StatusBarRenderer.maxAmmo0Width;
		private static readonly int maxAmmo3X = 314;
		private static readonly int maxAmmo3Y = 185;

		private static readonly int faceX = 143;
		private static readonly int faceY = 168;
		private static readonly int faceBackgroundX = 143;
		private static readonly int faceBackgroundY = 169;

		private DrawScreen screen;

		private Patches patches;

		private int scale;

		private NumberWidget ready;
		private PercentWidget health;
		private PercentWidget armor;

		private NumberWidget[] ammo;
		private NumberWidget[] maxAmmo;

		private MultIconWidget[] weapons;

		private MultIconWidget[] keys;

		public StatusBarRenderer(DrawScreen screen)
		{
			this.screen = screen;

			this.patches = new Patches();

			this.scale = screen.Width / 320;

			this.ready = new NumberWidget();
			this.ready.Patches = this.patches.TallNumbers;
			this.ready.Width = StatusBarRenderer.ammoWidth;
			this.ready.X = StatusBarRenderer.ammoX;
			this.ready.Y = StatusBarRenderer.ammoY;

			this.health = new PercentWidget();
			this.health.NumberWidget.Patches = this.patches.TallNumbers;
			this.health.NumberWidget.Width = 3;
			this.health.NumberWidget.X = StatusBarRenderer.healthX;
			this.health.NumberWidget.Y = StatusBarRenderer.healthY;
			this.health.Patch = this.patches.TallPercent;

			this.armor = new PercentWidget();
			this.armor.NumberWidget.Patches = this.patches.TallNumbers;
			this.armor.NumberWidget.Width = 3;
			this.armor.NumberWidget.X = StatusBarRenderer.armorX;
			this.armor.NumberWidget.Y = StatusBarRenderer.armorY;
			this.armor.Patch = this.patches.TallPercent;

			this.ammo = new NumberWidget[4];
			this.ammo[0] = new NumberWidget();
			this.ammo[0].Patches = this.patches.ShortNumbers;
			this.ammo[0].Width = StatusBarRenderer.ammo0Width;
			this.ammo[0].X = StatusBarRenderer.ammo0X;
			this.ammo[0].Y = StatusBarRenderer.ammo0Y;
			this.ammo[1] = new NumberWidget();
			this.ammo[1].Patches = this.patches.ShortNumbers;
			this.ammo[1].Width = StatusBarRenderer.ammo1Width;
			this.ammo[1].X = StatusBarRenderer.ammo1X;
			this.ammo[1].Y = StatusBarRenderer.ammo1Y;
			this.ammo[2] = new NumberWidget();
			this.ammo[2].Patches = this.patches.ShortNumbers;
			this.ammo[2].Width = StatusBarRenderer.ammo2Width;
			this.ammo[2].X = StatusBarRenderer.ammo2X;
			this.ammo[2].Y = StatusBarRenderer.ammo2Y;
			this.ammo[3] = new NumberWidget();
			this.ammo[3].Patches = this.patches.ShortNumbers;
			this.ammo[3].Width = StatusBarRenderer.ammo3Wdth;
			this.ammo[3].X = StatusBarRenderer.ammo3X;
			this.ammo[3].Y = StatusBarRenderer.ammo3Y;

			this.maxAmmo = new NumberWidget[4];
			this.maxAmmo[0] = new NumberWidget();
			this.maxAmmo[0].Patches = this.patches.ShortNumbers;
			this.maxAmmo[0].Width = StatusBarRenderer.maxAmmo0Width;
			this.maxAmmo[0].X = StatusBarRenderer.maxAmmo0X;
			this.maxAmmo[0].Y = StatusBarRenderer.maxAmmo0Y;
			this.maxAmmo[1] = new NumberWidget();
			this.maxAmmo[1].Patches = this.patches.ShortNumbers;
			this.maxAmmo[1].Width = StatusBarRenderer.maxAmmo1Width;
			this.maxAmmo[1].X = StatusBarRenderer.maxAmmo1X;
			this.maxAmmo[1].Y = StatusBarRenderer.maxAmmo1Y;
			this.maxAmmo[2] = new NumberWidget();
			this.maxAmmo[2].Patches = this.patches.ShortNumbers;
			this.maxAmmo[2].Width = StatusBarRenderer.maxAmmo2Width;
			this.maxAmmo[2].X = StatusBarRenderer.maxAmmo2X;
			this.maxAmmo[2].Y = StatusBarRenderer.maxAmmo2Y;
			this.maxAmmo[3] = new NumberWidget();
			this.maxAmmo[3].Patches = this.patches.ShortNumbers;
			this.maxAmmo[3].Width = StatusBarRenderer.maxAmmo3Width;
			this.maxAmmo[3].X = StatusBarRenderer.maxAmmo3X;
			this.maxAmmo[3].Y = StatusBarRenderer.maxAmmo3Y;

			this.weapons = new MultIconWidget[6];

			for (var i = 0; i < this.weapons.Length; i++)
			{
				this.weapons[i] = new MultIconWidget();
				this.weapons[i].X = StatusBarRenderer.armsX + (i % 3) * StatusBarRenderer.armsSpaceX;
				this.weapons[i].Y = StatusBarRenderer.armsY + (i / 3) * StatusBarRenderer.armsSpaceY;
				this.weapons[i].Patches = this.patches.Arms[i];
			}

			this.keys = new MultIconWidget[3];
			this.keys[0] = new MultIconWidget();
			this.keys[0].X = StatusBarRenderer.key0X;
			this.keys[0].Y = StatusBarRenderer.key0Y;
			this.keys[0].Patches = this.patches.Keys;
			this.keys[1] = new MultIconWidget();
			this.keys[1].X = StatusBarRenderer.key1X;
			this.keys[1].Y = StatusBarRenderer.key1Y;
			this.keys[1].Patches = this.patches.Keys;
			this.keys[2] = new MultIconWidget();
			this.keys[2].X = StatusBarRenderer.key2X;
			this.keys[2].Y = StatusBarRenderer.key2Y;
			this.keys[2].Patches = this.patches.Keys;
		}

		public void Render(Player player, bool drawBackground)
		{
			if (drawBackground)
			{
				this.screen.DrawPatch(this.patches.Background, 0, this.scale * (200 - StatusBarRenderer.Height), this.scale);
			}

			var inventory = player.Entity.GetComponent<InventoryComponent>();

			var requiresAmmoComponent = player.ReadyWeapon.GetComponent<RequiresAmmoComponent>();

			if (requiresAmmoComponent != null)
			{
				var itemComponent = inventory.Items.FirstOrDefault(entity => entity.Info.Name == requiresAmmoComponent.Info.Ammo)
					?.GetComponent<ItemComponent>();

				if (itemComponent != null)
					this.DrawNumber(this.ready, itemComponent.Amount);
			}

			var healthComponent = player.Entity.GetComponent<Health>();

			this.DrawPercent(this.health, healthComponent.Current);
			this.DrawPercent(this.armor, player.ArmorPoints);

			var bullets = inventory.Items.FirstOrDefault(entity => entity.Info is AmmoBullets)?.GetComponent<ItemComponent>();
			var bulletsInfo = EntityInfo.OfType<AmmoBullets>().GetComponentInfo<ItemComponentInfo>();
			var shells = inventory.Items.FirstOrDefault(entity => entity.Info is AmmoShells)?.GetComponent<ItemComponent>();
			var shellsInfo = EntityInfo.OfType<AmmoShells>().GetComponentInfo<ItemComponentInfo>();
			var rockets = inventory.Items.FirstOrDefault(entity => entity.Info is AmmoRockets)?.GetComponent<ItemComponent>();
			var rocketsInfo = EntityInfo.OfType<AmmoRockets>().GetComponentInfo<ItemComponentInfo>();
			var cells = inventory.Items.FirstOrDefault(entity => entity.Info is AmmoCells)?.GetComponent<ItemComponent>();
			var cellsInfo = EntityInfo.OfType<AmmoCells>().GetComponentInfo<ItemComponentInfo>();

			this.DrawNumber(this.ammo[0], bullets?.Amount ?? 0);
			this.DrawNumber(this.maxAmmo[0], bulletsInfo.StackSize);

			this.DrawNumber(this.ammo[1], shells?.Amount ?? 0);
			this.DrawNumber(this.maxAmmo[1], shellsInfo.StackSize);

			this.DrawNumber(this.ammo[2], cells?.Amount ?? 0);
			this.DrawNumber(this.maxAmmo[2], cellsInfo.StackSize);

			this.DrawNumber(this.ammo[3], rockets?.Amount ?? 0);
			this.DrawNumber(this.maxAmmo[3], rocketsInfo.StackSize);

			if (drawBackground)
			{
				this.screen.DrawPatch(
					this.patches.ArmsBackground,
					this.scale * StatusBarRenderer.armsBackgroundX,
					this.scale * StatusBarRenderer.armsBackgroundY,
					this.scale
				);
			}

			for (var i = 0; i < this.weapons.Length; i++)
			{
				this.DrawMultIcon(
					this.weapons[i],
					inventory.Items.Select(entity => entity.GetComponent<WeaponComponent>())
						.Any(weaponComponent => weaponComponent != null && weaponComponent.Info.Slot - 2 == i)
						? 1
						: 0
				);
			}

			if (drawBackground)
			{
				this.screen.DrawPatch(
					this.patches.Faces[player.Mobj.World.StatusBar.FaceIndex],
					this.scale * StatusBarRenderer.faceX,
					this.scale * StatusBarRenderer.faceY,
					this.scale
				);
			}

			for (var i = 0; i < 3; i++)
			{
				if (player.Cards[i + 3])
				{
					this.DrawMultIcon(this.keys[i], i + 3);
				}
				else if (player.Cards[i])
				{
					this.DrawMultIcon(this.keys[i], i);
				}
			}
		}

		private void DrawNumber(NumberWidget widget, int num)
		{
			var digits = widget.Width;

			var w = widget.Patches[0].Width;
			var h = widget.Patches[0].Height;
			var x = widget.X;

			var neg = num < 0;

			if (neg)
			{
				if (digits == 2 && num < -9)
				{
					num = -9;
				}
				else if (digits == 3 && num < -99)
				{
					num = -99;
				}

				num = -num;
			}

			x = widget.X - digits * w;

			if (num == 1994)
			{
				return;
			}

			x = widget.X;

			// In the special case of 0, you draw 0.
			if (num == 0)
			{
				this.screen.DrawPatch(widget.Patches[0], this.scale * (x - w), this.scale * widget.Y, this.scale);
			}

			// Draw the new number.
			while (num != 0 && digits-- != 0)
			{
				x -= w;

				this.screen.DrawPatch(widget.Patches[num % 10], this.scale * x, this.scale * widget.Y, this.scale);

				num /= 10;
			}

			// Draw a minus sign if necessary.
			if (neg)
			{
				this.screen.DrawPatch(this.patches.TallMinus, this.scale * (x - 8), this.scale * widget.Y, this.scale);
			}
		}

		private void DrawPercent(PercentWidget per, int value)
		{
			this.screen.DrawPatch(per.Patch, this.scale * per.NumberWidget.X, this.scale * per.NumberWidget.Y, this.scale);

			this.DrawNumber(per.NumberWidget, value);
		}

		private void DrawMultIcon(MultIconWidget mi, int value)
		{
			this.screen.DrawPatch(mi.Patches[value], this.scale * mi.X, this.scale * mi.Y, this.scale);
		}

		private class NumberWidget
		{
			public int X;
			public int Y;
			public int Width;
			public Patch[] Patches;
		}

		private class PercentWidget
		{
			public NumberWidget NumberWidget = new NumberWidget();
			public Patch Patch;
		}

		private class MultIconWidget
		{
			public int X;
			public int Y;
			public Patch[] Patches;
		}

		private class Patches
		{
			public Patch Background;
			public Patch[] TallNumbers;
			public Patch[] ShortNumbers;
			public Patch TallMinus;
			public Patch TallPercent;
			public Patch[] Keys;
			public Patch ArmsBackground;
			public Patch[][] Arms;
			public Patch FaceBackground;
			public Patch[] Faces;

			public Patches()
			{
				this.Background = Patch.FromWad("STBAR");

				this.TallNumbers = new Patch[10];
				this.ShortNumbers = new Patch[10];

				for (var i = 0; i < 10; i++)
				{
					this.TallNumbers[i] = Patch.FromWad("STTNUM" + i);
					this.ShortNumbers[i] = Patch.FromWad("STYSNUM" + i);
				}

				this.TallMinus = Patch.FromWad("STTMINUS");
				this.TallPercent = Patch.FromWad("STTPRCNT");

				this.Keys = new Patch[(int) CardType.Count];

				for (var i = 0; i < this.Keys.Length; i++)
				{
					this.Keys[i] = Patch.FromWad("STKEYS" + i);
				}

				this.ArmsBackground = Patch.FromWad("STARMS");
				this.Arms = new Patch[6][];

				for (var i = 0; i < 6; i++)
				{
					var num = i + 2;
					this.Arms[i] = new Patch[2];
					this.Arms[i][0] = Patch.FromWad("STGNUM" + num);
					this.Arms[i][1] = this.ShortNumbers[num];
				}

				this.FaceBackground = Patch.FromWad("STFB0");

				this.Faces = new Patch[StatusBar.Face.FaceCount];
				var faceCount = 0;

				for (var i = 0; i < StatusBar.Face.PainFaceCount; i++)
				{
					for (var j = 0; j < StatusBar.Face.StraightFaceCount; j++)
					{
						this.Faces[faceCount++] = Patch.FromWad("STFST" + i + j);
					}

					this.Faces[faceCount++] = Patch.FromWad("STFTR" + i + "0");
					this.Faces[faceCount++] = Patch.FromWad("STFTL" + i + "0");
					this.Faces[faceCount++] = Patch.FromWad("STFOUCH" + i);
					this.Faces[faceCount++] = Patch.FromWad("STFEVL" + i);
					this.Faces[faceCount++] = Patch.FromWad("STFKILL" + i);
				}

				this.Faces[faceCount++] = Patch.FromWad("STFGOD0");
				this.Faces[faceCount++] = Patch.FromWad("STFDEAD0");
			}
		}
	}
}
