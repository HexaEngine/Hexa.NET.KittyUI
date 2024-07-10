namespace Kitty.Debugging
{
    using Kitty.Logging;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class CrashLogger
    {
        /// <summary>
        /// Gets the file log writer used for crash logging.
        /// </summary>
        public static readonly FileLogWriter FileLogWriter = new($"logs/app-{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log");

        public static void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            Logger.Writers.Add(FileLogWriter);
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                Logger.Close();
                var exception = (Exception)e.ExceptionObject;

                StringBuilder sb = new();
                sb.AppendLine($"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}");
                sb.AppendLine($"Runtime: .Net {Environment.Version}");
                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine($"Unhandled exception {exception.HResult} {exception.Message} at {exception.TargetSite}");
                sb.AppendLine($"\t{Marshal.GetExceptionForHR(exception.HResult)?.Message}");
                sb.AppendLine();

                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("Callstack:");
                sb.AppendLine(exception.StackTrace?.Replace(Environment.NewLine, "\n\t"));

                var fileInfo = new FileInfo($"logs/crash-{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log");
                fileInfo.Directory?.Create();
                File.AppendAllText(fileInfo.FullName, sb.ToString());
            }
        }
    }
}