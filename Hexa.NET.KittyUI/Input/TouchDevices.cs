namespace Hexa.NET.KittyUI.Input
{
    using Hexa.NET.KittyUI.Input.Events;
    using Hexa.NET.SDL2;

    /// <summary>
    /// Provides functionality to manage touch devices.
    /// </summary>
    public static class TouchDevices
    {
        private static readonly List<TouchDevice> touchDevices = new();
        private static readonly Dictionary<long, TouchDevice> idToTouch = new();

        private static readonly TouchDeviceEventArgs touchDeviceEventArgs = new();

        /// <summary>
        /// Gets the list of available touch devices.
        /// </summary>
        public static IReadOnlyList<TouchDevice> Devices => touchDevices;

        /// <summary>
        /// Occurs when a touch device is added.
        /// </summary>
        public static event TouchDeviceEventHandler<TouchDeviceEventArgs>? TouchDeviceAdded;

        /// <summary>
        /// Occurs when a touch device is removed.
        /// </summary>
        public static event TouchDeviceEventHandler<TouchDeviceEventArgs>? TouchDeviceRemoved;

        /// <summary>
        /// Occurs when a touch event is detected (e.g., touch-up, touch-down, touch-motion).
        /// </summary>
        public static event TouchDeviceEventHandler<TouchEventArgs>? TouchUp;

        /// <summary>
        /// Occurs when a touch-down event is detected.
        /// </summary>
        public static event TouchDeviceEventHandler<TouchEventArgs>? TouchDown;

        /// <summary>
        /// Occurs when a touch-motion event is detected.
        /// </summary>
        public static event TouchDeviceEventHandler<TouchMotionEventArgs>? TouchMotion;

        /// <summary>
        /// Initializes the touch device management system.
        /// </summary>
        internal static void Init()
        {
            var touchDeviceCount = SDL.GetNumTouchDevices();

            for (int i = 0; i < touchDeviceCount; i++)
            {
                AddTouchDevice(i);
            }
        }

        /// <summary>
        /// Retrieves a touch device by its unique identifier.
        /// </summary>
        /// <param name="touchDeviceId">The unique identifier of the touch device.</param>
        /// <returns>The touch device associated with the specified identifier.</returns>
        public static TouchDevice GetById(long touchDeviceId)
        {
            return idToTouch[touchDeviceId];
        }

        internal static TouchDevice? AddTouchDevice(int index)
        {
            long id = SDL.GetTouchDevice(index);
            SDL.ClearError();
            if (id == 0) return null;
            TouchDevice dev = new(id, index);
            touchDevices.Add(dev);
            idToTouch.Add(dev.Id, dev);
            touchDeviceEventArgs.TouchDeviceId = dev.Id;
            TouchDeviceAdded?.Invoke(dev, touchDeviceEventArgs);
            return dev;
        }

        internal static TouchDevice? AddTouchDevice(long touchId)
        {
            if (touchId == 0) return null;
            TouchDevice dev = new(touchId);
            touchDevices.Add(dev);
            idToTouch.Add(touchId, dev);
            touchDeviceEventArgs.TouchDeviceId = dev.Id;
            TouchDeviceAdded?.Invoke(dev, touchDeviceEventArgs);
            return dev;
        }

        internal static bool RemoveTouchDevice(long touchId)
        {
            if (idToTouch.TryGetValue(touchId, out var dev))
            {
                idToTouch.Remove(touchId);
                touchDevices.Remove(dev);
                touchDeviceEventArgs.TouchDeviceId = dev.Id;
                TouchDeviceRemoved?.Invoke(dev, touchDeviceEventArgs);
                return true;
            }
            return false;
        }

        internal static TouchDevice? AddOrGetTouch(long id)
        {
            if (id == 0) return null;
            if (idToTouch.TryGetValue(id, out TouchDevice? dev))
            {
                return dev;
            }
            return AddTouchDevice(id);
        }

        internal static void FingerUp(SDLTouchFingerEvent evnt)
        {
            var result = AddOrGetTouch(evnt.TouchId)?.OnFingerUp(evnt);
            if (!result.HasValue) return;
            TouchUp?.Invoke(result.Value.Item1, result.Value.Item2);
        }

        internal static void FingerDown(SDLTouchFingerEvent evnt)
        {
            var result = AddOrGetTouch(evnt.TouchId)?.OnFingerDown(evnt);
            if (!result.HasValue) return;
            TouchDown?.Invoke(result.Value.Item1, result.Value.Item2);
        }

        internal static void FingerMotion(SDLTouchFingerEvent evnt)
        {
            var result = AddOrGetTouch(evnt.TouchId)?.OnFingerMotion(evnt);
            if (!result.HasValue) return;
            TouchMotion?.Invoke(result.Value.Item1, result.Value.Item2);
        }
    }
}