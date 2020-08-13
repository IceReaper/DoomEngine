//
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
	using Audio;
	using Doom.Common;
	using Doom.Game;
	using Doom.Graphics;
	using Doom.Info;
	using Doom.Math;
	using Doom.World;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using System.Text;

	public static class DeHackEd
    {
        private static Tuple<Action<World, Player, PlayerSpriteDef>, Action<World, Mobj>>[] sourcePointerTable;

        public static void ReadFiles(params string[] fileNames)
        {
            try
            {
                Console.Write("Apply DeHackEd patches: ");

                foreach (var fileName in fileNames)
                {
                    DeHackEd.ReadFile(fileName);
                }

                Console.WriteLine("OK (" + string.Join(", ", fileNames.Select(x => Path.GetFileName(x))) + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private static void ReadFile(string fileName)
        {
            if (DeHackEd.sourcePointerTable == null)
            {
                DeHackEd.sourcePointerTable = new Tuple<Action<World, Player, PlayerSpriteDef>, Action<World, Mobj>>[DoomInfo.States.Length];
                for (var i = 0; i < DeHackEd.sourcePointerTable.Length; i++)
                {
                    var playerAction = DoomInfo.States[i].PlayerAction;
                    var mobjAction = DoomInfo.States[i].MobjAction;
                    DeHackEd.sourcePointerTable[i] = Tuple.Create(playerAction, mobjAction);
                }
            }

            var data = new List<string>();
            var last = Block.None;
            foreach (var line in File.ReadLines(fileName))
            {
                var split = line.Split(' ');
                var blockType = DeHackEd.GetBlockType(split);
                if (blockType == Block.None)
                {
                    data.Add(line);
                }
                else
                {
                    DeHackEd.ProcessBlock(last, data);
                    data.Clear();
                    data.Add(line);
                    last = blockType;
                }
            }
            DeHackEd.ProcessBlock(last, data);
        }

        private static void ProcessBlock(Block type, List<string> data)
        {
            // Ensure the static members are initialized.
            DoomInfo.Strings.PRESSKEY.GetHashCode();

            switch (type)
            {
                case Block.Thing:
                    DeHackEd.ProcessThingBlock(data);
                    break;
                case Block.Frame:
                    DeHackEd.ProcessFrameBlock(data);
                    break;
                case Block.Pointer:
                    DeHackEd.ProcessPointerBlock(data);
                    break;
                case Block.Sound:
                    DeHackEd.ProcessSoundBlock(data);
                    break;
                case Block.Ammo:
                    DeHackEd.ProcessAmmoBlock(data);
                    break;
                case Block.Weapon:
                    DeHackEd.ProcessWeaponBlock(data);
                    break;
                case Block.Cheat:
                    DeHackEd.ProcessCheatBlock(data);
                    break;
                case Block.Misc:
                    DeHackEd.ProcessMiscBlock(data);
                    break;
                case Block.Text:
                    DeHackEd.ProcessTextBlock(data);
                    break;
                case Block.Sprite:
                    DeHackEd.ProcessSpriteBlock(data);
                    break;
            }
        }

        private static void ProcessThingBlock(List<string> data)
        {
            var thingNumber = int.Parse(data[0].Split(' ')[1]) - 1;
            var info = DoomInfo.MobjInfos[thingNumber];
            var dic = DeHackEd.GetKeyValuePairs(data);

            info.DoomEdNum = DeHackEd.GetInt(dic, "ID #", info.DoomEdNum);
            info.SpawnState = (MobjState)DeHackEd.GetInt(dic, "Initial frame", (int)info.SpawnState);
            info.SpawnHealth = DeHackEd.GetInt(dic, "Hit points", info.SpawnHealth);
            info.SeeState = (MobjState)DeHackEd.GetInt(dic, "First moving frame", (int)info.SeeState);
            info.SeeSound = (Sfx)DeHackEd.GetInt(dic, "Alert sound", (int)info.SeeSound);
            info.ReactionTime = DeHackEd.GetInt(dic, "Reaction time", info.ReactionTime);
            info.AttackSound = (Sfx)DeHackEd.GetInt(dic, "Attack sound", (int)info.AttackSound);
            info.PainState = (MobjState)DeHackEd.GetInt(dic, "Injury frame", (int)info.PainState);
            info.PainChance = DeHackEd.GetInt(dic, "Pain chance", info.PainChance);
            info.PainSound = (Sfx)DeHackEd.GetInt(dic, "Pain sound", (int)info.PainSound);
            info.MeleeState = (MobjState)DeHackEd.GetInt(dic, "Close attack frame", (int)info.MeleeState);
            info.MissileState = (MobjState)DeHackEd.GetInt(dic, "Far attack frame", (int)info.MissileState);
            info.DeathState = (MobjState)DeHackEd.GetInt(dic, "Death frame", (int)info.DeathState);
            info.XdeathState = (MobjState)DeHackEd.GetInt(dic, "Exploding frame", (int)info.XdeathState);
            info.DeathSound = (Sfx)DeHackEd.GetInt(dic, "Death sound", (int)info.DeathSound);
            info.Speed = DeHackEd.GetInt(dic, "Speed", info.Speed);
            info.Radius = new Fixed(DeHackEd.GetInt(dic, "Width", info.Radius.Data));
            info.Height = new Fixed(DeHackEd.GetInt(dic, "Height", info.Height.Data));
            info.Mass = DeHackEd.GetInt(dic, "Mass", info.Mass);
            info.Damage = DeHackEd.GetInt(dic, "Missile damage", info.Damage);
            info.ActiveSound = (Sfx)DeHackEd.GetInt(dic, "Action sound", (int)info.ActiveSound);
            info.Flags = (MobjFlags)DeHackEd.GetInt(dic, "Bits", (int)info.Flags);
            info.Raisestate = (MobjState)DeHackEd.GetInt(dic, "Respawn frame", (int)info.Raisestate);
        }

        private static void ProcessFrameBlock(List<string> data)
        {
            var frameNumber = int.Parse(data[0].Split(' ')[1]);
            var info = DoomInfo.States[frameNumber];
            var dic = DeHackEd.GetKeyValuePairs(data);

            info.Sprite = (Sprite)DeHackEd.GetInt(dic, "Sprite number", (int)info.Sprite);
            info.Frame = DeHackEd.GetInt(dic, "Sprite subnumber", info.Frame);
            info.Tics = DeHackEd.GetInt(dic, "Duration", info.Tics);
            info.Next = (MobjState)DeHackEd.GetInt(dic, "Next frame", (int)info.Next);
            info.Misc1 = DeHackEd.GetInt(dic, "Unknown 1", info.Misc1);
            info.Misc2 = DeHackEd.GetInt(dic, "Unknown 2", info.Misc2);
        }

        private static void ProcessPointerBlock(List<string> data)
        {
            var dic = DeHackEd.GetKeyValuePairs(data);
            var start = data[0].IndexOf('(') + 1;
            var end = data[0].IndexOf(')');
            var length = end - start;
            var targetFrameNumber = int.Parse(data[0].Substring(start, length).Split(' ')[1]);
            var sourceFrameNumber = DeHackEd.GetInt(dic, "Codep Frame", -1);
            if (sourceFrameNumber == -1)
            {
                return;
            }
            var info = DoomInfo.States[targetFrameNumber];

            info.PlayerAction = DeHackEd.sourcePointerTable[sourceFrameNumber].Item1;
            info.MobjAction = DeHackEd.sourcePointerTable[sourceFrameNumber].Item2;
        }

        private static void ProcessSoundBlock(List<string> data)
        {
        }

        private static void ProcessAmmoBlock(List<string> data)
        {
            var ammoNumber = int.Parse(data[0].Split(' ')[1]);
            var dic = DeHackEd.GetKeyValuePairs(data);
            var max = DoomInfo.AmmoInfos.Max;
            var clip = DoomInfo.AmmoInfos.Clip;

            max[ammoNumber] = DeHackEd.GetInt(dic, "Max ammo", max[ammoNumber]);
            clip[ammoNumber] = DeHackEd.GetInt(dic, "Per ammo", clip[ammoNumber]);
        }

        private static void ProcessWeaponBlock(List<string> data)
        {
            var weaponNumber = int.Parse(data[0].Split(' ')[1]);
            var info = DoomInfo.WeaponInfos[weaponNumber];
            var dic = DeHackEd.GetKeyValuePairs(data);

            info.Ammo = (AmmoType)DeHackEd.GetInt(dic, "Ammo type", (int)info.Ammo);
            info.UpState = (MobjState)DeHackEd.GetInt(dic, "Deselect frame", (int)info.UpState);
            info.DownState = (MobjState)DeHackEd.GetInt(dic, "Select frame", (int)info.DownState);
            info.ReadyState = (MobjState)DeHackEd.GetInt(dic, "Bobbing frame", (int)info.ReadyState);
            info.AttackState = (MobjState)DeHackEd.GetInt(dic, "Shooting frame", (int)info.AttackState);
            info.FlashState = (MobjState)DeHackEd.GetInt(dic, "Firing frame", (int)info.FlashState);
        }

        private static void ProcessCheatBlock(List<string> data)
        {
        }

        private static void ProcessMiscBlock(List<string> data)
        {
        }

        private static void ProcessTextBlock(List<string> data)
        {
            var split = data[0].Split(' ');
            var length1 = int.Parse(split[1]);
            var length2 = int.Parse(split[2]);

            var line = 1;
            var pos = 0;

            var sb1 = new StringBuilder();
            for (var i = 0; i < length1; i++)
            {
                if (pos == data[line].Length)
                {
                    sb1.Append('\n');
                    line++;
                    pos = 0;
                }
                else
                {
                    sb1.Append(data[line][pos]);
                    pos++;
                }
            }

            var sb2 = new StringBuilder();
            for (var i = 0; i < length2; i++)
            {
                if (pos == data[line].Length)
                {
                    sb2.Append('\n');
                    line++;
                    pos = 0;
                }
                else
                {
                    sb2.Append(data[line][pos]);
                    pos++;
                }
            }

            DoomString.Replace(sb1.ToString(), sb2.ToString());
        }

        private static void ProcessSpriteBlock(List<string> data)
        {
        }

        private static Block GetBlockType(string[] split)
        {
            if (DeHackEd.IsThingBlockStart(split))
            {
                return Block.Thing;
            }
            else if (DeHackEd.IsFrameBlockStart(split))
            {
                return Block.Frame;
            }
            else if (DeHackEd.IsPointerBlockStart(split))
            {
                return Block.Pointer;
            }
            else if (DeHackEd.IsSoundBlockStart(split))
            {
                return Block.Sound;
            }
            else if (DeHackEd.IsAmmoBlockStart(split))
            {
                return Block.Ammo;
            }
            else if (DeHackEd.IsWeaponBlockStart(split))
            {
                return Block.Weapon;
            }
            else if (DeHackEd.IsCheatBlockStart(split))
            {
                return Block.Cheat;
            }
            else if (DeHackEd.IsMiscBlockStart(split))
            {
                return Block.Misc;
            }
            else if (DeHackEd.IsTextBlockStart(split))
            {
                return Block.Text;
            }
            else if (DeHackEd.IsSpriteBlockStart(split))
            {
                return Block.Sprite;
            }
            else
            {
                return Block.None;
            }
        }

        private static bool IsThingBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Thing")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            return true;
        }

        private static bool IsFrameBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Frame")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            return true;
        }

        private static bool IsPointerBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Pointer")
            {
                return false;
            }

            return true;
        }

        private static bool IsSoundBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Sound")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            return true;
        }

        private static bool IsAmmoBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Ammo")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            return true;
        }

        private static bool IsWeaponBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Weapon")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            return true;
        }

        private static bool IsCheatBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Cheat")
            {
                return false;
            }

            if (split[1] != "0")
            {
                return false;
            }

            return true;
        }

        private static bool IsMiscBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Misc")
            {
                return false;
            }

            if (split[1] != "0")
            {
                return false;
            }

            return true;
        }

        private static bool IsTextBlockStart(string[] split)
        {
            if (split.Length < 3)
            {
                return false;
            }

            if (split[0] != "Text")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[2]))
            {
                return false;
            }

            return true;
        }

        private static bool IsSpriteBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (split[0] != "Sprite")
            {
                return false;
            }

            if (!DeHackEd.IsNumber(split[1]))
            {
                return false;
            }

            return true;
        }

        private static bool IsNumber(string value)
        {
            foreach (var ch in value)
            {
                if (!('0' <= ch && ch <= '9'))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, string> GetKeyValuePairs(List<string> data)
        {
            var dic = new Dictionary<string, string>();
            foreach (var line in data)
            {
                var split = line.Split('=');
                if (split.Length == 2)
                {
                    dic[split[0].Trim()] = split[1].Trim();
                }
            }
            return dic;
        }

        private static string GetString(Dictionary<string, string> dic, string key, string defaultValue)
        {
            string value;
            if (dic.TryGetValue(key, out value))
            {
                return value;
            }

            return defaultValue;
        }

        private static int GetInt(Dictionary<string, string> dic, string key, int defaultValue)
        {
            string value;
            if (dic.TryGetValue(key, out value))
            {
                int intValue;
                if (int.TryParse(value, out intValue))
                {
                    return intValue;
                }
            }

            return defaultValue;
        }



        private enum Block
        {
            None,
            Thing,
            Frame,
            Pointer,
            Sound,
            Ammo,
            Weapon,
            Cheat,
            Misc,
            Text,
            Sprite
        }
    }
}
