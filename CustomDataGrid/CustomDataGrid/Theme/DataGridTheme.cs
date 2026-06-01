using System.Windows;
using System.Windows.Media;

namespace Eneca.CustomDataGrid.Theme
{
    /// <summary>Design tokens for the grid (matches ENECA spec).</summary>
    public static class DataGridTheme
    {
        public const double HeaderHeight = 32;
        public const double RowHeight = 36;
        public const double HeaderCornerRadius = 8;
        public const double SeparatorWidth = 1;
        public const double SeparatorHeight = 24;
        public const double HeaderSeparatorVerticalMargin = 4;
        public static readonly Thickness HeaderSeparatorMargin = new Thickness(0, 4, 0, 4);

        public static readonly Color GridBackground = (Color)ColorConverter.ConvertFromString("#F5F5F5");
        public static readonly Color HeaderBackground = (Color)ColorConverter.ConvertFromString("#E6E6E6");
        public static readonly Color HeaderText = (Color)ColorConverter.ConvertFromString("#828282");
        public static readonly Color HeaderTextHover = (Color)ColorConverter.ConvertFromString("#155144");
        public static readonly Color HeaderTextPressed = (Color)ColorConverter.ConvertFromString("#86C3B7");
        public static readonly Color HeaderTextDisabled = (Color)ColorConverter.ConvertFromString("#BDBDBD");
        public static readonly Color HeaderTextActive = (Color)ColorConverter.ConvertFromString("#155144");
        public static readonly Color HeaderBackgroundActive = (Color)ColorConverter.ConvertFromString("#DCEFEB");
        public static readonly Color HeaderSeparator = (Color)ColorConverter.ConvertFromString("#F5F5F5");
        public static readonly Color RowBackground = (Color)ColorConverter.ConvertFromString("#FDFDFD");
        public static readonly Color RowText = (Color)ColorConverter.ConvertFromString("#1B1B1B");
        public static readonly Color RowSeparator = (Color)ColorConverter.ConvertFromString("#E6E6E6");

        public const double HeaderFontSize = 10;
        public const double RowFontSize = 14;
        public const double HeaderMargin = 8;
        public const double HeaderFilterIconSize = 16;
        public const double HeaderFilterIconRightPadding = 8;
        public const double HeaderSeparatorFilterOffset = 1;
        public static readonly Thickness RowTextMargin = new Thickness(8, 0, 8, 4);

        public const double CheckboxColumnWidth = 32;
        public const double MinColumnWidth = 50;
        public const double DefaultColumnWidth = 140;
        public const double ResizeThumbHitWidth = 2;
        public const double ResizeThumbHitWidthInteractive = 8;

        public static FontFamily Montserrat =>
            new FontFamily("Montserrat, Segoe UI, Arial");
    }
}
