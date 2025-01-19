namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.Structs;

    public unsafe struct CefDrawData
    {
        public PaintElementType Type;
        public Rect DirtyRect;
        public int Width;
        public int Height;
        public void* Data;

        public CefDrawData(PaintElementType type, Rect dirtyRect, int width, int height, void* data)
        {
            Type = type;
            DirtyRect = dirtyRect;
            Width = width;
            Height = height;
            Data = data;
        }

        public void Release()
        {
            Free(Data);
        }
    }
}