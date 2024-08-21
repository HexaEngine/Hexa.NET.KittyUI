namespace Hexa.NET.KittyUI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.KittyUI.UI;
    using Hexa.NET.KittyUI.Windows;
    using System;
    using System.Collections.Generic;

    public delegate void ImGuiFontBuilderCallback(ImGuiFontBuilder builder);

    public delegate void ImGuiStyleBuilderCallback(ImGuiStylePtr style);

    public unsafe class AppBuilder
    {
        internal readonly List<(ImGuiFontBuilderCallback, string? alias)> fontBuilders = new();
        internal readonly List<ImGuiFontBuilder> builders = new();
        internal readonly List<ImGuiStyleBuilderCallback> styleBuilders = [];
        private string title = "Kitty";
        private TitleBar? titlebar;

        public void Run()
        {
            Window window = new()
            {
                Title = title,
                TitleBar = titlebar,
            };
            Application.Run(window, this);
        }

        public AppBuilder SetTitle(string title)
        {
            this.title = title;
            return this;
        }

        public AppBuilder AddTitleBar(TitleBar titlebar)
        {
            this.titlebar = titlebar;
            return this;
        }

        public AppBuilder AddTitleBar<T>() where T : TitleBar, new()
        {
            T titlebar = new();
            return AddTitleBar(titlebar);
        }

        internal void BuildFonts(ImGuiIOPtr io, Dictionary<string, ImFontPtr> aliasToFont)
        {
            if (fontBuilders.Count == 0)
            {
                AddDefaultFont();
            }

            for (int i = 0; i < fontBuilders.Count; i++)
            {
                (ImGuiFontBuilderCallback fontBuilder, string? alias) = fontBuilders[i];
                ImGuiFontBuilder builder = new(io.Fonts);
                fontBuilder(builder);

                if (alias != null)
                {
                    aliasToFont.Add(alias, builder.Font);
                }
                builders.Add(builder);
            }

            io.Fonts.Build();
        }

        internal void BuildStyle(ImGuiStylePtr style)
        {
            if (styleBuilders.Count == 0)
            {
                StyleDefault();
            }

            for (int i = 0; i < styleBuilders.Count; i++)
            {
                styleBuilders[i](style);
            }
        }

        internal void Dispose()
        {
            for (int i = 0; i < builders.Count; i++)
            {
                builders[i].Destroy();
            }
            builders.Clear();
        }

        public AppBuilder AddDefaultFont()
        {
            fontBuilders.Add((DefaultCallback, null));
            return this;
        }

        private void DefaultCallback(ImGuiFontBuilder builder)
        {
            Span<char> glyphMaterialRanges =
            [
                    (char)0xe003, (char)0xF8FF,
                    (char)0, (char)0 // null terminator
            ];
            builder.AddFontFromEmbeddedResource("Hexa.NET.KittyUI.assets.fonts.arial.ttf", 15);
            builder.SetOption(conf => conf.GlyphMinAdvanceX = 16);
            builder.SetOption(conf => conf.GlyphOffset = new(0, 2));
            builder.AddFontFromEmbeddedResource("Hexa.NET.KittyUI.assets.fonts.MaterialSymbolsRounded.ttf", 18, glyphMaterialRanges);
        }

        public AppBuilder AddFont(ImGuiFontBuilderCallback action)
        {
            fontBuilders.Add((action, null));
            return this;
        }

        public AppBuilder AddFont(string alias, ImGuiFontBuilderCallback action)
        {
            fontBuilders.Add((action, alias));
            return this;
        }

        public AppBuilder Style(ImGuiStyleBuilderCallback action)
        {
            styleBuilders.Add(action);
            return this;
        }

        public AppBuilder StyleDefault()
        {
            styleBuilders.Add(x => ImGuiManager.StyleKitty());
            return this;
        }

        public AppBuilder AddWindow<T>(bool show = false, bool mainWindow = false) where T : IImGuiWindow, new()
        {
            WidgetManager.Register<T>(show, mainWindow);
            return this;
        }

        public AppBuilder AddWindow(IImGuiWindow window, bool show = false, bool mainWindow = false)
        {
            WidgetManager.Register(window, show, mainWindow);
            return this;
        }

        public AppBuilder UseAppShell(string title, Action<ShellBuilder> action)
        {
            ShellBuilder builder = new(title);
            action(builder);
            WidgetManager.Register(builder.Shell, true, true);
            SetTitle(title);

            builder.Shell.Navigation.NavigateToRoot();

            if (titlebar != null)
            {
                titlebar.Navigation = builder.Shell.Navigation;
            }

            return this;
        }

        public class WrappedWindow : ImWindow
        {
            private readonly string name;
            private readonly Action draw;

            public WrappedWindow(string name, Action draw)
            {
                this.name = name;
                this.draw = draw;
                IsShown = true;
                Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration;
            }

            protected override string Name => name;

            public override unsafe void DrawWindow(ImGuiWindowFlags overwriteFlags)
            {
                ImGuiWindowClass windowClass;
                windowClass.DockNodeFlagsOverrideSet = (ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoTabBar;
                ImGui.SetNextWindowClass(&windowClass);
                ImGui.DockBuilderDockWindow(Name, WidgetManager.DockSpaceId);

                var flags = Flags | overwriteFlags;

                if (!IsShown) return;
                if (!ImGui.Begin(Name, flags))
                {
                    ImGui.End();
                    return;
                }

                windowEnded = false;

                DrawContent();

                if (!windowEnded)
                    ImGui.End();
            }

            public override void DrawContent()
            {
                draw();
            }
        }

        public AppBuilder AddWindow(string name, Action window)
        {
            WidgetManager.Register(new WrappedWindow(name, window));
            return this;
        }
    }
}