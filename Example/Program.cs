// See https://aka.ms/new-console-template for more information
using Example;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.UI;
using Hexa.NET.KittyUI.OpenAL;

AppBuilder.Create()
    .WithOpenAL()
    .EnableLogging(true)
    .EnableDebugTools(true)
    .SetTitle("Test App")
    .AddWindow<MainWindow>()
    .AddTitleBar<TitleBar>()
    .StyleColorsDark()
    .Run();

namespace Example
{
    public class MainWindow : ImWindow
    {
        public MainWindow()
        {
            IsEmbedded = true;
        }

        public override string Name => "Main Window";

        public override unsafe void DrawContent()
        {
            ImGui.Text("Hello, World!"u8);
        }
    }
}