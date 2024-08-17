namespace Hexa.NET.KittyUI.Windows
{
    public struct DisplayMode
    {
        public uint Format;
        public int W;
        public int H;
        public int RefreshRate;
        public unsafe void* DriverData;
    }
}