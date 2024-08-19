namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;
    using HexaGen.Runtime.COM;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using static Hexa.NET.SDL2.SDL;

    [SupportedOSPlatform("windows")]
    public static class WindowsAPI
    {
        public const int SmCxScreen = 0; // Width of the screen
        public const int SmCyScreen = 1; // Height of the screen

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);
    }

    [SupportedOSPlatform("windows")]
    public unsafe class WindowTransitionAnimation
    {
        public static void AnimateWindowMinimize(SDLWindow* sdlWindow, nint hwnd, int duration)
        {
            int windowX, windowY, windowWidth, windowHeight;
            SDLGetWindowPosition(sdlWindow, &windowX, &windowY);
            SDLGetWindowSize(sdlWindow, &windowWidth, &windowHeight);

            Rectangle taskbarRect = Taskbar.GetAppRect(hwnd);
            int targetX, targetY;

            // Calculate target position (adjust based on the taskbar position)
            if (taskbarRect.Bottom == WindowsAPI.GetSystemMetrics(WindowsAPI.SmCxScreen)) // Bottom taskbar
            {
                targetX = taskbarRect.Left + (taskbarRect.Right - taskbarRect.Left) / 2;
                targetY = taskbarRect.Bottom;
            }
            else if (taskbarRect.Top == 0) // Top taskbar
            {
                targetX = taskbarRect.Left + (taskbarRect.Right - taskbarRect.Left) / 2;
                targetY = taskbarRect.Top;
            }
            else if (taskbarRect.Right == WindowsAPI.GetSystemMetrics(WindowsAPI.SmCxScreen)) // Right taskbar
            {
                targetX = taskbarRect.Right;
                targetY = taskbarRect.Top + (taskbarRect.Bottom - taskbarRect.Top) / 2;
            }
            else // Left taskbar
            {
                targetX = taskbarRect.Left;
                targetY = taskbarRect.Top + (taskbarRect.Bottom - taskbarRect.Top) / 2;
            }

            for (int i = 0; i < duration; i += 10)
            {
                float factor = (float)i / duration;

                int newX = windowX + (int)((targetX - windowX) * factor);
                int newY = windowY + (int)((targetY - windowY) * factor);
                int newWidth = (int)(windowWidth * (1 - factor));
                int newHeight = (int)(windowHeight * (1 - factor));

                SDLSetWindowPosition(sdlWindow, newX, newY);
                SDLSetWindowSize(sdlWindow, newWidth, newHeight);

                Thread.Sleep(10);
            }

            SDLMinimizeWindow(sdlWindow); // Finally, hide the window
        }
    }
}