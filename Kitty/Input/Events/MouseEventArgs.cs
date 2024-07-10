namespace Kitty.Input.Events
{
    using Kitty.Windows.Events;

    /// <summary>
    /// Provides a base class for mouse-related event arguments.
    /// </summary>
    public class MouseEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the identifier associated with the mouse device.
        /// </summary>
        public uint MouseId { get; internal set; }
    }
}