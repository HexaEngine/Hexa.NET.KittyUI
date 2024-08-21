namespace ExampleAndroid;

using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;

public class Program
{
    public static void Main(string[] args)
    {
        AppBuilder builder = new();
        builder.AddWindow("Main Window", () =>
        {
            ImGui.Text("Hello, World!");
        });
        builder.Run();
    }
}