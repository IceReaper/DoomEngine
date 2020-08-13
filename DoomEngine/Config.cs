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
	using System.Collections.Generic;
	using System.IO;
	using UserInput;

	public sealed class Config
    {
        public KeyBinding key_forward;
        public KeyBinding key_backward;
        public KeyBinding key_strafeleft;
        public KeyBinding key_straferight;
        public KeyBinding key_turnleft;
        public KeyBinding key_turnright;
        public KeyBinding key_fire;
        public KeyBinding key_use;
        public KeyBinding key_run;
        public KeyBinding key_strafe;

        public int mouse_sensitivity;
        public bool mouse_disableyaxis;

        public bool game_alwaysrun;

        public int video_screenwidth;
        public int video_screenheight;
        public bool video_fullscreen;
        public bool video_highresolution;
        public bool video_displaymessage;
        public int video_gamescreensize;
        public int video_gammacorrection;

        public int audio_soundvolume;
        public int audio_musicvolume;

        // Default settings.
        public Config()
        {
            this.key_forward = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Up,
                    DoomKey.W
                });
            this.key_backward = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Down,
                    DoomKey.S
                });
            this.key_strafeleft = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.A
                });
            this.key_straferight = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.D
                });
            this.key_turnleft = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Left
                });
            this.key_turnright = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Right
                });
            this.key_fire = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.LControl,
                    DoomKey.RControl
                },
                new DoomMouseButton[]
                {
                    DoomMouseButton.Mouse1
                });
            this.key_use = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.Space
                },
                new DoomMouseButton[]
                {
                    DoomMouseButton.Mouse2
                });
            this.key_run = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.LShift,
                    DoomKey.RShift
                });
            this.key_strafe = new KeyBinding(
                new DoomKey[]
                {
                    DoomKey.LAlt,
                    DoomKey.RAlt
                });

            this.mouse_sensitivity = 3;
            this.mouse_disableyaxis = false;

            this.game_alwaysrun = true;

            var vm = ConfigUtilities.GetDefaultVideoMode();
            this.video_screenwidth = (int)vm.Width;
            this.video_screenheight = (int)vm.Height;
            this.video_fullscreen = false;
            this.video_highresolution = true;
            this.video_gamescreensize = 7;
            this.video_displaymessage = true;
            this.video_gammacorrection = 0;

            this.audio_soundvolume = 8;
            this.audio_musicvolume = 8;
        }

        public Config(string path) : this()
        {
            try
            {
                Console.Write("Restore settings: ");

                var dic = new Dictionary<string, string>();
                foreach (var line in File.ReadLines(path))
                {
                    var split = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length == 2)
                    {
                        dic[split[0].Trim()] = split[1].Trim();
                    }
                }

                this.key_forward = Config.GetKeyBinding(dic, nameof(this.key_forward), this.key_forward);
                this.key_backward = Config.GetKeyBinding(dic, nameof(this.key_backward), this.key_backward);
                this.key_strafeleft = Config.GetKeyBinding(dic, nameof(this.key_strafeleft), this.key_strafeleft);
                this.key_straferight = Config.GetKeyBinding(dic, nameof(this.key_straferight), this.key_straferight);
                this.key_turnleft = Config.GetKeyBinding(dic, nameof(this.key_turnleft), this.key_turnleft);
                this.key_turnright = Config.GetKeyBinding(dic, nameof(this.key_turnright), this.key_turnright);
                this.key_fire = Config.GetKeyBinding(dic, nameof(this.key_fire), this.key_fire);
                this.key_use = Config.GetKeyBinding(dic, nameof(this.key_use), this.key_use);
                this.key_run = Config.GetKeyBinding(dic, nameof(this.key_run), this.key_run);
                this.key_strafe = Config.GetKeyBinding(dic, nameof(this.key_strafe), this.key_strafe);

                this.mouse_sensitivity = Config.GetInt(dic, nameof(this.mouse_sensitivity), this.mouse_sensitivity);
                this.mouse_disableyaxis = Config.GetBool(dic, nameof(this.mouse_disableyaxis), this.mouse_disableyaxis);

                this.game_alwaysrun = Config.GetBool(dic, nameof(this.game_alwaysrun), this.game_alwaysrun);

                this.video_screenwidth = Config.GetInt(dic, nameof(this.video_screenwidth), this.video_screenwidth);
                this.video_screenheight = Config.GetInt(dic, nameof(this.video_screenheight), this.video_screenheight);
                this.video_fullscreen = Config.GetBool(dic, nameof(this.video_fullscreen), this.video_fullscreen);
                this.video_highresolution = Config.GetBool(dic, nameof(this.video_highresolution), this.video_highresolution);
                this.video_displaymessage = Config.GetBool(dic, nameof(this.video_displaymessage), this.video_displaymessage);
                this.video_gamescreensize = Config.GetInt(dic, nameof(this.video_gamescreensize), this.video_gamescreensize);
                this.video_gammacorrection = Config.GetInt(dic, nameof(this.video_gammacorrection), this.video_gammacorrection);

                this.audio_soundvolume = Config.GetInt(dic, nameof(this.audio_soundvolume), this.audio_soundvolume);
                this.audio_musicvolume = Config.GetInt(dic, nameof(this.audio_musicvolume), this.audio_musicvolume);

                Console.WriteLine("OK");
            }
            catch
            {
                Console.WriteLine("Failed");
            }
        }

        public void Save(string path)
        {
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine(nameof(this.key_forward) + " = " + this.key_forward);
                    writer.WriteLine(nameof(this.key_strafeleft) + " = " + this.key_strafeleft);
                    writer.WriteLine(nameof(this.key_straferight) + " = " + this.key_straferight);
                    writer.WriteLine(nameof(this.key_turnleft) + " = " + this.key_turnleft);
                    writer.WriteLine(nameof(this.key_turnright) + " = " + this.key_turnright);
                    writer.WriteLine(nameof(this.key_fire) + " = " + this.key_fire);
                    writer.WriteLine(nameof(this.key_use) + " = " + this.key_use);
                    writer.WriteLine(nameof(this.key_run) + " = " + this.key_run);
                    writer.WriteLine(nameof(this.key_strafe) + " = " + this.key_strafe);

                    writer.WriteLine(nameof(this.mouse_sensitivity) + " = " + this.mouse_sensitivity);
                    writer.WriteLine(nameof(this.mouse_disableyaxis) + " = " + Config.BoolToString(this.mouse_disableyaxis));

                    writer.WriteLine(nameof(this.game_alwaysrun) + " = " + Config.BoolToString(this.game_alwaysrun));

                    writer.WriteLine(nameof(this.video_screenwidth) + " = " + this.video_screenwidth);
                    writer.WriteLine(nameof(this.video_screenheight) + " = " + this.video_screenheight);
                    writer.WriteLine(nameof(this.video_fullscreen) + " = " + Config.BoolToString(this.video_fullscreen));
                    writer.WriteLine(nameof(this.video_highresolution) + " = " + Config.BoolToString(this.video_highresolution));
                    writer.WriteLine(nameof(this.video_displaymessage) + " = " + Config.BoolToString(this.video_displaymessage));
                    writer.WriteLine(nameof(this.video_gamescreensize) + " = " + this.video_gamescreensize);
                    writer.WriteLine(nameof(this.video_gammacorrection) + " = " + this.video_gammacorrection);

                    writer.WriteLine(nameof(this.audio_soundvolume) + " = " + this.audio_soundvolume);
                    writer.WriteLine(nameof(this.audio_musicvolume) + " = " + this.audio_musicvolume);
                }
            }
            catch
            {
            }
        }

        private static int GetInt(Dictionary<string, string> dic, string name, int defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                int value;
                if (int.TryParse(stringValue, out value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static bool GetBool(Dictionary<string, string> dic, string name, bool defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                if (stringValue == "true")
                {
                    return true;
                }
                else if (stringValue == "false")
                {
                    return false;
                }
            }

            return defaultValue;
        }

        private static KeyBinding GetKeyBinding(Dictionary<string, string> dic, string name, KeyBinding defaultValue)
        {
            string stringValue;
            if (dic.TryGetValue(name, out stringValue))
            {
                return KeyBinding.Parse(stringValue);
            }

            return defaultValue;
        }

        private static string BoolToString(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
