// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.WebView;
using System.Numerics;
using TestApp;

Application.GraphicsDebugging = true;
AppBuilder appBuilder = new();
appBuilder.EnableLogging(true);
appBuilder.EnableDebugTools(true);
appBuilder.SetTitle("Test App");
//appBuilder.SetGraphicsBackend(Hexa.NET.KittyUI.Graphics.GraphicsBackend.OpenGL);
appBuilder.AddWindow<MainWindow>();
appBuilder.StyleColorsDark();
appBuilder.Run();

namespace TestApp
{
    public class MainWindow : ImWindow
    {
        public override string Name => "Main Window";

        public override void Init()
        {

        }

        public override void DrawContent()
        {
            ImGui.Text("Hello, World!");
            if (ImGui.Button("Open"))
            {
                new Browser().Show();
            }
        }
    }

    public class Browser : ImWindow
    {
        private WebView view = null!;

        public override string Name => "Browser";

        public override void Init()
        {
            view = new("https://google.com")
            {
                Size = new(1280, 720)
            };

        }

        public override void DrawWindow(ImGuiWindowFlags overwriteFlags)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            base.DrawWindow(overwriteFlags);
            ImGui.PopStyleVar();
        }

        protected override void OnSizeChanged(Vector2 oldSize, Vector2 size)
        {
            view.Size = ImGui.GetContentRegionAvail();
        }

        public override void Dispose()
        {
            view.Dispose();
        }

        public override void DrawContent()
        {
            view.Draw("WebBrowser"u8);
        }
    }
}