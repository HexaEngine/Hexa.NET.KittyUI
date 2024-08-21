namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.KittyUI.Windows.Events;

    public class CloseWindowRequest : RoutedEventArgs
    {
        public CloseWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }
}