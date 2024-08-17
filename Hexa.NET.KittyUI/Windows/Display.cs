namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.SDL2;

    public static unsafe class Display
    {
        public static int NumVideoDisplays => SDL.SDLGetNumVideoDisplays();

        public static DisplayMode GetClosestDisplayMode(int displayIndex, DisplayMode mode)
        {
            DisplayMode closest;
            SDL.SDLGetClosestDisplayMode(displayIndex, (SDLDisplayMode*)&mode, (SDLDisplayMode*)&closest);
            return closest;
        }

        public static DisplayMode GetCurrentDisplayMode(int displayIndex)
        {
            DisplayMode mode;
            SDL.SDLGetCurrentDisplayMode(displayIndex, (SDLDisplayMode*)&mode);
            return mode;
        }

        public static DisplayMode GetDesktopDisplayMode(int displayIndex)
        {
            DisplayMode mode;
            SDL.SDLGetDesktopDisplayMode(displayIndex, (SDLDisplayMode*)&mode);
            return mode;
        }

        public static string GetDisplayName(int displayIndex)
        {
            return SDL.SDLGetDisplayNameS(displayIndex);
        }

        public static DisplayOrientation GetDisplayOrientation(int displayIndex)
        {
            return (DisplayOrientation)SDL.SDLGetDisplayOrientation(displayIndex);
        }

        public static void GetDisplayDPI(int displayIndex, ref float ddpi, ref float hdpi, ref float vdpi)
        {
            SDL.SDLGetDisplayDPI(displayIndex, ref ddpi, ref hdpi, ref vdpi);
        }

        public static void GetDisplayBounds(int displayIndex, ref int x, ref int y, ref int width, ref int height)
        {
            SDLRect rectangle;
            SDL.SDLGetDisplayBounds(displayIndex, &rectangle);
            x = rectangle.X;
            y = rectangle.Y;
            width = rectangle.W;
            height = rectangle.H;
        }

        public static void GetDisplayUsableBounds(int displayIndex, ref int x, ref int y, ref int width, ref int height)
        {
            SDLRect rectangle;
            SDL.SDLGetDisplayUsableBounds(displayIndex, &rectangle);
            x = rectangle.X;
            y = rectangle.Y;
            width = rectangle.W;
            height = rectangle.H;
        }

        public static DisplayMode GetDisplayMode(int displayIndex, int modeIndex)
        {
            DisplayMode mode;
            SDL.SDLGetDisplayMode(displayIndex, modeIndex, (SDLDisplayMode*)&mode);
            return mode;
        }

        public static int GetNumDisplayModes(int displayIndex)
        {
            return SDL.SDLGetNumDisplayModes(displayIndex);
        }

        public static int GetPointDisplayIndex(int x, int y)
        {
            var point = new SDLPoint(x, y);
            return SDL.SDLGetPointDisplayIndex(ref point);
        }

        public static int GetRectDisplayIndex(int x, int y, int width, int height)
        {
            var rect = new SDLRect(x, y, width, height);
            return SDL.SDLGetRectDisplayIndex(ref rect);
        }

        public static int GetWindowDisplayIndex(SdlWindow window)
        {
            return SDL.SDLGetWindowDisplayIndex(window.GetWindow());
        }

        public static DisplayMode GetWindowDisplayMode(SdlWindow window)
        {
            DisplayMode mode;
            SDL.SDLGetWindowDisplayMode(window.GetWindow(), (SDLDisplayMode*)&mode);
            return mode;
        }
    }
}