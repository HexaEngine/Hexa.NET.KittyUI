namespace Hexa.NET.KittyUI.UI
{
    using System.Collections.Generic;

    public interface INavigation
    {
        void NavigateTo(string path, object? args = null);

        void NavigateTo(IPage page, object? args = null);

        void NavigateBack();

        void NavigateToRoot();

        IEnumerable<IPage> GetHistoryStack();

        void NavigateBackTo(IPage page);

        void ShowMenu();

        bool CanGoBack { get; }

        bool CanGoForward { get; }
    }
}