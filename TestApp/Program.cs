// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Kitty;
using Kitty.Graphics;
using Kitty.UI;
using Kitty.UI.Dialogs;
using TestApp;

WidgetManager.Register<MainWindow>(show: true);
Application.Run();

namespace TestApp
{
    public class MainWindow : ImWindow
    {
        private string file;

        public MainWindow()
        {
            // embed this window in the 'physical' window
            IsEmbedded = true;
        }

        protected override string Name => "Main Window";

        public override void DrawWindow(IGraphicsContext context)
        {
            base.DrawWindow(context);
        }

        public override unsafe void DrawContent(IGraphicsContext context)
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

            void* fu = stackalloc byte[12];
            ((int*)fu)[0] = 10;
            ((double*)&((int*)fu)[1])[0] = 10.0f;

            ImGui.TextV("%d, Hello, %f", (nuint)fu);
        }

        private void Callback(object? sender, DialogResult result)
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
    }
}