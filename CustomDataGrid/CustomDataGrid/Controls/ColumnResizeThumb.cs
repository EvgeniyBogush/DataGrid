using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Eneca.CustomDataGrid.Theme;

namespace Eneca.CustomDataGrid.Controls
{
    /// <summary>
    /// Single header control: 1px separator (#F5F5F5, 24px, 4px vertical inset) + column resize drag.
    /// </summary>
    internal sealed class ColumnResizeThumb : Thumb
    {
        public static readonly DependencyProperty DataColumnIndexProperty =
            DependencyProperty.Register(
                nameof(DataColumnIndex),
                typeof(int),
                typeof(ColumnResizeThumb),
                new PropertyMetadata(-1));

        public int DataColumnIndex
        {
            get => (int)GetValue(DataColumnIndexProperty);
            set => SetValue(DataColumnIndexProperty, value);
        }

        public EnecaDataGrid OwnerGrid { get; set; }

        static ColumnResizeThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ColumnResizeThumb),
                new FrameworkPropertyMetadata(typeof(ColumnResizeThumb)));
        }

        public ColumnResizeThumb()
        {
            Height = DataGridTheme.HeaderHeight;
            Background = Brushes.Transparent;
            Cursor = DataGridCursors.ColumnResize;
            Focusable = false;
            Template = CreateVisualTemplate();
            DragStarted += OnDragStarted;
            DragCompleted += OnDragCompleted;
            DragDelta += OnDragDelta;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            OwnerGrid?.NotifyResizeThumbMouseEnter(DataColumnIndex);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            OwnerGrid?.NotifyResizeThumbMouseLeave(DataColumnIndex);
        }

        private static ControlTemplate CreateVisualTemplate()
        {
            var line = new FrameworkElementFactory(typeof(Border));
            line.SetValue(Border.WidthProperty, DataGridTheme.SeparatorWidth);
            line.SetValue(Border.HeightProperty, DataGridTheme.SeparatorHeight);
            line.SetValue(Border.BackgroundProperty, new SolidColorBrush(DataGridTheme.HeaderSeparator));
            line.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
            line.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top);
            line.SetValue(MarginProperty, DataGridTheme.HeaderSeparatorMargin);

            var template = new ControlTemplate(typeof(ColumnResizeThumb));
            template.VisualTree = line;
            return template;
        }

        private void OnDragStarted(object sender, DragStartedEventArgs e) =>
            Mouse.OverrideCursor = DataGridCursors.ColumnResize;

        private void OnDragCompleted(object sender, DragCompletedEventArgs e) =>
            Mouse.OverrideCursor = null;

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (OwnerGrid == null || DataColumnIndex < 0)
                return;

            OwnerGrid.ResizeDataColumn(DataColumnIndex, e.HorizontalChange);
        }
    }
}
