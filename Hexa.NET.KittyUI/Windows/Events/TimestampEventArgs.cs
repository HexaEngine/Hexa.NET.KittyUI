namespace Hexa.NET.KittyUI.Windows.Events
{
    /// <summary>
    /// An event with a Timestamp.
    /// </summary>
    /// <seealso cref="EventArgs" />
    public class TimestampEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the timestamp of an event.
        /// </summary>
        public uint Timestamp { get; internal set; }
    }
}