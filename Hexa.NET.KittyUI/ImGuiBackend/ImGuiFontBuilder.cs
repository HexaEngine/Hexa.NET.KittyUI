namespace Hexa.NET.KittyUI.ImGuiBackend
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Utilities;
    using System.IO;
    using System.Reflection;

    public unsafe struct GlyphRanges
    {
        private UnsafeList<uint> glyphs;

        public GlyphRanges(params uint[] glyphs)
        {
            this.glyphs = new UnsafeList<uint>(glyphs);
        }

        public GlyphRanges(ReadOnlySpan<uint> glyphs)
        {
            this.glyphs = new UnsafeList<uint>(glyphs.Length);
            for (int i = 0; i < glyphs.Length; i++)
            {
                this.glyphs.Add(glyphs[i]);
            }
        }

        public void AddGlyph(uint glyph)
        {
            glyphs.Add(glyph);
        }

        public uint* GetRanges()
        {
            return glyphs.Data;
        }

        public void Release()
        {
            glyphs.Release();
        }
    }

    public unsafe class ImGuiFontBuilder
    {
        private ImFontAtlasPtr fontAtlas;
        private ImFontConfigPtr config;
        private ImFontPtr font;
        private readonly List<GlyphRanges> ranges = [];

        public ImGuiFontBuilder(ImFontAtlasPtr fontAtlasPtr)
        {
            config = ImGui.ImFontConfig();
            config.FontDataOwnedByAtlas = false;
            fontAtlas = fontAtlasPtr;
        }

        public ImFontConfigPtr Config => config;

        public ImFontPtr Font => font;

        public ImGuiFontBuilder SetOption(Action<ImFontConfigPtr> action)
        {
            action(config);
            return this;
        }

        public ImGuiFontBuilder AddDefaultFont()
        {
            font = fontAtlas.AddFontDefault();
            config.MergeMode = true;
            return this;
        }

        public ImGuiFontBuilder AddFontFromFileTTF(string path, float size, GlyphRanges glyphRanges)
        {
            ranges.Add(glyphRanges);
            return AddFontFromFileTTF(path, size, glyphRanges.GetRanges());
        }

        public ImGuiFontBuilder AddFontFromFileTTF(string path, float size, ReadOnlySpan<uint> glyphRanges)
        {
            return AddFontFromFileTTF(path, size, new GlyphRanges(glyphRanges));
        }

        public ImGuiFontBuilder AddFontFromFileTTF(string path, float size, uint* glyphRanges)
        {
            font = fontAtlas.AddFontFromFileTTF(Path.GetFullPath(path), size, config, glyphRanges);
            config.MergeMode = true;
            return this;
        }

        public ImGuiFontBuilder AddFontFromFileTTF(string path, float size)
        {
            var fullpath = Path.GetFullPath(path);
            bool exists = File.Exists(fullpath);
            if (!exists)
            {
                throw new FileNotFoundException($"Font file not found: {fullpath}");
            }
            font = fontAtlas.AddFontFromFileTTF(fullpath, size, config);
            config.MergeMode = true;
            return this;
        }

        public ImGuiFontBuilder AddFontFromEmbeddedResource(string path, float size)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path) ?? throw new FileNotFoundException($"Embedded resource not found: {path}");
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            fixed (byte* pFontData = buffer)
            {
                font = fontAtlas.AddFontFromMemoryTTF(pFontData, buffer.Length, size, config);
                config.MergeMode = true;
            }
            return this;
        }

        public ImGuiFontBuilder AddFontFromEmbeddedResource(string path, float size, ReadOnlySpan<uint> glyphRanges)
        {
            return AddFontFromEmbeddedResource(path, size, new GlyphRanges(glyphRanges));
        }

        public ImGuiFontBuilder AddFontFromEmbeddedResource(string path, float size, GlyphRanges glyphRanges)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path) ?? throw new FileNotFoundException($"Embedded resource not found: {path}");
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            ranges.Add(glyphRanges);
            fixed (byte* pFontData = buffer)
            {
                font = fontAtlas.AddFontFromMemoryTTF(pFontData, buffer.Length, size, config, glyphRanges.GetRanges());
                config.MergeMode = true;
            }
            return this;
        }

        public ImGuiFontBuilder AddFontFromMemoryTTF(byte* fontData, int fontDataSize, float size)
        {
            // IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
            // This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
            font = fontAtlas.AddFontFromMemoryTTF(fontData, fontDataSize, size, config);
            config.MergeMode = true;
            return this;
        }

        public ImGuiFontBuilder AddFontFromMemoryTTF(ReadOnlySpan<byte> fontData, float size, ReadOnlySpan<uint> glyphRanges)
        {
            fixed (byte* pFontData = fontData)
            {
                fixed (uint* pGlyphRanges = glyphRanges)
                {
                    return AddFontFromMemoryTTF(pFontData, fontData.Length, size, pGlyphRanges);
                }
            }
        }

        public ImGuiFontBuilder AddFontFromMemoryTTF(byte* fontData, int fontDataSize, float size, ReadOnlySpan<uint> glyphRanges)
        {
            fixed (uint* pGlyphRanges = glyphRanges)
                return AddFontFromMemoryTTF(fontData, fontDataSize, size, pGlyphRanges);
        }

        public ImGuiFontBuilder AddFontFromMemoryTTF(byte* fontData, int fontDataSize, float size, uint* pGlyphRanges)
        {
            // IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
            // This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
            font = fontAtlas.AddFontFromMemoryTTF(fontData, fontDataSize, size, config, pGlyphRanges);

            return this;
        }

        public void Destroy()
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                ranges[i].Release();
            }
            config.Destroy();
            config = default;
            fontAtlas = default;
        }
    }
}