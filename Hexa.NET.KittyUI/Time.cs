namespace Hexa.NET.KittyUI
{
    using System;
    using System.Diagnostics;

    public static class Time
    {
        private static long last;
        private static double cumulativeFrameTime;

        public static float Delta { get; private set; }

        public static long Frame { get; private set; }

        public static double CumulativeFrameTime => cumulativeFrameTime;

        public static void Initialize()
        {
            last = Stopwatch.GetTimestamp();
            cumulativeFrameTime = 0;
        }

        public static void FrameUpdate()
        {
            Frame++;
            long now = Stopwatch.GetTimestamp();
            double deltaTime = ((double)now - last) / Stopwatch.Frequency;

            // Handle edge cases: negative time (clock adjustment) or extremely small values
            if (deltaTime < 0 || deltaTime > 1.0) // Cap at 1 second to prevent huge jumps
            {
                deltaTime = 1.0 / 60.0; // Default to 16.67ms (60 fps)
            }
            else if (deltaTime == 0)
            {
                deltaTime = 1e-9; // Set to 1 nanosecond to avoid zero delta
            }

            // Calculate the frame time by the time difference over the timer speed resolution.
            Delta = (float)deltaTime;
            cumulativeFrameTime += Delta;

            last = now;
        }
    }
}