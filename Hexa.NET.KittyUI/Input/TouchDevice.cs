﻿namespace Hexa.NET.KittyUI.Input
{
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.Input.Events;
    using Hexa.NET.SDL2;
    using static Hexa.NET.KittyUI.Extensions.SdlErrorHandlingExtensions;

    /// <summary>
    /// Represents a generic delegate for handling events in the TouchDevice class.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of event arguments for the event.</typeparam>
    /// <param name="sender">The source of the event, which is the TouchDevice that raised the event.</param>
    /// <param name="e">The event arguments specific to the type of event being handled.</param>
    public delegate void TouchDeviceEventHandler<TEventArgs>(TouchDevice sender, TEventArgs e);

    /// <summary>
    /// Represents a touch input device.
    /// </summary>
    public unsafe class TouchDevice
    {
        private readonly long id;
        private readonly string name;
        private readonly TouchDeviceType type;
        private readonly Finger[] fingers;
        private readonly Dictionary<long, int> fingerIdToIndex = new();

        private readonly TouchEventArgs touchEventArgs = new();
        private readonly TouchMotionEventArgs touchMotionEventArgs = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchDevice"/> class using the specified index.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index">The index of the touch device.</param>
        public TouchDevice(long id, int index)
        {
            byte* pName = SDL.GetTouchName(index);
            if (pName == null)
            {
                SdlLogWarn();
                name = "Unknown";
            }
            else
            {
                name = ToStringFromUTF8(pName)!;
            }

            type = (TouchDeviceType)SDL.GetTouchDeviceType(id);

            var fingerCount = SDL.GetNumTouchFingers(id);
            if (fingerCount == 0)
            {
                SdlLogWarn();
            }
            fingers = new Finger[fingerCount];
            for (int i = 0; i < fingerCount; i++)
            {
                var finger = SDL.GetTouchFinger(id, i);
                if (finger == null)
                {
                    SdlLogger.Warn($"No finger found at index {i} for touch device {id}.");
                    continue;
                }
                fingers[i] = new(finger);
                fingerIdToIndex.Add(finger->Id, i);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchDevice"/> class using the specified device ID.
        /// </summary>
        /// <param name="id">The ID of the touch device.</param>
        public TouchDevice(long id)
        {
            this.id = id;
            name = "Unknown";
            type = (TouchDeviceType)SDL.GetTouchDeviceType(id);

            var fingerCount = SDL.GetNumTouchFingers(id);
            if (fingerCount == 0)
            {
                SdlLogWarn();
            }
            fingers = new Finger[fingerCount];
            for (int i = 0; i < fingerCount; i++)
            {
                var finger = SDL.GetTouchFinger(id, i);
                if (finger == null)
                {
                    SdlLogger.Warn($"No finger found at index {i} for touch device {id}.");
                    continue;
                }
                fingers[i] = new(finger);
                fingerIdToIndex.Add(finger->Id, i);
            }
        }

        /// <summary>
        /// Gets the ID of the touch device.
        /// </summary>
        public long Id => id;

        /// <summary>
        /// Gets the name of the touch device.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Gets the type of the touch device.
        /// </summary>
        public TouchDeviceType Type => type;

        /// <summary>
        /// Gets the number of fingers associated with the touch device.
        /// </summary>
        public int FingerCount => fingers.Length;

        /// <summary>
        /// Occurs when a finger is lifted off the touch device.
        /// </summary>
        public event TouchDeviceEventHandler<TouchEventArgs>? TouchUp;

        /// <summary>
        /// Occurs when a finger is placed on the touch device.
        /// </summary>
        public event TouchDeviceEventHandler<TouchEventArgs>? TouchDown;

        /// <summary>
        /// Occurs when a finger moves on the touch device.
        /// </summary>
        public event TouchDeviceEventHandler<TouchMotionEventArgs>? TouchMotion;

        internal (TouchDevice, TouchEventArgs) OnFingerUp(SDLTouchFingerEvent evnt)
        {
            touchEventArgs.Timestamp = evnt.Timestamp;
            touchEventArgs.TouchDeviceId = id;
            touchEventArgs.FingerId = evnt.FingerId;
            touchEventArgs.Pressure = evnt.Pressure;
            touchEventArgs.X = evnt.X;
            touchEventArgs.Y = evnt.Y;
            touchEventArgs.State = FingerState.Up;

            var idx = fingerIdToIndex[evnt.FingerId];
            var finger = fingers[idx];
            finger.OnFingerUp(touchEventArgs);
            ImGuiDebugTools.WriteLine($"Up {evnt.FingerId}, {evnt.X}, {evnt.Y}");

            TouchUp?.Invoke(this, touchEventArgs);
            return (this, touchEventArgs);
        }

        internal (TouchDevice, TouchEventArgs) OnFingerDown(SDLTouchFingerEvent evnt)
        {
            touchEventArgs.Timestamp = evnt.Timestamp;
            touchEventArgs.TouchDeviceId = id;
            touchEventArgs.FingerId = evnt.FingerId;
            touchEventArgs.Pressure = evnt.Pressure;
            touchEventArgs.X = evnt.X;
            touchEventArgs.Y = evnt.Y;
            touchEventArgs.State = FingerState.Down;

            var idx = fingerIdToIndex[evnt.FingerId];
            var finger = fingers[idx];
            finger.OnFingerDown(touchEventArgs);
            ImGuiDebugTools.WriteLine($"Down {evnt.FingerId}, {evnt.X}, {evnt.Y}");

            TouchDown?.Invoke(this, touchEventArgs);
            return (this, touchEventArgs);
        }

        internal (TouchDevice, TouchMotionEventArgs) OnFingerMotion(SDLTouchFingerEvent evnt)
        {
            touchMotionEventArgs.Timestamp = evnt.Timestamp;
            touchMotionEventArgs.TouchDeviceId = id;
            touchMotionEventArgs.FingerId = evnt.FingerId;
            touchMotionEventArgs.Pressure = evnt.Pressure;
            touchMotionEventArgs.X = evnt.X;
            touchMotionEventArgs.Y = evnt.Y;
            touchMotionEventArgs.Dx = evnt.Dx;
            touchMotionEventArgs.Dy = evnt.Dy;

            var idx = fingerIdToIndex[evnt.FingerId];
            var finger = fingers[idx];
            finger.OnFingerMotion(touchMotionEventArgs);
            ImGuiDebugTools.WriteLine($"Motion {evnt.FingerId}, {evnt.X}, {evnt.Y}, {evnt.Pressure}");

            TouchMotion?.Invoke(this, touchMotionEventArgs);
            return (this, touchMotionEventArgs);
        }

        /// <summary>
        /// Checks if a finger with the specified ID is in the "down" state.
        /// </summary>
        /// <param name="fingerId">The ID of the finger to check.</param>
        /// <returns>True if the finger is in the "down" state; otherwise, false.</returns>
        public bool IsDownById(long fingerId)
        {
            return IsDownByIndex(fingerIdToIndex[fingerId]);
        }

        /// <summary>
        /// Checks if a finger with the specified ID is in the "up" state.
        /// </summary>
        /// <param name="fingerId">The ID of the finger to check.</param>
        /// <returns>True if the finger is in the "up" state; otherwise, false.</returns>
        public bool IsUpById(long fingerId)
        {
            return IsUpByIndex(fingerIdToIndex[fingerId]);
        }

        /// <summary>
        /// Checks if a finger at the specified index is in the "down" state.
        /// </summary>
        /// <param name="index">The index of the finger to check.</param>
        /// <returns>True if the finger is in the "down" state; otherwise, false.</returns>
        public bool IsDownByIndex(int index)
        {
            return fingers[index].State == FingerState.Down;
        }

        /// <summary>
        /// Checks if a finger at the specified index is in the "up" state.
        /// </summary>
        /// <param name="index">The index of the finger to check.</param>
        /// <returns>True if the finger is in the "up" state; otherwise, false.</returns>
        public bool IsUpByIndex(int index)
        {
            return fingers[index].State == FingerState.Up;
        }

        /// <summary>
        /// Gets a finger by its ID.
        /// </summary>
        /// <param name="fingerId">The ID of the finger to retrieve.</param>
        /// <returns>The <see cref="Finger"/> associated with the specified ID.</returns>
        public Finger GetFingerById(long fingerId)
        {
            return GetFingerByIndex(fingerIdToIndex[fingerId]);
        }

        /// <summary>
        /// Gets a finger by its index.
        /// </summary>
        /// <param name="index">The index of the finger to retrieve.</param>
        /// <returns>The <see cref="Finger"/> at the specified index.</returns>
        public Finger GetFingerByIndex(int index)
        {
            return fingers[index];
        }
    }
}