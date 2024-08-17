namespace Hexa.NET.KittyUI.Input.Events
{
    using Hexa.NET.KittyUI.Windows.Events;

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