// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Kitty;
using Kitty.Windows;

Window window = new();
window.Draw += DrawWindow;

static void DrawWindow(Kitty.Graphics.IGraphicsContext context)
{
    ImGui.ShowDemoWindow();
}

Application.Run(window);