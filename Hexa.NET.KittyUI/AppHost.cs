namespace Hexa.NET.KittyUI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.UI;
    using Hexa.NET.KittyUI.Windows;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Diagnostics.CodeAnalysis;

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