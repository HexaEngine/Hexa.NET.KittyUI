namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.SDL2;

    public static unsafe class Clipboard
    {
        public static char* GetClipboardText()
        {
            return (char*)SDL.SDLGetClipboardText();
        }

        public static string GetClipboardTextS()
        {
            return SDL.SDLGetClipboardTextS();
        }

        public static void SetClipboardText(char* text)
        {
            SDL.SDLSetClipboardText((byte*)text);
        }

        public static void SetClipboardText(string text)
        {
            SDL.SDLSetClipboardText(text);
        }

        public static void Free(char* text)
        {
            SDL.SDLFree(text);
        }
    }
}