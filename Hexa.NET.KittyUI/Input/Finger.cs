namespace Hexa.NET.KittyUI.Input
{
    using System.Numerics;

    /// <summary>
    /// Represents a finger in an input system.
    /// </summary>
    public readonly unsafe struct Finger
    {
        private readonly SDL3.SDLFinger* finger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Finger"/> class.
        /// </summary>
        /// <param name="finger">A pointer to the native SDL finger.</param>
        public Finger(SDL3.SDLFinger* finger)
        {
            this.finger = finger;
        }

        public bool IsNull => finger == null;

        /// <summary>
        /// Gets the unique identifier for the finger.
        /// </summary>
        public readonly long Id => finger->Id;

        /// <summary>
        /// Gets the current state of the finger.
        /// </summary>
        public readonly FingerState State => finger->Pressure > 0 ? FingerState.Down : FingerState.Up;

        /// <summary>
        /// Gets the X-coordinate of the finger's position.
        /// </summary>
        public readonly float X => finger->X;

        /// <summary>
        /// Gets the Y-coordinate of the finger's position.
        /// </summary>
        public readonly float Y => finger->Y;

        /// <summary>
        /// Gets the position represented as a two-dimensional vector.
        /// </summary>
        public readonly Vector2 Position => new(X, Y);

        /// <summary>
        /// Gets the pressure applied by the finger.
        /// </summary>
        public readonly float Pressure => finger->Pressure;
    }
}