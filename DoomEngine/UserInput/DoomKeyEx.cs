﻿//
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

namespace DoomEngine.UserInput
{
	public static class DoomKeyEx
	{
		public static char GetChar(this DoomKey key)
		{
			switch (key)
			{
				case DoomKey.A:
					return 'a';

				case DoomKey.B:
					return 'b';

				case DoomKey.C:
					return 'c';

				case DoomKey.D:
					return 'd';

				case DoomKey.E:
					return 'e';

				case DoomKey.F:
					return 'f';

				case DoomKey.G:
					return 'g';

				case DoomKey.H:
					return 'h';

				case DoomKey.I:
					return 'i';

				case DoomKey.J:
					return 'j';

				case DoomKey.K:
					return 'k';

				case DoomKey.L:
					return 'l';

				case DoomKey.M:
					return 'm';

				case DoomKey.N:
					return 'n';

				case DoomKey.O:
					return 'o';

				case DoomKey.P:
					return 'p';

				case DoomKey.Q:
					return 'q';

				case DoomKey.R:
					return 'r';

				case DoomKey.S:
					return 's';

				case DoomKey.T:
					return 't';

				case DoomKey.U:
					return 'u';

				case DoomKey.V:
					return 'v';

				case DoomKey.W:
					return 'w';

				case DoomKey.X:
					return 'x';

				case DoomKey.Y:
					return 'y';

				case DoomKey.Z:
					return 'z';

				case DoomKey.Num0:
					return '0';

				case DoomKey.Num1:
					return '1';

				case DoomKey.Num2:
					return '2';

				case DoomKey.Num3:
					return '3';

				case DoomKey.Num4:
					return '4';

				case DoomKey.Num5:
					return '5';

				case DoomKey.Num6:
					return '6';

				case DoomKey.Num7:
					return '7';

				case DoomKey.Num8:
					return '8';

				case DoomKey.Num9:
					return '9';

				case DoomKey.LBracket:
					return '[';

				case DoomKey.RBracket:
					return ']';

				case DoomKey.Semicolon:
					return ';';

				case DoomKey.Comma:
					return ',';

				case DoomKey.Period:
					return '.';

				case DoomKey.Quote:
					return '"';

				case DoomKey.Slash:
					return '/';

				case DoomKey.Backslash:
					return '\\';

				case DoomKey.Equal:
					return '=';

				case DoomKey.Hyphen:
					return '-';

				case DoomKey.Space:
					return ' ';

				case DoomKey.Add:
					return '+';

				case DoomKey.Subtract:
					return '-';

				case DoomKey.Multiply:
					return '*';

				case DoomKey.Divide:
					return '/';

				case DoomKey.Numpad0:
					return '0';

				case DoomKey.Numpad1:
					return '1';

				case DoomKey.Numpad2:
					return '2';

				case DoomKey.Numpad3:
					return '3';

				case DoomKey.Numpad4:
					return '4';

				case DoomKey.Numpad5:
					return '5';

				case DoomKey.Numpad6:
					return '6';

				case DoomKey.Numpad7:
					return '7';

				case DoomKey.Numpad8:
					return '8';

				case DoomKey.Numpad9:
					return '9';

				default:
					return '\0';
			}
		}

