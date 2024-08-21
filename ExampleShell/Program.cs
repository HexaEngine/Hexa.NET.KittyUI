using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.UI;

AppBuilder builder = new();
builder.AddTitleBar(new TitleBar());
builder.UseAppShell
    ("Test Shell App",
        shellBuilder =>
            shellBuilder
            .AddPage<MainPage>("/")
            .AddPage<SubPage>("/SubPage")
    )
.Run();

public class MainPage : Page
{
    public override string Title { get; } = "Main Page";

    public override void DrawContent()
    {
        ImGui.Text("Hello World");

        if (ImGui.Button("Go to sub page"))
        {
            Navigation.NavigateTo("SubPage");
        }
    }
}

public class SubPage : Page
{
    public override string Title { get; } = "Sub Page";

    public override void DrawContent()
    {
        ImGui.Text("Hello from sub page");
    }
}