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

namespace DoomEngine
{
	using System;
	using System.Diagnostics;
	using System.IO;

	public static class ConfigUtilities
	{
		public static string GetExeDirectory()
		{
			return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
		}

		public static string GetConfigPath()
		{
			return Path.Combine(ConfigUtilities.GetExeDirectory(), "managed-doom.cfg");
		}

		public static string GetDefaultIwadPath()
		{
			var names = new string[] {"DOOM2.WAD", "PLUTONIA.WAD", "TNT.WAD", "DOOM.WAD", "DOOM1.WAD"};

			var exeDirectory = ConfigUtilities.GetExeDirectory();

			foreach (var name in names)
			{
				var path = Path.Combine(exeDirectory, name);

				if (File.Exists(path))
				{
					return path;
				}
			}

			var currentDirectory = Directory.GetCurrentDirectory();

			foreach (var name in names)
			{
				var path = Path.Combine(currentDirectory, name);

				if (File.Exists(path))
				{
					return path;
				}
			}

			throw new Exception("No IWAD was found!");
		}
	}
}