		public static string ToString(DoomKey key)
		{
			switch (key)
			{
				case DoomKey.A:
					return "a";

				case DoomKey.B:
					return "b";

				case DoomKey.C:
					return "c";

				case DoomKey.D:
					return "d";

				case DoomKey.E:
					return "e";

				case DoomKey.F:
					return "f";

				case DoomKey.G:
					return "g";

				case DoomKey.H:
					return "h";

				case DoomKey.I:
					return "i";

				case DoomKey.J:
					return "j";

				case DoomKey.K:
					return "k";

				case DoomKey.L:
					return "l";

				case DoomKey.M:
					return "m";

				case DoomKey.N:
					return "n";

				case DoomKey.O:
					return "o";

				case DoomKey.P:
					return "p";

				case DoomKey.Q:
					return "q";

				case DoomKey.R:
					return "r";

				case DoomKey.S:
					return "s";

				case DoomKey.T:
					return "t";

				case DoomKey.U:
					return "u";

				case DoomKey.V:
					return "v";

				case DoomKey.W:
					return "w";

				case DoomKey.X:
					return "x";

				case DoomKey.Y:
					return "y";

				case DoomKey.Z:
					return "z";

				case DoomKey.Num0:
					return "num0";

				case DoomKey.Num1:
					return "num1";

				case DoomKey.Num2:
					return "num2";

				case DoomKey.Num3:
					return "num3";

				case DoomKey.Num4:
					return "num4";

				case DoomKey.Num5:
					return "num5";

				case DoomKey.Num6:
					return "num6";

				case DoomKey.Num7:
					return "num7";

				case DoomKey.Num8:
					return "num8";

				case DoomKey.Num9:
					return "num9";

				case DoomKey.Escape:
					return "escape";

				case DoomKey.LControl:
					return "lcontrol";

				case DoomKey.LShift:
					return "lshift";

				case DoomKey.LAlt:
					return "lalt";

				case DoomKey.LSystem:
					return "lsystem";

				case DoomKey.RControl:
					return "rcontrol";

				case DoomKey.RShift:
					return "rshift";

				case DoomKey.RAlt:
					return "ralt";

				case DoomKey.RSystem:
					return "rsystem";

				case DoomKey.Menu:
					return "menu";

				case DoomKey.LBracket:
					return "lbracket";

				case DoomKey.RBracket:
					return "rbracket";

				case DoomKey.Semicolon:
					return "semicolon";

				case DoomKey.Comma:
					return "comma";

				case DoomKey.Period:
					return "period";

				case DoomKey.Quote:
					return "quote";

				case DoomKey.Slash:
					return "slash";

				case DoomKey.Backslash:
					return "backslash";

				case DoomKey.Tilde:
					return "tilde";

				case DoomKey.Equal:
					return "equal";

				case DoomKey.Hyphen:
					return "hyphen";

				case DoomKey.Space:
					return "space";

				case DoomKey.Enter:
					return "enter";

				case DoomKey.Backspace:
					return "backspace";

				case DoomKey.Tab:
					return "tab";

				case DoomKey.PageUp:
					return "pageup";

				case DoomKey.PageDown:
					return "pagedown";

				case DoomKey.End:
					return "end";

				case DoomKey.Home:
					return "home";

				case DoomKey.Insert:
					return "insert";

				case DoomKey.Delete:
					return "delete";

				case DoomKey.Add:
					return "add";

				case DoomKey.Subtract:
					return "subtract";

				case DoomKey.Multiply:
					return "multiply";

				case DoomKey.Divide:
					return "divide";

				case DoomKey.Left:
					return "left";

				case DoomKey.Right:
					return "right";

				case DoomKey.Up:
					return "up";

				case DoomKey.Down:
					return "down";

				case DoomKey.Numpad0:
					return "numpad0";

				case DoomKey.Numpad1:
					return "numpad1";

				case DoomKey.Numpad2:
					return "numpad2";

				case DoomKey.Numpad3:
					return "numpad3";

				case DoomKey.Numpad4:
					return "numpad4";

				case DoomKey.Numpad5:
					return "numpad5";

				case DoomKey.Numpad6:
					return "numpad6";

				case DoomKey.Numpad7:
					return "numpad7";

				case DoomKey.Numpad8:
					return "numpad8";

				case DoomKey.Numpad9:
					return "numpad9";

				case DoomKey.F1:
					return "f1";

				case DoomKey.F2:
					return "f2";

				case DoomKey.F3:
					return "f3";

				case DoomKey.F4:
					return "f4";

				case DoomKey.F5:
					return "f5";

				case DoomKey.F6:
					return "f6";

				case DoomKey.F7:
					return "f7";

				case DoomKey.F8:
					return "f8";

				case DoomKey.F9:
					return "f9";

				case DoomKey.F10:
					return "f10";

				case DoomKey.F11:
					return "f11";

				case DoomKey.F12:
					return "f12";

				case DoomKey.F13:
					return "f13";

				case DoomKey.F14:
					return "f14";

				case DoomKey.F15:
					return "f15";

				case DoomKey.Pause:
					return "pause";

				default:
					return "unknown";
			}
		}

