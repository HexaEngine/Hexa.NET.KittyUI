namespace Kitty.UI.Dialogs
{
    public interface IDialog
    {
        bool Shown { get; }

        void Draw();
        void Hide();
        void Reset();
        void Show();
    }
}