// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.ImGui.Widgets.Dialogs;
using Hexa.NET.KittyUI;

string? file = null;

AppBuilder builder = new();
builder.AddWindow("Main Window", () =>
{
    ImGui.Text("Hello, World!");

    if (file != null)
    {
        ImGui.Text($"Selected file: {file}");
    }

    if (ImGui.Button("... (open)"))
    {
        OpenFileDialog dialog = new();
        dialog.AllowMultipleSelection = true;
        dialog.Show(Callback);
    }

    if (ImGui.Button("... (save)"))
    {
        SaveFileDialog dialog = new();
        dialog.Show(Callback);
    }
});
builder.AddWindow<SecondaryWindow>();

builder.Run();

void Callback(object? sender, DialogResult result)
{
    if (sender is OpenFileDialog dialog)
    {
        if (result == DialogResult.Ok)
        {
            file = dialog.SelectedFile;
        }
    }
    if (sender is SaveFileDialog saveFileDialog)
    {
        if (result == DialogResult.Ok)
        {
            file = saveFileDialog.SelectedFile;
        }
    }
}

internal class SecondaryWindow : ImWindow
{
    protected override string Name { get; }

    public override void DrawContent()
    {
        throw new NotImplementedException();
    }
}