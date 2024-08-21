namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.SDL2;

    public static unsafe class Display
    {
        public static int NumVideoDisplays => SDL.GetNumVideoDisplays();

        public static DisplayMode GetClosestDisplayMode(int displayIndex, DisplayMode mode)
        {
            DisplayMode closest;
            SDL.GetClosestDisplayMode(displayIndex, (SDLDisplayMode*)&mode, (SDLDisplayMode*)&closest);
            return closest;
        }

        public static DisplayMode GetCurrentDisplayMode(int displayIndex)
        {
            DisplayMode mode;
            SDL.GetCurrentDisplayMode(displayIndex, (SDLDisplayMode*)&mode);
            return mode;
        }

        public static DisplayMode GetDesktopDisplayMode(int displayIndex)
        {
            DisplayMode mode;
            SDL.GetDesktopDisplayMode(displayIndex, (SDLDisplayMode*)&mode);
            return mode;
        }

        public static string GetDisplayName(int displayIndex)
        {
            return SDL.GetDisplayNameS(displayIndex);
        }

        public static DisplayOrientation GetDisplayOrientation(int displayIndex)
        {
            return (DisplayOrientation)SDL.GetDisplayOrientation(displayIndex);
        }

        public static void GetDisplayDPI(int displayIndex, ref float ddpi, ref float hdpi, ref float vdpi)
        {
            SDL.GetDisplayDPI(displayIndex, ref ddpi, ref hdpi, ref vdpi);
        }

        public static void GetDisplayBounds(int displayIndex, ref int x, ref int y, ref int width, ref int height)
        {
            SDLRect rectangle;
            SDL.GetDisplayBounds(displayIndex, &rectangle);
            x = rectangle.X;
            y = rectangle.Y;
            width = rectangle.W;
            height = rectangle.H;
        }

        public static void GetDisplayUsableBounds(int displayIndex, ref int x, ref int y, ref int width, ref int height)
        {
            SDLRect rectangle;
            SDL.GetDisplayUsableBounds(displayIndex, &rectangle);
            x = rectangle.X;
            y = rectangle.Y;
            width = rectangle.W;
            height = rectangle.H;
        }

        public static DisplayMode GetDisplayMode(int displayIndex, int modeIndex)
        {
            DisplayMode mode;
            SDL.GetDisplayMode(displayIndex, modeIndex, (SDLDisplayMode*)&mode);
            return mode;
        }

        public static int GetNumDisplayModes(int displayIndex)
        {
            return SDL.GetNumDisplayModes(displayIndex);
        }

        public static int GetPointDisplayIndex(int x, int y)
        {
            var point = new SDLPoint(x, y);
            return SDL.GetPointDisplayIndex(ref point);
        }

        public static int GetRectDisplayIndex(int x, int y, int width, int height)
        {
            var rect = new SDLRect(x, y, width, height);
            return SDL.GetRectDisplayIndex(ref rect);
        }

        public static int GetWindowDisplayIndex(CoreWindow window)
        {
            return SDL.GetWindowDisplayIndex(window.GetWindow());
        }

        public static DisplayMode GetWindowDisplayMode(CoreWindow window)
        {
            DisplayMode mode;
            SDL.GetWindowDisplayMode(window.GetWindow(), (SDLDisplayMode*)&mode);
            return mode;
        }
    }
}