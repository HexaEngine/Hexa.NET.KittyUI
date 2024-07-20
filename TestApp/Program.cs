// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Kitty;
using Kitty.Graphics;
using Kitty.UI;
using TestApp;

WidgetManager.Register<MainWindow>(show: true);

Application.Run();

namespace TestApp
{
    public class MainWindow : ImWindow
    {
        public MainWindow()
        {
            IsEmbedded = true;
        }

        protected override string Name => "Main Window";

        public override void DrawContent(IGraphicsContext context)
        {
            ImGui.Text("Hello, World!");
        }
    }
}