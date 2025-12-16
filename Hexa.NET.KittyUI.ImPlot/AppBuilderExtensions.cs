namespace Hexa.NET.KittyUI.ImPlot
{
    public static class AppBuilderExtensions
    {
        public static AppBuilder EnableImPlot(this AppBuilder builder)
        {
            builder.AddImGuiAddon(new ImPlotAddon());
            return builder;
        }
    }
}