namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.ImGui;

    public interface IPage
    {
        string Title { get; }

        void OnNavigatedTo(IPage? previousPage);

        void OnNavigatedFrom(IPage? nextPage);

        void DrawPage(ImGuiWindowFlags overwriteFlags);

        INavigation Navigation { get; set; }
    }
}