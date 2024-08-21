namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.KittyUI.Windows.Events;

    public class RestoreWindowRequest : RoutedEventArgs
    {
        public RestoreWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }
}