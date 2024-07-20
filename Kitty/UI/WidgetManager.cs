namespace Kitty.UI
{
    using Kitty.Graphics;

    public static class WidgetManager
    {
        private static IGraphicsDevice? device;
        private static readonly List<IImGuiWindow> widgets = new();

        static WidgetManager()
        {
        }

        public static bool Register<T>(bool show = false) where T : IImGuiWindow, new()
        {
            return Register(new T(), show);
        }

        public static void Unregister<T>() where T : IImGuiWindow, new()
        {
            IImGuiWindow? window = widgets.FirstOrDefault(x => x is T);
            if (window != null)
            {
                if (device != null)
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

            if (device == null)
            {
                widgets.Add(widget);
                return false;
            }
            else
            {
                widget.Init(device);
                widgets.Add(widget);
                return true;
            }
        }

        public static void Init(IGraphicsDevice device)
        {
            WidgetManager.device = device;
            for (int i = 0; i < widgets.Count; i++)
            {
                var widget = widgets[i];
                widget.Init(device);
            }
        }

        public static void Draw(IGraphicsContext context)
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].DrawWindow(context);
            }
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
        }
    }
}