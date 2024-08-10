namespace Hexa.NET.Kitty
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.Kitty.ImGuiBackend;
    using Hexa.NET.Kitty.UI;
    using System;
    using System.Collections.Generic;

    public delegate void ImGuiFontBuilderCallback(ImGuiFontBuilder builder);

    public unsafe class AppBuilder
    {
        internal readonly List<(ImGuiFontBuilderCallback, string? alias)> fontBuilders = new();
        internal readonly List<ImGuiFontBuilder> builders = new();

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

        internal void Dispose()
        {
            for (int i = 0; i < builders.Count; i++)
            {
                builders[i].Destroy();
            }
            builders.Clear();
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
            builder.AddFontFromFileTTF("./assets/fonts/arial.ttf", 15);
            builder.SetOption(conf => conf.GlyphMinAdvanceX = 16);
            builder.SetOption(conf => conf.GlyphOffset = new(0, 2));
            builder.AddFontFromFileTTF("./assets/fonts/MaterialSymbolsRounded.ttf", 18, glyphMaterialRanges);
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
    }
}