		public static DoomKey Parse(string value)
		{
			switch (value)
			{
				case "a":
					return DoomKey.A;

				case "b":
					return DoomKey.B;

				case "c":
					return DoomKey.C;

				case "d":
					return DoomKey.D;

				case "e":
					return DoomKey.E;

				case "f":
					return DoomKey.F;

				case "g":
					return DoomKey.G;

				case "h":
					return DoomKey.H;

				case "i":
					return DoomKey.I;

				case "j":
					return DoomKey.J;

				case "k":
					return DoomKey.K;

				case "l":
					return DoomKey.L;

				case "m":
					return DoomKey.M;

				case "n":
					return DoomKey.N;

				case "o":
					return DoomKey.O;

				case "p":
					return DoomKey.P;

				case "q":
					return DoomKey.Q;

				case "r":
					return DoomKey.R;

				case "s":
					return DoomKey.S;

				case "t":
					return DoomKey.T;

				case "u":
					return DoomKey.U;

				case "v":
					return DoomKey.V;

				case "w":
					return DoomKey.W;

				case "x":
					return DoomKey.X;

				case "y":
					return DoomKey.Y;

				case "z":
					return DoomKey.Z;

				case "num0":
					return DoomKey.Num0;

				case "num1":
					return DoomKey.Num1;

				case "num2":
					return DoomKey.Num2;

				case "num3":
					return DoomKey.Num3;

				case "num4":
					return DoomKey.Num4;

				case "num5":
					return DoomKey.Num5;

				case "num6":
					return DoomKey.Num6;

				case "num7":
					return DoomKey.Num7;

				case "num8":
					return DoomKey.Num8;

				case "num9":
					return DoomKey.Num9;

				case "escape":
					return DoomKey.Escape;

				case "lcontrol":
					return DoomKey.LControl;

				case "lshift":
					return DoomKey.LShift;

				case "lalt":
					return DoomKey.LAlt;

				case "lsystem":
					return DoomKey.LSystem;

				case "rcontrol":
					return DoomKey.RControl;

				case "rshift":
					return DoomKey.RShift;

				case "ralt":
					return DoomKey.RAlt;

				case "rsystem":
					return DoomKey.RSystem;

				case "menu":
					return DoomKey.Menu;

				case "lbracket":
					return DoomKey.LBracket;

				case "rbracket":
					return DoomKey.RBracket;

				case "semicolon":
					return DoomKey.Semicolon;

				case "comma":
					return DoomKey.Comma;

				case "period":
					return DoomKey.Period;

				case "quote":
					return DoomKey.Quote;

				case "slash":
					return DoomKey.Slash;

				case "backslash":
					return DoomKey.Backslash;

				case "tilde":
					return DoomKey.Tilde;

				case "equal":
					return DoomKey.Equal;

				case "hyphen":
					return DoomKey.Hyphen;

				case "space":
					return DoomKey.Space;

				case "enter":
					return DoomKey.Enter;

				case "backspace":
					return DoomKey.Backspace;

				case "tab":
					return DoomKey.Tab;

				case "pageup":
					return DoomKey.PageUp;

				case "pagedown":
					return DoomKey.PageDown;

				case "end":
					return DoomKey.End;

				case "home":
					return DoomKey.Home;

				case "insert":
					return DoomKey.Insert;

				case "delete":
					return DoomKey.Delete;

				case "add":
					return DoomKey.Add;

				case "subtract":
					return DoomKey.Subtract;

				case "multiply":
					return DoomKey.Multiply;

				case "divide":
					return DoomKey.Divide;

				case "left":
					return DoomKey.Left;

				case "right":
					return DoomKey.Right;

				case "up":
					return DoomKey.Up;

				case "down":
					return DoomKey.Down;

				case "numpad0":
					return DoomKey.Numpad0;

				case "numpad1":
					return DoomKey.Numpad1;

				case "numpad2":
					return DoomKey.Numpad2;

				case "numpad3":
					return DoomKey.Numpad3;

				case "numpad4":
					return DoomKey.Numpad4;

				case "numpad5":
					return DoomKey.Numpad5;

				case "numpad6":
					return DoomKey.Numpad6;

				case "numpad7":
					return DoomKey.Numpad7;

				case "numpad8":
					return DoomKey.Numpad8;

				case "numpad9":
					return DoomKey.Numpad9;

				case "f1":
					return DoomKey.F1;

				case "f2":
					return DoomKey.F2;

				case "f3":
					return DoomKey.F3;

				case "f4":
					return DoomKey.F4;

				case "f5":
					return DoomKey.F5;

				case "f6":
					return DoomKey.F6;

				case "f7":
					return DoomKey.F7;

				case "f8":
					return DoomKey.F8;

				case "f9":
					return DoomKey.F9;

				case "f10":
					return DoomKey.F10;

				case "f11":
					return DoomKey.F11;

				case "f12":
					return DoomKey.F12;

				case "f13":
					return DoomKey.F13;

				case "f14":
					return DoomKey.F14;

				case "f15":
					return DoomKey.F15;

				case "pause":
					return DoomKey.Pause;

				default:
					return DoomKey.Unknown;
			}
		}
	}
}
