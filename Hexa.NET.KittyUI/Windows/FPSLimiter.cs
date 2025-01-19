namespace Hexa.NET.KittyUI.Windows
{
#if GLES

    using Hexa.NET.OpenGLES;

#else


#endif

    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public class FPSLimiter
    {
        private long fpsStartTime;
        private long fpsFrameCount;
        private int targetFPS = 120;

        public int TargetFPS { get => targetFPS; set => targetFPS = value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LimitFrameRate()
        {
            int fps = targetFPS;
            long freq = Stopwatch.Frequency;
            long frame = Stopwatch.GetTimestamp();
            while ((frame - fpsStartTime) * fps < freq * fpsFrameCount)
            {
                int sleepTime = (int)((fpsStartTime * fps + freq * fpsFrameCount - frame * fps) * 1000 / (freq * fps));
                if (sleepTime > 0) Thread.Sleep(sleepTime);
                frame = Stopwatch.GetTimestamp();
            }
            if (++fpsFrameCount > fps)
            {
                fpsFrameCount = 0;
                fpsStartTime = frame;
            }
        }
    }
}