namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.Logging;

#if GLES

    using Hexa.NET.OpenGLES;
    using Hexa.NET.OpenGLES.KHR;

#else

    using Hexa.NET.OpenGL;
    using Hexa.NET.OpenGL.ARB;
    using Hexa.NET.OpenGL.KHR;

#endif

    using System.Threading;
    using Hexa.NET.KittyUI.Threading;
    using HexaGen.Runtime;

    public unsafe class OpenGLAdapter
    {
        static ThreadDispatcher dispatcher = null!;

        public static readonly ILogger GLLogger = LoggerFactory.GetLogger(nameof(GL));

        public static IGLContext Context { get; private set; } = null!;

        public static GL GL { get; private set; }

        public static UploadQueue UploadQueue { get; private set; } = null!;

        public static DeleteQueue DeleteQueue { get; private set; } = null!;

        public static IThreadDispatcher Dispatcher => dispatcher;

        public static bool IsPersistentMappingSupported { get; private set; }

        public static bool NoExtensions { get; private set; }

        public static void Init(IWindow window)
        {
            dispatcher = new ThreadDispatcher(Thread.CurrentThread);
            Context = window.OpenGLCreateContext();
            Context.MakeCurrent();
            GL = new(Context);

            GLVersion.Current = GLVersion.GetInternalVersion(GL);

#if GLES
            if (GLVersion.Current >= new GLVersion(3, 1, true))
            {
                GL.DebugMessageCallback(GLESDebugCallback, null);
                GL.Enable(GLEnableCap.DebugOutput);
            }
            else if (!NoExtensions && GL.TryGetExtension<GLKHRDebug>(out var GLKHRDebug))
            {
                GLKHRDebug.DebugMessageCallback(GLESDebugCallback, null);
                GL.Enable(GLEnableCap.DebugOutput);
            }
#else
            if (GLVersion.Current >= new GLVersion(4, 3))
            {
                GL.DebugMessageCallback(GLDebugCallback, null);
                GL.Enable(GLEnableCap.DebugOutput);
            }
            else if (!NoExtensions && GL.TryGetExtension<GLARBDebugOutput>(out var GLARBDebugOutput))
            {
                GLARBDebugOutput.DebugMessageCallbackARB(GLDebugCallback, null);
                GL.Enable(GLEnableCap.DebugOutput);
            }
            else if (!NoExtensions && GL.TryGetExtension<GLKHRDebug>(out var GLKHRDebug))
            {
                GLKHRDebug.DebugMessageCallback(GLDebugCallback, null);
                GL.Enable(GLEnableCap.DebugOutput);
            }

            if (GLVersion.Current >= new GLVersion(4, 4))
            {
                IsPersistentMappingSupported = true;
            }
#endif

            UploadQueue = new(Context, window);
            DeleteQueue = new(GL, Thread.CurrentThread);

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

        private static void GLESDebugCallback(OpenGLES.GLEnum source, OpenGLES.GLEnum type, uint id, OpenGLES.GLEnum severity, int length, byte* message, void* userParam)
        {
            LogSeverity logSeverity = severity switch
            {
                OpenGLES.GLEnum.DebugSeverityNotification => LogSeverity.Info,
                OpenGLES.GLEnum.DebugSeverityLow => LogSeverity.Debug,
                OpenGLES.GLEnum.DebugSeverityMedium => LogSeverity.Warning,
                OpenGLES.GLEnum.DebugSeverityHigh => LogSeverity.Error,
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
            dispatcher.ExecuteQueue();
            DeleteQueue.ProcessQueue();
        }

        public static void Shutdown()
        {
            UploadQueue.Dispose();
            Context.Dispose();
            GL.Dispose();
        }
    }
}