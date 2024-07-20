namespace Kitty
{
    using Hexa.NET.ImGui;
    using Kitty.Graphics;
    using Kitty.ImGuiBackend;
    using Kitty.UI;
    using System;
    using System.Collections.Generic;

    public class AppBuilder
    {
        internal readonly List<Action<ImGuiFontBuilder>> fontBuilder = new();

        internal void BuildFonts(ImGuiIOPtr io)
        {
            if (fontBuilder.Count == 0)
            {
                io.Fonts.AddFontDefault();
            }

            for (int i = 0; i < fontBuilder.Count; i++)
            {
                ImGuiFontBuilder builder = new(io.Fonts);
                fontBuilder[i](builder);
                builder.Destroy();
            }
        }

        public AppBuilder AddWindow<T>() where T : IImGuiWindow, new()
        {
            WidgetManager.Register<T>();
            return this;
        }

        public AppBuilder AddWindow(IImGuiWindow window)
        {
            WidgetManager.Register(window);
            return this;
        }

        public class WrappedWindow : ImWindow
        {
            private readonly string name;
            private readonly Action<IGraphicsContext> draw;

            public WrappedWindow(string name, Action<IGraphicsContext> draw)
            {
                this.name = name;
                this.draw = draw;
                IsShown = true;
                Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration;
            }

            protected override string Name => name;

            public override unsafe void DrawWindow(IGraphicsContext context)
            {
                ImGuiWindowClass windowClass;
                windowClass.DockNodeFlagsOverrideSet = (ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoTabBar;
                ImGui.SetNextWindowClass(&windowClass);
                ImGui.DockBuilderDockWindow(Name, ImGuiManager.DockSpaceId);

                if (!IsShown) return;
                if (!ImGui.Begin(Name, Flags))
                {
                    ImGui.End();
                    return;
                }

                windowEnded = false;

                DrawContent(context);

                if (!windowEnded)
                    ImGui.End();
            }

            public override void DrawContent(IGraphicsContext context)
            {
                draw(context);
            }
        }

        public AppBuilder AddWindow(string name, Action<IGraphicsContext> window)
        {
            WidgetManager.Register(new WrappedWindow(name, window));
            return this;
        }

        public AppBuilder AddFont(Action<ImGuiFontBuilder> action)
        {
            fontBuilder.Add(action);
            return this;
        }
    }
}