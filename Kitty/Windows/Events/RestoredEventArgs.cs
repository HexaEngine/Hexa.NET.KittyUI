﻿namespace Kitty.Windows.Events
{
    /// <summary>
    /// Provides event arguments for the restored event of a window.
    /// </summary>
    public class RestoredEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the old state of the window.
        /// </summary>
        public WindowState OldState { get; internal set; }

        /// <summary>
        /// Gets the new state of the window.
        /// </summary>
        public WindowState NewState { get; internal set; }
    }
}