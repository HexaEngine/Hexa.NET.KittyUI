namespace Hexa.NET.KittyUI.UI
{
    using System.Collections.Generic;

    public interface INavigation
    {
        void NavigateTo(string path);

        void NavigateTo(IPage page);

        void NavigateBack();

        void NavigateToRoot();

        IEnumerable<IPage> GetHistoryStack();
        void NavigateBackTo(IPage page);
    }
}