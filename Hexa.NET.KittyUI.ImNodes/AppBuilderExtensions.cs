namespace Hexa.NET.KittyUI.ImNodes
{
    public static class AppBuilderExtensions
    {
        public static AppBuilder EnableImNodes(this AppBuilder builder)
        {
            builder.AddImGuiAddon(new ImNodesAddon());
            return builder;
        }
    }
}