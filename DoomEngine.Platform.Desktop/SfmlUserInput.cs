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
	using Doom.Game;
	using Doom.World;
	using Platform;
	using SFML.Graphics;
	using SFML.System;
	using SFML.Window;
	using System;
	using System.Runtime.ExceptionServices;
	using UserInput;

	public sealed class SfmlUserInput : IUserInput
	{
		private Config config;

		private RenderWindow window;

		private bool useMouse;

		private bool[] weaponKeys;
		private int turnHeld;

		private int windowCenterX;
		private int windowCenterY;
		private int mouseX;
		private int mouseY;
		private bool cursorCentered;

		public SfmlUserInput(Config config, RenderWindow window, bool useMouse)
		{
			try
			{
				Console.Write("Initialize user input: ");

				this.config = config;

				config.mouse_sensitivity = Math.Max(config.mouse_sensitivity, 0);

				this.window = window;

				this.useMouse = useMouse;

				this.weaponKeys = new bool[7];
				this.turnHeld = 0;

				this.windowCenterX = (int) window.Size.X / 2;
				this.windowCenterY = (int) window.Size.Y / 2;
				this.mouseX = 0;
				this.mouseY = 0;
				this.cursorCentered = false;

				if (useMouse)
				{
					window.SetMouseCursorGrabbed(true);
					window.SetMouseCursorVisible(false);
				}

				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				this.Dispose();
				ExceptionDispatchInfo.Throw(e);
			}
		}

		public void BuildTicCmd(TicCmd cmd)
		{
			var keyForward = SfmlUserInput.IsPressed(this.config.key_forward);
			var keyBackward = SfmlUserInput.IsPressed(this.config.key_backward);
			var keyStrafeLeft = SfmlUserInput.IsPressed(this.config.key_strafeleft);
			var keyStrafeRight = SfmlUserInput.IsPressed(this.config.key_straferight);
			var keyTurnLeft = SfmlUserInput.IsPressed(this.config.key_turnleft);
			var keyTurnRight = SfmlUserInput.IsPressed(this.config.key_turnright);
			var keyFire = SfmlUserInput.IsPressed(this.config.key_fire);
			var keyUse = SfmlUserInput.IsPressed(this.config.key_use);
			var keyRun = SfmlUserInput.IsPressed(this.config.key_run);
			var keyStrafe = SfmlUserInput.IsPressed(this.config.key_strafe);

			this.weaponKeys[0] = Keyboard.IsKeyPressed(Keyboard.Key.Num1);
			this.weaponKeys[1] = Keyboard.IsKeyPressed(Keyboard.Key.Num2);
			this.weaponKeys[2] = Keyboard.IsKeyPressed(Keyboard.Key.Num3);
			this.weaponKeys[3] = Keyboard.IsKeyPressed(Keyboard.Key.Num4);
			this.weaponKeys[4] = Keyboard.IsKeyPressed(Keyboard.Key.Num5);
			this.weaponKeys[5] = Keyboard.IsKeyPressed(Keyboard.Key.Num6);
			this.weaponKeys[6] = Keyboard.IsKeyPressed(Keyboard.Key.Num7);

			cmd.Clear();

			var strafe = keyStrafe;
			var speed = keyRun ? 1 : 0;
			var forward = 0;
			var side = 0;

			if (this.config.game_alwaysrun)
			{
				speed = 1 - speed;
			}

			if (keyTurnLeft || keyTurnRight)
			{
				this.turnHeld++;
			}
			else
			{
				this.turnHeld = 0;
			}

			int turnSpeed;

			if (this.turnHeld < PlayerBehavior.SlowTurnTics)
			{
				turnSpeed = 2;
			}
			else
			{
				turnSpeed = speed;
			}

			if (strafe)
			{
				if (keyTurnRight)
				{
					side += PlayerBehavior.SideMove[speed];
				}

				if (keyTurnLeft)
				{
					side -= PlayerBehavior.SideMove[speed];
				}
			}
			else
			{
				if (keyTurnRight)
				{
					cmd.AngleTurn -= (short) PlayerBehavior.AngleTurn[turnSpeed];
				}

				if (keyTurnLeft)
				{
					cmd.AngleTurn += (short) PlayerBehavior.AngleTurn[turnSpeed];
				}
			}

			if (keyForward)
			{
				forward += PlayerBehavior.ForwardMove[speed];
			}

			if (keyBackward)
			{
				forward -= PlayerBehavior.ForwardMove[speed];
			}

			if (keyStrafeLeft)
			{
				side -= PlayerBehavior.SideMove[speed];
			}

			if (keyStrafeRight)
			{
				side += PlayerBehavior.SideMove[speed];
			}

			if (keyFire)
			{
				cmd.Buttons |= TicCmdButtons.Attack;
			}

			if (keyUse)
			{
				cmd.Buttons |= TicCmdButtons.Use;
			}

			// Check weapon keys.
			for (var i = 0; i < this.weaponKeys.Length; i++)
			{
				if (this.weaponKeys[i])
				{
					cmd.Buttons |= TicCmdButtons.Change;
					cmd.Buttons |= (byte) (i << TicCmdButtons.WeaponShift);

					break;
				}
			}

			if (this.useMouse)
			{
				this.UpdateMouse();
				var ms = 0.5F * this.config.mouse_sensitivity;
				var mx = (int) MathF.Round(ms * this.mouseX);
				var my = (int) MathF.Round(ms * this.mouseY);
				forward += my;

				if (strafe)
				{
					side += mx * 2;
				}
				else
				{
					cmd.AngleTurn -= (short) (mx * 0x8);
				}
			}

			if (forward > PlayerBehavior.MaxMove)
			{
				forward = PlayerBehavior.MaxMove;
			}
			else if (forward < -PlayerBehavior.MaxMove)
			{
				forward = -PlayerBehavior.MaxMove;
			}

			if (side > PlayerBehavior.MaxMove)
			{
				side = PlayerBehavior.MaxMove;
			}
			else if (side < -PlayerBehavior.MaxMove)
			{
				side = -PlayerBehavior.MaxMove;
			}

			cmd.ForwardMove += (sbyte) forward;
			cmd.SideMove += (sbyte) side;
		}

		private static bool IsPressed(KeyBinding keyBinding)
		{
			foreach (var key in keyBinding.Keys)
			{
				if (Keyboard.IsKeyPressed((Keyboard.Key) key))
				{
					return true;
				}
			}

			foreach (var mouseButton in keyBinding.MouseButtons)
			{
				if (Mouse.IsButtonPressed((Mouse.Button) mouseButton))
				{
					return true;
				}
			}

			return false;
		}

		public void Reset()
		{
			this.mouseX = 0;
			this.mouseY = 0;
			this.cursorCentered = false;
		}

		private void UpdateMouse()
		{
			if (this.cursorCentered)
			{
				var current = Mouse.GetPosition(this.window);

				this.mouseX = current.X - this.windowCenterX;

				if (this.config.mouse_disableyaxis)
				{
					this.mouseY = 0;
				}
				else
				{
					this.mouseY = -(current.Y - this.windowCenterY);
				}
			}
			else
			{
				this.mouseX = 0;
				this.mouseY = 0;
			}

			Mouse.SetPosition(new Vector2i(this.windowCenterX, this.windowCenterY), this.window);
			var pos = Mouse.GetPosition(this.window);
			this.cursorCentered = (pos.X == this.windowCenterX && pos.Y == this.windowCenterY);
		}

		public void Dispose()
		{
			Console.WriteLine("Shutdown user input.");

			if (this.useMouse)
			{
				this.window.SetMouseCursorVisible(true);
				this.window.SetMouseCursorGrabbed(false);
			}
		}

		public int MaxMouseSensitivity
		{
			get
			{
				return 9;
			}
		}

		public int MouseSensitivity
		{
			get
			{
				return this.config.mouse_sensitivity;
			}

			set
			{
				this.config.mouse_sensitivity = value;
			}
		}
	}
}
