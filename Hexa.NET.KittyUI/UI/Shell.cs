namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.KittyUI.Windows.Events;
    using Hexa.NET.Mathematics;
    using System.Numerics;

    public static class ImGuiExtensions
    {
        public static bool Contains(this ImRect rect, Vector2 pos)
        {
            return rect.Min.X <= pos.X && rect.Max.X >= pos.X && rect.Min.Y <= pos.Y && rect.Max.Y >= pos.Y;
        }

        public static bool Contains(this ImRect rect, Point2 pos)
        {
            return (int)rect.Min.X < pos.X && (int)rect.Max.X >= pos.X && (int)rect.Min.Y < pos.Y && (int)rect.Max.Y >= pos.Y;
        }

        public static Vector2 Size(this ImRect rect)
        {
            return rect.Max - rect.Min;
        }

        public static Vector2 Midpoint(this ImRect rect)
        {
            var size = rect.Max - rect.Min;
            return rect.Min + size / 2f;
        }
    }

    public class CloseWindowRequest : RoutedEventArgs
    {
        public CloseWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }

    public class MaximizeWindowRequest : RoutedEventArgs
    {
        public MaximizeWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }

    public class MinimizeWindowRequest : RoutedEventArgs
    {
        public MinimizeWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }

    public class RestoreWindowRequest : RoutedEventArgs
    {
        public RestoreWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }

    public class Shell : ImWindow
    {
        private readonly NavigationManager navigation = new();

        public Shell(string name)
        {
            IsEmbedded = true;
            Name = name;
        }

        protected override string Name { get; }

        public INavigation Navigation => navigation;

        public override void DrawContent()
        {
            bool first = true;
            var avail = ImGui.GetContentRegionAvail();
            var cur = ImGui.GetCursorScreenPos();
            var draw = ImGui.GetWindowDrawList();
            var pad = ImGui.GetStyle().WindowPadding;
            var lineHeight = ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2;
            draw.AddRectFilled(cur - pad, cur + new Vector2(avail.X, lineHeight) + pad, 0xFF3c3c3c);
            var before = ImGui.GetCursorPos();
            IPage? pageTarget = null;
            foreach (var item in navigation.GetHistoryStack())
            {
                if (!first)
                {
                    ImGui.SameLine();
                    ImGui.Text(">"u8);
                    ImGui.SameLine();
                }
                first = false;
                if (ImGuiButton.TransparentButton(item.Title))
                {
                    pageTarget = item;
                }
            }

            if (pageTarget != null)
            {
                navigation.NavigateBackTo(pageTarget);
            }

            ImGui.SetCursorPos(before);
            ImGui.ItemSizeRect(new(cur - pad, cur + new Vector2(avail.X, lineHeight) + pad), 0);

            navigation.CurrentPage?.DrawPage(ImGuiWindowFlags.None);
        }

        public void RegisterPage(string path, IPage page)
        {
            navigation.RegisterPage(path, page);
        }

        public void SetRootPage(string path)
        {
            navigation.SetRootPage(path);
        }
    }
}