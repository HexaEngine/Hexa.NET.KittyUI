namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.Logging;
    using Hexa.NET.OpenGL;
    using System.Runtime.InteropServices;
    using System.Threading;

    public unsafe class OpenGLAdapter
    {
        public static readonly ILogger GLLogger = LoggerFactory.GetLogger(nameof(GL));

        public static IGLContext Context { get; private set; } = null!;

        public static UploadQueue UploadQueue { get; private set; } = null!;

        public static DeleteQueue DeleteQueue { get; private set; } = null!;

        public static bool CanUploadTexturesAsync { get; private set; }

        public static void Init(IWindow window)
        {
            Context = window.OpenGLCreateContext();
            Context.MakeCurrent();
            GL.InitApi(Context);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // not supported on macos since it uses OpenGL 4.1 and this needs atleast 4.3
            {
                GL.DebugMessageCallback(GLDebugCallback, null);
                GL.Enable(GLEnableCap.DebugOutput);
                CanUploadTexturesAsync = true;
            }

            OpenGLTexturePool.Global.AllocateNewBlock();

            UploadQueue = new(Thread.CurrentThread);
            DeleteQueue = new(Thread.CurrentThread);

            LoggerFactory.General.Info($"Backend: Using Graphics API: OpenGL {ToStringFromUTF8(GL.GetString(GLStringName.Version))}");
            LoggerFactory.General.Info($"Backend: Using Graphics Device: {ToStringFromUTF8(GL.GetString(GLStringName.Renderer))}");
        }

        public static LogSeverity LogLevel { get; set; } = LogSeverity.Warning;

        private static void GLDebugCallback(GLEnum source, GLEnum type, uint id, GLEnum severity, int length, byte* message, void* userParam)
        {
            LogSeverity logSeverity = severity switch
            {
                GLEnum.DebugSeverityNotification => LogSeverity.Info,
                GLEnum.DebugSeverityLow => LogSeverity.Debug,
                GLEnum.DebugSeverityMedium => LogSeverity.Warning,
                GLEnum.DebugSeverityHigh => LogSeverity.Error,
                _ => LogSeverity.Info
            };

            if (logSeverity < LogLevel)
            {
                return;
            }

            string msg = ToStringFromUTF8(message)!;

            GLLogger.Log(logSeverity, $"[{source}] [{type}] ID: {id}, {msg}");
        }

        public static void ProcessQueues()
        {
            OpenGLTexturePool.Global.AllocateNewBlock();
            DeleteQueue.ProcessQueue();
            UploadQueue.ProcessQueue();
        }

        public static void Shutdown()
        {
            OpenGLTexturePool.Global.Dispose();
            Context.Dispose();
            GL.FreeApi();
        }
    }
}