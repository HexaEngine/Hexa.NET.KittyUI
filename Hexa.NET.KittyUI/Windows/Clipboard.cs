﻿namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.SDL2;

    public static unsafe class Clipboard
    {
        public static char* GetClipboardText()
        {
            return (char*)SDL.GetClipboardText();
        }

        public static string GetClipboardTextS()
        {
            return SDL.GetClipboardTextS();
        }

        public static void SetClipboardText(char* text)
        {
            SDL.SetClipboardText((byte*)text);
        }

        public static void SetClipboardText(string text)
        {
            SDL.SetClipboardText(text);
        }

        public static void Free(char* text)
        {
            SDL.Free(text);
        }
    }
}