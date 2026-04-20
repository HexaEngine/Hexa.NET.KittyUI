namespace Hexa.NET.KittyUI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Microsoft.Extensions.DependencyInjection;

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
}