﻿namespace Hexa.NET.KittyUI.Input.Events
{
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Windows.Events;

    /// <summary>
    /// Provides a base class for gamepad-related event arguments.
    /// </summary>
    public class GamepadEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the ID of the gamepad associated with the event.
        /// </summary>
        public int GamepadId { get; internal set; }

        /// <summary>
        /// Gets the gamepad associated with the event.
        /// </summary>
        public Gamepad Gamepad => Gamepads.GetById(GamepadId);
    }
}