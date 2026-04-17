using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;

AppBuilder.Create()
    .Build()
    .AddWindow("Main Window", () =>
    {
        ImGui.Text("Hello, World!");
    })
    .Run();