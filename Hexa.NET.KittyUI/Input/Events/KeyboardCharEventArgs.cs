namespace Hexa.NET.KittyUI.Input.Events
{
    using Hexa.NET.KittyUI.Windows.Events;

    /// <summary>
    /// Provides data for keyboard character input events.
    /// </summary>
    public class TextInputEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextInputEventArgs"/> class.
        /// </summary>
        public TextInputEventArgs()
        {
        }

        /// <summary>
        /// Gets the character associated with the event.
        /// </summary>
        public unsafe byte* Text { get; internal set; }
    }
}