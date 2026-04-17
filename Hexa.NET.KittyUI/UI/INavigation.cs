namespace Hexa.NET.KittyUI.UI
{
    using System.Collections.Generic;

    public interface INavigation
    {
        void NavigateTo(string path, object? args = null);

        void NavigateTo(Page page, object? args = null);

        void NavigateBack();

        void NavigateToRoot();

        IEnumerable<Page> GetHistoryStack();

        void NavigateBackTo(Page page);

        void ShowMenu();

        bool CanGoBack { get; }

        bool CanGoForward { get; }
    }
}