namespace Hexa.NET.KittyUI.OpenAL
{
    public static class AppBuilderExtensions
    {
        public static AppBuilder WithOpenAL(this AppBuilder builder)
        {
            OpenALAdapter.Init();
            builder.EnableSubSystem(SubSystems.Audio);
            return builder;
        }
    }
}