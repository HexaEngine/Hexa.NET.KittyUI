namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using System.Numerics;

    public class Shell : ImWindow
    {
        private readonly NavigationManager navigation = new();
        private bool showMenu;

        public Shell(string name)
        {
            IsEmbedded = true;
            Name = name;
            navigation.OpenMenu += OpenMenu;
        }

        private void OpenMenu(object? sender, EventArgs e)
        {
            showMenu = !showMenu;
        }

        public override string Name { get; }

        public INavigation Navigation => navigation;

        public override void DrawContent()
        {
            var viewport = ImGui.GetWindowViewport();

            if (showMenu)
            {
                if (ImGui.Begin("ShellMenuPopup", ImGuiWindowFlags.NoDecoration))
                {
                    ImGui.SetWindowPos(viewport.WorkPos);
                    ImGui.SetWindowSize(new Vector2(0, viewport.WorkSize.Y));
                    ImGui.Text("Hello world");

                    if (!ImGui.IsWindowFocused())
                    {
                        showMenu = false;
                    }
                }

                ImGui.End();
            }

            ImGui.BeginDisabled(showMenu);
            navigation.CurrentPage?.DrawPage(ImGuiWindowFlags.None);
            ImGui.EndDisabled();
        }

        public void RegisterPage(string path, IPage page)
        {
            navigation.RegisterPage(path, page);
        }

        public void SetRootPage(string path)
        {
            navigation.SetRootPage(path);
        }

        public override void Dispose()
        {
            navigation.OpenMenu -= OpenMenu;
        }
    }
}