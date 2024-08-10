﻿namespace Hexa.NET.Kitty.UI
{
    using Hexa.NET.ImGui;

    public static class WidgetManager
    {
        private static bool initialized;
        private static readonly List<IImGuiWindow> widgets = new();

        static WidgetManager()
        {
        }

        public static bool BlockInput { get; set; }

        public static bool Register<T>(bool show = false) where T : IImGuiWindow, new()
        {
            return Register(new T(), show);
        }

        public static void Unregister<T>() where T : IImGuiWindow, new()
        {
            IImGuiWindow? window = widgets.FirstOrDefault(x => x is T);
            if (window != null)
            {
                if (initialized)
                {
                    window.Dispose();
                }

                widgets.Remove(window);
            }
        }

        public static bool Register(IImGuiWindow widget, bool show = false)
        {
            if (show)
            {
                widget.Show();
            }

            if (!initialized)
            {
                widgets.Add(widget);
                return false;
            }
            else
            {
                widget.Init();
                widgets.Add(widget);
                return true;
            }
        }

        public static void Init()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                var widget = widgets[i];
                widget.Init();
            }
            initialized = true;
        }

        public static void Draw()
        {
            ImGui.BeginDisabled(BlockInput);
            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].DrawWindow();
            }
            ImGui.EndDisabled();
        }

        public static unsafe void DrawMenu()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].DrawMenu();
            }
        }

        public static void Dispose()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].Dispose();
            }
            widgets.Clear();
            initialized = false;
        }
    }
}