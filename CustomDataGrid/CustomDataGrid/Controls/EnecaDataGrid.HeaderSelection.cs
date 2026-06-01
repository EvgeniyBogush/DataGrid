using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Eneca.CustomDataGrid.Models;
using Eneca.CustomDataGrid.Theme;

namespace Eneca.CustomDataGrid.Controls
{
    public partial class EnecaDataGrid
    {
        private sealed class HeaderCellPresenter
        {
            public Border Shell { get; set; }
            public TextBlock Label { get; set; }
            public int DataColumnIndex { get; set; }
        }

        private readonly System.Collections.Generic.Dictionary<int, HeaderCellPresenter> _headerCells =
            new System.Collections.Generic.Dictionary<int, HeaderCellPresenter>();

        private int _hoveredHeaderColumnIndex = -1;
        private int _pressedHeaderColumnIndex = -1;
        private int _resizeThumbHoverColumnIndex = -1;

        public static readonly DependencyProperty EnableHeaderSelectionProperty =
            DependencyProperty.Register(
                nameof(EnableHeaderSelection),
                typeof(bool),
                typeof(EnecaDataGrid),
                new PropertyMetadata(false, OnLayoutOptionChanged));

        /// <summary>
        /// Enables interactive header cell states (Hover, Pressed, Active, Clicked, Disabled).
        /// When false, the header looks and behaves as before.
        /// </summary>
        public bool EnableHeaderSelection
        {
            get => (bool)GetValue(EnableHeaderSelectionProperty);
            set => SetValue(EnableHeaderSelectionProperty, value);
        }

        public event EventHandler<DataGridColumnEventArgs> HeaderCellClick;

        private void ClearHeaderSelectionState()
        {
            _headerCells.Clear();
            _hoveredHeaderColumnIndex = -1;
            _pressedHeaderColumnIndex = -1;
            _resizeThumbHoverColumnIndex = -1;
        }

        internal void NotifyResizeThumbMouseEnter(int dataColumnIndex)
        {
            _resizeThumbHoverColumnIndex = dataColumnIndex;

            if (_hoveredHeaderColumnIndex == dataColumnIndex)
            {
                _hoveredHeaderColumnIndex = -1;
                ApplyHeaderCellVisual(dataColumnIndex);
            }
        }

        internal void NotifyResizeThumbMouseLeave(int dataColumnIndex)
        {
            if (_resizeThumbHoverColumnIndex != dataColumnIndex)
                return;

            _resizeThumbHoverColumnIndex = -1;

            if (_headerCells.TryGetValue(dataColumnIndex, out var presenter)
                && presenter.Shell.IsMouseOver
                && !IsHeaderColumnDisabled(dataColumnIndex))
            {
                _hoveredHeaderColumnIndex = dataColumnIndex;
                ApplyHeaderCellVisual(dataColumnIndex);
            }
        }

        private UIElement WrapHeaderCellForSelection(Grid content, TextBlock label, int globalColumnIndex)
        {
            if (!EnableHeaderSelection)
                return content;

            var dataIndex = GetDataColumnIndex(globalColumnIndex);
            if (dataIndex < 0)
                return content;

            var shell = new Border
            {
                Height = DataGridTheme.HeaderHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent,
                Child = content
            };

            _headerCells[dataIndex] = new HeaderCellPresenter
            {
                Shell = shell,
                Label = label,
                DataColumnIndex = dataIndex
            };

            shell.MouseEnter += (_, __) => OnHeaderCellMouseEnter(dataIndex);
            shell.MouseLeave += (_, __) => OnHeaderCellMouseLeave(dataIndex);
            shell.PreviewMouseMove += OnHeaderCellPreviewMouseMove;
            shell.PreviewMouseLeftButtonDown += OnHeaderCellPreviewMouseDown;
            shell.PreviewMouseLeftButtonUp += OnHeaderCellPreviewMouseUp;

            ApplyHeaderCellVisual(dataIndex);
            return shell;
        }

        private void OnHeaderCellMouseEnter(int dataColumnIndex)
        {
            if (!EnableHeaderSelection || IsHeaderColumnDisabled(dataColumnIndex))
                return;

            if (_resizeThumbHoverColumnIndex == dataColumnIndex)
                return;

            _hoveredHeaderColumnIndex = dataColumnIndex;
            ApplyHeaderCellVisual(dataColumnIndex);
        }

        private void OnHeaderCellPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!EnableHeaderSelection || !(sender is Border shell))
                return;

            if (!TryGetHeaderPresenter(shell, out var presenter))
                return;

            var dataIndex = presenter.DataColumnIndex;
            if (IsHeaderInteractionBlockedSource(e.OriginalSource as DependencyObject))
            {
                if (_hoveredHeaderColumnIndex == dataIndex)
                {
                    _hoveredHeaderColumnIndex = -1;
                    ApplyHeaderCellVisual(dataIndex);
                }

                return;
            }

            if (IsHeaderColumnDisabled(dataIndex))
                return;

