namespace Hexa.NET.KittyUI.ImGuizmo
{
    public static class AppBuilderExtensions
    {
        public static AppBuilder EnableImGuizmo(this AppBuilder builder)
        {
            builder.AddImGuiAddon(new ImGuizmoAddon());
            return builder;
        }
    }
}