namespace Hexa.NET.Kitty.OpenGL
{
    using Hexa.NET.Kitty.Windows;
    using Silk.NET.Core.Contexts;
    using Silk.NET.OpenGL;

    public class OpenGLAdapter
    {
        public static GL GL { get; private set; }

        public static IGLContext Context { get; private set; }

        public static void Init(IWindow window)
        {
            Context = window.OpenGLCreateContext();
            GL = GL.GetApi(Context);
        }

        public static void Shutdown()
        {
            Context.Dispose();
            GL.Dispose();
        }
    }
}