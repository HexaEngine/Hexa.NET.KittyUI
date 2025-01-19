namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.OffScreen;
    using System;

    public static class CefManager
    {
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            CefSettings settings = new()
            {
                LogSeverity = LogSeverity.Verbose,
                CachePath = Path.GetFullPath("cache"),
                WindowlessRenderingEnabled = true
            };
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