            if (_hoveredHeaderColumnIndex != dataIndex)
            {
                _hoveredHeaderColumnIndex = dataIndex;
                ApplyHeaderCellVisual(dataIndex);
            }
        }

        private void OnHeaderCellMouseLeave(int dataColumnIndex)
        {
            if (_hoveredHeaderColumnIndex == dataColumnIndex)
                _hoveredHeaderColumnIndex = -1;

            if (_pressedHeaderColumnIndex == dataColumnIndex)
                _pressedHeaderColumnIndex = -1;

            ApplyHeaderCellVisual(dataColumnIndex);
        }

        private void OnHeaderCellPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!EnableHeaderSelection || !(sender is Border shell))
                return;

            if (!TryGetHeaderPresenter(shell, out var presenter))
                return;

            if (IsHeaderInteractionBlockedSource(e.OriginalSource as DependencyObject))
                return;

            if (IsHeaderColumnDisabled(presenter.DataColumnIndex))
            {
                e.Handled = true;
                return;
            }

            _pressedHeaderColumnIndex = presenter.DataColumnIndex;
            ApplyHeaderCellVisual(presenter.DataColumnIndex);
            shell.CaptureMouse();
        }

        private void OnHeaderCellPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!EnableHeaderSelection || !(sender is Border shell))
                return;

            if (!TryGetHeaderPresenter(shell, out var presenter))
                return;

            if (IsHeaderInteractionBlockedSource(e.OriginalSource as DependencyObject))
                return;

            var dataIndex = presenter.DataColumnIndex;
            var wasPressed = _pressedHeaderColumnIndex == dataIndex;
            _pressedHeaderColumnIndex = -1;

            if (shell.IsMouseCaptured)
                shell.ReleaseMouseCapture();

            if (wasPressed && !IsHeaderColumnDisabled(dataIndex))
            {
                var column = Columns[dataIndex];
                column.IsHeaderClicked = !column.IsHeaderClicked;
                HeaderCellClick?.Invoke(this, new DataGridColumnEventArgs(dataIndex, column));
            }

            ApplyHeaderCellVisual(dataIndex);
        }

        private bool TryGetHeaderPresenter(Border shell, out HeaderCellPresenter presenter)
        {
            foreach (var entry in _headerCells.Values)
            {
                if (entry.Shell == shell)
                {
                    presenter = entry;
                    return true;
                }
            }

            presenter = null;
            return false;
        }

        private static bool IsHeaderInteractionBlockedSource(DependencyObject source)
        {
            while (source != null)
            {
                if (source is ColumnResizeThumb || source is Thumb)
                    return true;

                if (source is FrameworkElement element && Equals(element.Tag, "HeaderFilterIcon"))
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private bool IsHeaderColumnDisabled(int dataColumnIndex) =>
            dataColumnIndex >= 0
            && dataColumnIndex < Columns.Count
            && Columns[dataColumnIndex].IsHeaderDisabled;

        private void ApplyHeaderCellVisual(int dataColumnIndex)
        {
            if (!_headerCells.TryGetValue(dataColumnIndex, out var presenter))
                return;

            var column = Columns[dataColumnIndex];
            var state = ResolveHeaderVisualState(dataColumnIndex, column);
            var highlighted = column.IsHeaderActive || column.IsHeaderClicked;

            presenter.Shell.Background = highlighted
                ? new SolidColorBrush(DataGridTheme.HeaderBackgroundActive)
                : Brushes.Transparent;

            presenter.Label.Foreground = new SolidColorBrush(GetHeaderTextColor(state));

            if (_resizeThumbHoverColumnIndex == dataColumnIndex)
                presenter.Shell.Cursor = Cursors.Arrow;
            else
                presenter.Shell.Cursor = state == DataGridHeaderVisualState.Disabled
                    ? Cursors.Arrow
                    : Cursors.Hand;
        }

        private DataGridHeaderVisualState ResolveHeaderVisualState(int dataColumnIndex, DataGridColumnModel column)
        {
            if (column.IsHeaderDisabled)
                return DataGridHeaderVisualState.Disabled;

            if (_pressedHeaderColumnIndex == dataColumnIndex)
                return DataGridHeaderVisualState.Pressed;

            if (_hoveredHeaderColumnIndex == dataColumnIndex)
                return DataGridHeaderVisualState.Hover;

            if (column.IsHeaderActive)
                return DataGridHeaderVisualState.Active;

            if (column.IsHeaderClicked)
                return DataGridHeaderVisualState.Clicked;

            return DataGridHeaderVisualState.Default;
        }

        private static Color GetHeaderTextColor(DataGridHeaderVisualState state)
        {
            switch (state)
            {
                case DataGridHeaderVisualState.Hover:
                case DataGridHeaderVisualState.Active:
                case DataGridHeaderVisualState.Clicked:
                    return DataGridTheme.HeaderTextActive;
                case DataGridHeaderVisualState.Pressed:
                    return DataGridTheme.HeaderTextPressed;
                case DataGridHeaderVisualState.Disabled:
                    return DataGridTheme.HeaderTextDisabled;
                default:
                    return DataGridTheme.HeaderText;
            }
        }

        private void OnHeaderSelectionColumnPropertyChanged(DataGridColumnModel column, string propertyName)
        {
            if (propertyName != nameof(DataGridColumnModel.IsHeaderDisabled)
                && propertyName != nameof(DataGridColumnModel.IsHeaderActive)
                && propertyName != nameof(DataGridColumnModel.IsHeaderClicked))
                return;

            var index = Columns.IndexOf(column);
            if (index >= 0)
                ApplyHeaderCellVisual(index);
        }
    }
}
