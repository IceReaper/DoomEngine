﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedDoom
{
    public sealed class SaveMenu : MenuDef
    {
        private string[] name;
        private int[] titleX;
        private int[] titleY;
        private TextBoxMenuItem[] items;

        private int index;
        private TextBoxMenuItem choice;

        private TextInput textInput;

        public SaveMenu(
            DoomMenu menu,
            string name, int titleX, int titleY,
            int firstChoice,
            params TextBoxMenuItem[] items) : base(menu)
        {
            this.name = new[] { name };
            this.titleX = new[] { titleX };
            this.titleY = new[] { titleY };
            this.items = items;

            index = firstChoice;
            choice = items[index];
        }

        public override void Open()
        {
            if (Menu.Application.State != ApplicationState.Game ||
                Menu.Application.Game.State != GameState.Level)
            {
                Menu.NotifySaveFailed();
                return;
            }

            for (var i = 0; i < items.Length; i++)
            {
                items[i].SetText(Menu.SaveSlots[i]);
            }
        }

        private void Up()
        {
            index--;
            if (index < 0)
            {
                index = items.Length - 1;
            }

            choice = items[index];
        }

        private void Down()
        {
            index++;
            if (index >= items.Length)
            {
                index = 0;
            }

            choice = items[index];
        }

        public override bool DoEvent(DoomEvent e)
        {
            if (e.Type != EventType.KeyDown)
            {
                return true;
            }

            if (textInput != null)
            {
                var result = textInput.DoEvent(e);

                if (textInput.State == TextInputState.Canceled)
                {
                    textInput = null;
                }
                else if (textInput.State == TextInputState.Finished)
                {
                    textInput = null;
                }

                if (result)
                {
                    return true;
                }
            }

            if (e.Key == DoomKeys.Up)
            {
                Up();
                Menu.StartSound(Sfx.PSTOP);
            }

            if (e.Key == DoomKeys.Down)
            {
                Down();
                Menu.StartSound(Sfx.PSTOP);
            }

            if (e.Key == DoomKeys.Enter)
            {
                textInput = choice.Edit(() => DoSave(index));
                Menu.StartSound(Sfx.PISTOL);
            }

            if (e.Key == DoomKeys.Escape)
            {
                Menu.Close();
                Menu.StartSound(Sfx.SWTCHX);
            }

            return true;
        }

        private void DoSave(int slotNumber)
        {
            Menu.SaveSlots[slotNumber] = new string(items[slotNumber].Text.ToArray());
            if (Menu.Application.SaveGame(slotNumber, Menu.SaveSlots[slotNumber]))
            {
                Menu.Close();
            }
            else
            {
                Menu.NotifySaveFailed();
            }
            Menu.StartSound(Sfx.PISTOL);
        }

        public IReadOnlyList<string> Name => name;
        public IReadOnlyList<int> TitleX => titleX;
        public IReadOnlyList<int> TitleY => titleY;
        public IReadOnlyList<MenuItem> Items => items;
        public MenuItem Choice => choice;
    }
}