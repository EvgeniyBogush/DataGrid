using System;
using System.Windows;
using System.Windows.Media;

namespace Eneca.CustomDataGrid.Theme
{
    internal static class DataGridIcons
    {
        private static DrawingImage _filter;

        public static DrawingImage Filter => _filter ?? (_filter = LoadIcon("Filter"));

        private static DrawingImage LoadIcon(string key)
        {
            var resources = new ResourceDictionary
            {
                Source = new Uri($"/Eneca.CustomDataGrid;component/Controls/Icons/{key}.xaml", UriKind.Relative)
            };

            return resources[key] as DrawingImage;
        }
    }
}
