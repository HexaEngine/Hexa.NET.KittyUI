namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.Logging;
    using Hexa.NET.OpenGL;

    public unsafe class OpenGLAdapter
    {
        public static IGLContext Context { get; private set; } = null!;

        public static UploadQueue UploadQueue { get; private set; } = null!;

        public static DeleteQueue DeleteQueue { get; private set; } = null!;

        public static void Init(IWindow window)
        {
            Context = window.OpenGLCreateContext();
            Context.MakeCurrent();
            GL.InitApi(Context);

            UploadQueue = new(Thread.CurrentThread);
            DeleteQueue = new(Thread.CurrentThread);

            LoggerFactory.General.Info($"Backend: Using Graphics API: OpenGL {ToStringFromUTF8(GL.GetString(GLStringName.Version))}");
            LoggerFactory.General.Info($"Backend: Using Graphics Device: {ToStringFromUTF8(GL.GetString(GLStringName.Renderer))}");
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