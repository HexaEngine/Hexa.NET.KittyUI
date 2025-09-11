namespace Hexa.NET.KittyUI.Input
{
    using Hexa.NET.KittyUI.Input.Events;
    using Hexa.NET.SDL3;

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

        private readonly TouchEventArgs touchEventArgs = new();
        private readonly TouchMotionEventArgs touchMotionEventArgs = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchDevice"/> class using the specified device ID.
        /// </summary>
        /// <param name="id">The ID of the touch device.</param>
        public TouchDevice(long id)
        {
            this.id = id;
            name = SDL.GetTouchDeviceNameS(id);
            type = (TouchDeviceType)SDL.GetTouchDeviceType(id);
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

        public ReadOnlySpan<Finger> Fingers
        {
            get
            {
                int count;
                var fingers = SDL.GetTouchFingers(id, &count);
                return new ReadOnlySpan<Finger>(fingers, count);
            }
        }

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
            touchEventArgs.FingerId = evnt.FingerID;
            touchEventArgs.Pressure = evnt.Pressure;
            touchEventArgs.X = evnt.X;
            touchEventArgs.Y = evnt.Y;
            touchEventArgs.State = FingerState.Up;

            TouchUp?.Invoke(this, touchEventArgs);
            return (this, touchEventArgs);
        }

        internal (TouchDevice, TouchEventArgs) OnFingerDown(SDLTouchFingerEvent evnt)
        {
            touchEventArgs.Timestamp = evnt.Timestamp;
            touchEventArgs.TouchDeviceId = id;
            touchEventArgs.FingerId = evnt.FingerID;
            touchEventArgs.Pressure = evnt.Pressure;
            touchEventArgs.X = evnt.X;
            touchEventArgs.Y = evnt.Y;
            touchEventArgs.State = FingerState.Down;

            TouchDown?.Invoke(this, touchEventArgs);
            return (this, touchEventArgs);
        }

        internal (TouchDevice, TouchMotionEventArgs) OnFingerMotion(SDLTouchFingerEvent evnt)
        {
            touchMotionEventArgs.Timestamp = evnt.Timestamp;
            touchMotionEventArgs.TouchDeviceId = id;
            touchMotionEventArgs.FingerId = evnt.FingerID;
            touchMotionEventArgs.Pressure = evnt.Pressure;
            touchMotionEventArgs.X = evnt.X;
            touchMotionEventArgs.Y = evnt.Y;
            touchMotionEventArgs.Dx = evnt.Dx;
            touchMotionEventArgs.Dy = evnt.Dy;

            TouchMotion?.Invoke(this, touchMotionEventArgs);
            return (this, touchMotionEventArgs);
        }

        /// <summary>
        /// Gets a finger by its ID.
        /// </summary>
        /// <param name="fingerId">The ID of the finger to retrieve.</param>
        /// <returns>The <see cref="Finger"/> associated with the specified ID.</returns>
        public Finger GetFingerById(long fingerId)
        {
            var fingers = Fingers;
            for (int i = 0; i < fingers.Length; i++)
            {
                if (fingers[i].Id == fingerId)
                {
                    return fingers[i];
                }
            }
            return default;
        }

        /// <summary>
        /// Gets a finger by its index.
        /// </summary>
        /// <param name="index">The index of the finger to retrieve.</param>
        /// <returns>The <see cref="Finger"/> at the specified index.</returns>
        public Finger GetFingerByIndex(int index)
        {
            return Fingers[index];
        }
    }
}