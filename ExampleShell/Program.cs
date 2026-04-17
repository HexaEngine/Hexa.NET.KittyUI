using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.UI;
using Microsoft.Extensions.DependencyInjection;

AppBuilder builder = AppBuilder.Create();
builder.Services.AddSingleton<FooService>();
var app = builder.Build();
app.UseTitleBar<TitleBar>();
app.UseAppShell
    ("Test Shell App",
        shellBuilder =>
            shellBuilder
            .AddPage<MainPage>("/")
            .AddPage<SubPage>("/SubPage")
    )
.Run();

public class FooService
{
}

public class MainPage : Page
{
    private readonly FooService fooService;

    public MainPage(FooService fooService)
    {
        this.fooService = fooService;
    }

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