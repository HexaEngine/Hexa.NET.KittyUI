using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;

AppBuilder builder = new();
builder.AddWindow("Main Window", () =>
{
    ImGui.Text("Hello, World!");
});
builder.Run();