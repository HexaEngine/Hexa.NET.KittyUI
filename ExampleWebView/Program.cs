// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.WebView;
using TestApp;

Application.GraphicsDebugging = true;
AppBuilder appBuilder = new();
appBuilder.EnableLogging(true);
appBuilder.EnableDebugTools(true);
appBuilder.SetTitle("Test App");
appBuilder.Style(style =>
{
    style.WindowPadding = default;
});
appBuilder.AddWindow<MainWindow>(true, true);
appBuilder.StyleColorsDark();
appBuilder.Run();

namespace TestApp
{
    public class MainWindow : ImWindow
    {
        private WebView view;

        public MainWindow()
        {
        }

        protected override string Name => "Main Window";

        public override void Init()
        {
            view = new("https://google.com")
            {
                Size = new(1280, 720)
            };
            
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