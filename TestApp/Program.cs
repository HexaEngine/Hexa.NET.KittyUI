// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.ImGui.Widgets.Dialogs;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.Graphics;
using Hexa.NET.KittyUI.Graphics.Imaging;
using Hexa.NET.KittyUI.Web;
using TestApp;

AppBuilder appBuilder = new();
//appBuilder.SetGraphicsBackend(GraphicsBackend.OpenGL);
appBuilder.SetTitle("Test App");
appBuilder.AddWindow<MainWindow>(true, true);
appBuilder.StyleColorsDark();
appBuilder.Run();

namespace TestApp
{
    public class MainWindow : ImWindow
    {
        private string? file;
        private Image2D? image;

        protected override string Name => "Main Window";

        public override unsafe void DrawContent()
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

            if (ImGui.Button("Load Texture"))
            {
                LoadTexture("icon.png");
            }

            if (image != null)
            {
                ImGui.Image(image, new(256, 256));
            }
        }

        private void LoadTexture(string path)
        {
            Task.Run(() =>
            {
                image = Image2D.LoadFromFile(path);
            });
        }

        private void LoadWebTexture()
        {
            Task.Run(async () =>
            {
                HttpClient client = new();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:132.0) Gecko/20100101 Firefox/132.0");
                MemoryStream ms = new();
                await client.DownloadAsyncCached("https://assets.change.org/photos/2/dn/rv/ScdnRvnrUismCXg-800x450-noPad.jpg?1632662406", ms);
                image = Image2D.LoadFromMemory(ImageFileFormat.JPEG, ms.GetBuffer(), 0, (int)ms.Length);
            });
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