namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;

    public interface IGLContext : HexaGen.Runtime.INativeContext
    {
        Point2 FramebufferSize { get; }
        nint Handle { get; }
        bool IsCurrent { get; }
        unsafe SDLWindow* Window { get; set; }

        void MakeCurrent();

        void SwapBuffers();

        void SwapInterval(int interval);
    }
}