namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.OffScreen;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.Windows;
    using System;

    public static class CefManager
    {
        private static bool initialized;

        public static RenderHandlerBase CreateRenderer()
        {
            return Application.GraphicsBackend switch
            {
                GraphicsBackend.D3D11 => new D3D11RenderHandler(),
                GraphicsBackend.OpenGL => new OpenGLRenderHandler(),
                _ => throw new PlatformNotSupportedException(),
            };
        }

        public static BrowserSettings GetDefaultBrowserSettings()
        {
            var mode = Display.GetDesktopDisplayMode(0);
            return new BrowserSettings
            {
                WindowlessFrameRate = mode.RefreshRate,
                Javascript = CefState.Enabled,
                WebGl = CefState.Enabled,
                Databases = CefState.Enabled,
                LocalStorage = CefState.Enabled
            };
        }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            CefSettings settings = new()
            {
                LogSeverity = LogSeverity.Disable,
                CachePath = Path.GetFullPath("cefcache"),
                WindowlessRenderingEnabled = true,
                RemoteDebuggingPort = 0,
            };

            settings.CefCommandLineArgs.Add("disable-breakpad", "1");
            settings.CefCommandLineArgs.Add("disable-metrics", "1"); 
            settings.CefCommandLineArgs.Add("disable-metrics-reporting", "1"); 

            settings.EnableAudio();
            bool success = Cef.Initialize(settings);
            if (!success)
            {
                throw new InvalidOperationException("Failed to initialize CEF.");
            }

            Application.Exiting += Exiting;

            initialized = true;
        }

        private static void Exiting()
        {
            Cef.Shutdown();
        }
    }
}