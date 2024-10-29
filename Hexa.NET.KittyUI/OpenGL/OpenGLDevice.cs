namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.OpenGL;
    using HexaGen.Runtime;

    public class OpenGLAdapter
    {
        public static IGLContext Context { get; private set; } = null!;

        public static UploadQueue UploadQueue { get; private set; } = null!;

        public static DeleteQueue DeleteQueue { get; private set; } = null!;

        public static void Init(IWindow window)
        {
            Context = window.OpenGLCreateContext();
            GL.InitApi(Context);
            UploadQueue = new(Thread.CurrentThread);
            DeleteQueue = new(Thread.CurrentThread);
        }

        public static void ProcessQueues()
        {
            DeleteQueue.ProcessQueue();
            UploadQueue.ProcessQueue();
        }

        public static void Shutdown()
        {
            Context.Dispose();
            GL.FreeApi();
        }
    }
}