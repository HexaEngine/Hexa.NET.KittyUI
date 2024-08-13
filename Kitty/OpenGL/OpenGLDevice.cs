namespace Hexa.NET.Kitty.OpenGL
{
    using Hexa.NET.Kitty.Windows;
    using Silk.NET.Core.Contexts;
    using Silk.NET.OpenGL;

    public class OpenGLAdapter
    {
        public static GL GL { get; private set; }

        public static IGLContext Context { get; private set; }

        public static UploadQueue UploadQueue { get; private set; }

        public static DeleteQueue DeleteQueue { get; private set; }

        public static void Init(IWindow window)
        {
            Context = window.OpenGLCreateContext();
            GL = GL.GetApi(Context);
            UploadQueue = new(GL, Thread.CurrentThread);
            DeleteQueue = new(GL, Thread.CurrentThread);
        }

        public static void ProcessQueues()
        {
            DeleteQueue.ProcessQueue();
            UploadQueue.ProcessQueue();
        }

        public static void Shutdown()
        {
            Context.Dispose();
            GL.Dispose();
        }
    }
}