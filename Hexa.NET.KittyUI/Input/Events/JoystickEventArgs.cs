namespace Hexa.NET.KittyUI.Input.Events
{
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Windows.Events;

    /// <summary>
    /// Provides base data for joystick-related events.
    /// </summary>
    public class JoystickEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the ID of the joystick associated with the event.
        /// </summary>
        public int JoystickId { get; internal set; }

        /// <summary>
        /// Gets the joystick object associated with the event.
        /// </summary>
        public Joystick Joystick => Joysticks.GetById(JoystickId);
    }
}