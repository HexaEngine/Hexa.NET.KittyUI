namespace Hexa.NET.KittyUI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.KittyUI.UI;
    using System;
    using System.Collections.Generic;

    public class AppHostOptions : IDisposable
    {
        public List<(ImGuiFontBuilderCallback, string? alias)> FontBuilders { get; } = [];

        public List<ImGuiFontBuilder> Fonts { get; } = [];

        public List<ImGuiStyleBuilderCallback> StyleBuilders { get; } = [];

        public List<ImGuiConfigureCallback> ConfigureCallbacks { get; } = [];

        public string Title { get; set; } = "Kitty";

        public TitleBar? Titlebar { get; set; }

        public SubSystems SubSystems { get; set; }

        private void DefaultCallback(ImGuiFontBuilder builder)
        {
            Span<uint> glyphMaterialRanges =
            [
                    0xe003, 0xF8FF,
                    0, 0 // null terminator
            ];
            builder.AddFontFromEmbeddedResource("Hexa.NET.KittyUI.assets.fonts.arial.ttf", 15);
            builder.SetOption(conf => conf.GlyphMinAdvanceX = 16);
            builder.SetOption(conf => conf.GlyphOffset = new(0, 2));
            builder.AddFontFromEmbeddedResource("Hexa.NET.KittyUI.assets.fonts.MaterialSymbolsRounded.ttf", 18, glyphMaterialRanges);
        }

        public void AddDefaultFont()
        {
            FontBuilders.Add((DefaultCallback, null));
        }

        public void BuildFonts(ImGuiIOPtr io, Dictionary<string, ImFontPtr> aliasToFont)
        {
            if (FontBuilders.Count == 0)
            {
                AddDefaultFont();
            }

            for (int i = 0; i < FontBuilders.Count; i++)
            {
                (ImGuiFontBuilderCallback fontBuilder, string? alias) = FontBuilders[i];
                ImGuiFontBuilder builder = new(io.Fonts);
                fontBuilder(builder);

                if (alias != null)
                {
                    aliasToFont.Add(alias, builder.Font);
                }
                Fonts.Add(builder);
            }
        }

        /// <summary>
        /// Sets the style to buildin custom style.
        /// </summary>
        /// <returns></returns>
        public void StyleDefault()
        {
            StyleBuilders.Add(x => ImGuiManager.StyleKitty());
        }

        public void BuildStyle(ImGuiStylePtr style)
        {
            if (StyleBuilders.Count == 0)
            {
                StyleDefault();
            }

            for (int i = 0; i < StyleBuilders.Count; i++)
            {
                StyleBuilders[i](style);
            }
        }

        public void BuildImGuiConfig(ImGuiContextPtr context, ImGuiIOPtr io)
        {
            foreach (var callback in ConfigureCallbacks)
            {
                callback(context, io);
            }
        }

        public void Dispose()
        {
            foreach (var font in Fonts)
            {
                font.Destroy();
            }
            Fonts.Clear();
            FontBuilders.Clear();
            StyleBuilders.Clear();
            ConfigureCallbacks.Clear();
            Titlebar = null;
            GC.SuppressFinalize(this);
        }
    }
}