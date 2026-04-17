namespace Hexa.NET.KittyUI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.KittyUI.UI;
    using Hexa.NET.KittyUI.Windows;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public delegate void ImGuiFontBuilderCallback(ImGuiFontBuilder builder);

    public delegate void ImGuiStyleBuilderCallback(ImGuiStylePtr style);

    public delegate void ImGuiConfigureCallback(ImGuiContextPtr context, ImGuiIOPtr io);

    public unsafe class AppBuilder
    {
        internal readonly ServiceCollection services = new();
        private readonly AppHostOptions appHostOptions = new();

        public AppBuilder()
        {
        }

        public static AppBuilder Create()
        {
            return new AppBuilder();
        }

        public ServiceCollection Services => services;

        public AppBuilder EnableSubSystem(SubSystems subSystem)
        {
            appHostOptions.SubSystems |= subSystem;
            return this;
        }

        public AppBuilder DisableSubSystem(SubSystems subSystem)
        {
            appHostOptions.SubSystems &= ~subSystem;
            return this;
        }

        public AppBuilder SetLogFolder(string folder)
        {
            Application.LogFolder = folder;
            return this;
        }

        public AppBuilder EnableLogging(bool enabled)
        {
            Application.LoggingEnabled = enabled;
            return this;
        }

        public AppBuilder EnableDebugTools(bool enabled)
        {
            ImGuiDebugTools.Enabled = enabled;
            return this;
        }

        public AppBuilder SetGraphicsBackend(GraphicsBackend backend)
        {
            Application.SelectedGraphicsBackend = backend;
            return this;
        }

        public AppBuilder AddImGuiAddon(ImGuiAddon addon)
        {
            ImGuiManager.AddAddon(addon);
            return this;
        }

        public AppBuilder AddDefaultFont()
        {
            appHostOptions.AddDefaultFont();
            return this;
        }

        public AppBuilder AddFont(ImGuiFontBuilderCallback action)
        {
            appHostOptions.FontBuilders.Add((action, null));
            return this;
        }

        public AppBuilder AddFont(string alias, ImGuiFontBuilderCallback action)
        {
            appHostOptions.FontBuilders.Add((action, alias));
            return this;
        }

        public AppBuilder Style(ImGuiStyleBuilderCallback action)
        {
            appHostOptions.StyleBuilders.Add(action);
            return this;
        }

        public AppBuilder ImGuiConfigure(ImGuiConfigureCallback callback)
        {
            appHostOptions.ConfigureCallbacks.Add(callback);
            return this;
        }

        /// <summary>
        /// Sets the style to buildin custom style.
        /// </summary>
        /// <returns></returns>
        public AppBuilder StyleDefault()
        {
            appHostOptions.StyleBuilders.Add(x => ImGuiManager.StyleKitty());
            return this;
        }

        /// <summary>
        /// Sets the style to <see cref="ImGui.StyleColorsDark()"/>
        /// </summary>
        /// <returns></returns>
        public AppBuilder StyleColorsDark()
        {
            appHostOptions.StyleBuilders.Add(x => ImGui.StyleColorsDark());
            return this;
        }

        /// <summary>
        /// Sets the style to <see cref="ImGui.StyleColorsLight()"/>
        /// </summary>
        /// <returns></returns>
        public AppBuilder StyleColorsLight()
        {
            appHostOptions.StyleBuilders.Add(x => ImGui.StyleColorsLight());
            return this;
        }

        /// <summary>
        /// Sets the style to <see cref="ImGui.StyleColorsClassic()"/>
        /// </summary>
        /// <returns></returns>
        public AppBuilder StyleColorsClassic()
        {
            appHostOptions.StyleBuilders.Add(x => ImGui.StyleColorsClassic());
            return this;
        }

        public AppHost Build()
        {
            AppHost host = new(appHostOptions, services);
            return host;
        }
    }

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

    public class AppHost : IDisposable
    {
        private readonly AppHostOptions options;
        private readonly ServiceProvider serviceProvider;
        private string title = "Kitty";
        private TitleBar? titlebar;

        public AppHost(AppHostOptions options, ServiceCollection services)
        {
            this.options = options;
            this.serviceProvider = services.BuildServiceProvider();
        }

        public AppHostOptions Options => options;

        public IServiceProvider Services => serviceProvider;

        public T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        {
            return ActivatorUtilities.CreateInstance<T>(serviceProvider);
        }

        public AppHost AddWindow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : IImGuiWindow
        {
            var window = CreateInstance<T>();
            window.Show();
            return this;
        }

        public AppHost AddWindow(IImGuiWindow window)
        {
            window.Show();
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

            public override string Name => name;

            public override unsafe void DrawWindow(ImGuiWindowFlags overwriteFlags)
            {
                ImGuiWindowClass windowClass;
                windowClass.DockNodeFlagsOverrideSet = (ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoTabBar;
                ImGui.SetNextWindowClass(&windowClass);
                ImGuiP.DockBuilderDockWindow(Name, WidgetManager.DockSpaceId);

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

        public AppHost AddWindow(string name, Action draw)
        {
            WrappedWindow window = new(name, draw);
            window.Show();
            return this;
        }

        public AppHost UseAppShell(string title, Action<ShellBuilder> action)
        {
            ShellBuilder builder = new(this, title);
            action(builder);
            builder.Shell.Show();
            SetTitle(title);

            builder.Shell.Navigation.NavigateToRoot();

            if (titlebar != null)
            {
                titlebar.Navigation = builder.Shell.Navigation;
            }

            return this;
        }

        public AppHost SetTitle(string title)
        {
            this.title = title;
            return this;
        }

        public AppHost UseTitleBar(TitleBar titlebar)
        {
            this.titlebar = titlebar;
            return this;
        }

        public AppHost UseTitleBar<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : TitleBar
        {
            T titlebar = CreateInstance<T>();
            return UseTitleBar(titlebar);
        }

        public void Run(IRenderWindow window)
        {
            window.Title = title;
            window.TitleBar = titlebar;
            Application.Run(window, this);
        }

        public void Run<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : IRenderWindow
        {
            var window = CreateInstance<T>();
            window.Title = title;
            window.TitleBar = titlebar;
            Run(window);
        }

        public void Run()
        {
            Window window = new()
            {
                Title = title,
                TitleBar = titlebar,
            };
            Run(window);
        }

        private void Run(Window window)
        {
            Application.SubSystems = options.SubSystems;
            Application.Run(window, this);
        }

        public void Dispose()
        {
            serviceProvider.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}