namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.SDL2;
    using Silk.NET.Maths;

    public interface IGLContext : HexaGen.Runtime.INativeContext
    {
        Vector2D<int> FramebufferSize { get; }
        nint Handle { get; }
        bool IsCurrent { get; }
        unsafe SDLWindow* Window { get; set; }

        void MakeCurrent();

        void SwapBuffers();

        void SwapInterval(int interval);
    }
}