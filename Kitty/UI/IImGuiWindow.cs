namespace Hexa.NET.Kitty.UI
{
    public interface IImGuiWindow
    {
        void Close();

        void Dispose();

        void DrawContent();

        void DrawMenu();

        void DrawWindow();

        void Init();

        void Show();
    }
}