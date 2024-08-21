﻿namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.KittyUI.Windows.Events;

    public class MaximizeWindowRequest : RoutedEventArgs
    {
        public MaximizeWindowRequest(CoreWindow window)
        {
            Window = window;
        }

        public CoreWindow Window { get; }
    }
}