namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.ImGui;

    public interface IPage
    {
        string Title { get; }

        void OnNavigatedTo(IPage? previousPage, object? args);

        void OnNavigatedFrom(IPage? nextPage, object? args);

        void DrawPage(ImGuiWindowFlags overwriteFlags);

        INavigation Navigation { get; set; }
    }
}