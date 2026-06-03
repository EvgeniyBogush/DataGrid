using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EnecaDataGrid.Controls;

public sealed class EnecaDataGrid : DataGrid
{
    private DataGridTemplateColumn? _checkBoxColumn;
    private bool _firstColumnIsCheckBoxColumn;
    private bool _freezeFirstColumn;
    private bool _enableHeaderSelection = true;
    private CellVisualStateMode _cellVisualStateMode = CellVisualStateMode.Interactive;

    public EnecaDataGrid()
    {
        AddHandler(DataGridCell.LoadedEvent, new RoutedEventHandler(DataGridCellLoaded), true);
        AddHandler(DataGridColumnHeader.LoadedEvent, new RoutedEventHandler(DataGridColumnHeaderLoaded), true);
        AddHandler(DataGridColumnHeader.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(DataGridColumnHeaderPreviewMouseLeftButtonDown), true);
        AddHandler(DataGridColumnHeader.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(DataGridColumnHeaderPreviewMouseLeftButtonUp), true);
        AddHandler(DataGridColumnHeader.MouseLeaveEvent, new MouseEventHandler(DataGridColumnHeaderMouseLeave), true);
        AddHandler(DataGridCell.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(DataGridCellPreviewMouseLeftButtonDown), true);
        AddHandler(DataGridCell.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(DataGridCellPreviewMouseLeftButtonUp), true);
        AddHandler(CheckBox.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(RowCheckBoxPreviewMouseLeftButtonDown), true);

        Loaded += (_, _) => ApplyCurrentConfiguration();
        Columns.CollectionChanged += ColumnsCollectionChanged;
        LoadingRow += (_, e) =>
        {
            ScheduleUpdateColumnVisualStates();
            UpdateRowSelectionState(e.Row);
        };
        SelectionChanged += (_, _) => ScheduleUpdateRowSelectionStates();
        SelectedCellsChanged += (_, _) => ScheduleUpdateRowSelectionStates();
        ColumnReordered += (_, _) =>
        {
            EnsureCheckBoxColumnPosition();
            ScheduleUpdateColumnVisualStates();
        };
    }

    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.RegisterAttached(
            "IsPressed",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsClickedProperty =
        DependencyProperty.RegisterAttached(
            "IsClicked",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsCheckBoxHeaderProperty =
        DependencyProperty.RegisterAttached(
            "IsCheckBoxHeader",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HideRightSeparatorProperty =
        DependencyProperty.RegisterAttached(
            "HideRightSeparator",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsRowCheckBoxCheckedProperty =
        DependencyProperty.RegisterAttached(
            "IsRowCheckBoxChecked",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.RegisterAttached(
            "IsFilterVisible",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ResizeThumbIconIsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "ResizeThumbIconIsEnabled",
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false, OnResizeThumbIconIsEnabledChanged));

    public static readonly DependencyProperty ResizeThumbIconSourceProperty =
        DependencyProperty.RegisterAttached(
            "ResizeThumbIconSource",
            typeof(ImageSource),
            typeof(EnecaDataGrid),
            new PropertyMetadata(null, OnResizeThumbIconSourceChanged));

    private static readonly DependencyProperty ResizeThumbIconStateProperty =
        DependencyProperty.RegisterAttached(
            "ResizeThumbIconState",
            typeof(ResizeThumbIconState),
            typeof(EnecaDataGrid),
            new PropertyMetadata(null));

    public bool FirstColumnIsCheckBoxColumn
    {
        get => _firstColumnIsCheckBoxColumn;
        set
        {
            if (_firstColumnIsCheckBoxColumn == value)
            {
                return;
            }

            _firstColumnIsCheckBoxColumn = value;
            UpdateCheckBoxColumn();
            UpdateFrozenColumns();
        }
    }

    public bool FreezeFirstColumn
    {
        get => _freezeFirstColumn;
        set
        {
            if (_freezeFirstColumn == value)
            {
                return;
            }

            _freezeFirstColumn = value;
            UpdateFrozenColumns();
        }
    }

    public bool EnableHeaderSelection
    {
        get => _enableHeaderSelection;
        set => _enableHeaderSelection = value;
    }

    public CellVisualStateMode CellVisualStateMode
    {
        get => _cellVisualStateMode;
        set
        {
            if (_cellVisualStateMode == value)
            {
                return;
            }

            _cellVisualStateMode = value;
            ScheduleUpdateColumnVisualStates();
        }
    }

    public static bool GetIsPressed(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsPressedProperty);
    }

    public static void SetIsPressed(DependencyObject obj, bool value)
    {
        obj.SetValue(IsPressedProperty, value);
    }

    public static bool GetIsClicked(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsClickedProperty);
    }

    public static void SetIsClicked(DependencyObject obj, bool value)
    {
        obj.SetValue(IsClickedProperty, value);
    }

    public static bool GetIsCheckBoxHeader(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsCheckBoxHeaderProperty);
    }

    public static void SetIsCheckBoxHeader(DependencyObject obj, bool value)
    {
        obj.SetValue(IsCheckBoxHeaderProperty, value);
    }

    public static bool GetHideRightSeparator(DependencyObject obj)
    {
        return (bool)obj.GetValue(HideRightSeparatorProperty);
    }

    public static void SetHideRightSeparator(DependencyObject obj, bool value)
    {
        obj.SetValue(HideRightSeparatorProperty, value);
    }

    public static bool GetIsRowCheckBoxChecked(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsRowCheckBoxCheckedProperty);
    }

    public static void SetIsRowCheckBoxChecked(DependencyObject obj, bool value)
    {
        obj.SetValue(IsRowCheckBoxCheckedProperty, value);
    }

    public static bool GetIsFilterVisible(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFilterVisibleProperty);
    }

    public static void SetIsFilterVisible(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFilterVisibleProperty, value);
    }

    public static bool GetResizeThumbIconIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(ResizeThumbIconIsEnabledProperty);
    }

    public static void SetResizeThumbIconIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(ResizeThumbIconIsEnabledProperty, value);
    }

    public static ImageSource? GetResizeThumbIconSource(DependencyObject obj)
    {
        return (ImageSource?)obj.GetValue(ResizeThumbIconSourceProperty);
    }

    public static void SetResizeThumbIconSource(DependencyObject obj, ImageSource? value)
    {
        obj.SetValue(ResizeThumbIconSourceProperty, value);
    }

    private static ResizeThumbIconState? GetResizeThumbIconState(DependencyObject obj)
    {
        return (ResizeThumbIconState?)obj.GetValue(ResizeThumbIconStateProperty);
    }

    private static void SetResizeThumbIconState(DependencyObject obj, ResizeThumbIconState? value)
    {
        obj.SetValue(ResizeThumbIconStateProperty, value);
    }

    private void ApplyCurrentConfiguration()
    {
        UpdateCheckBoxColumn();
        UpdateFrozenColumns();
        ScheduleUpdateColumnVisualStates();
        ScheduleUpdateRowSelectionStates();
    }

    private void ColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        EnsureCheckBoxColumnPosition();
        UpdateFrozenColumns();
        ScheduleUpdateColumnVisualStates();
        ScheduleUpdateRowSelectionStates();
    }

    private void UpdateCheckBoxColumn()
    {
        if (FirstColumnIsCheckBoxColumn)
        {
            if (_checkBoxColumn is null)
            {
                _checkBoxColumn = CreateCheckBoxColumn();
                Columns.Insert(0, _checkBoxColumn);
            }

            EnsureCheckBoxColumnPosition();
        }
        else if (_checkBoxColumn is not null)
        {
            Columns.Remove(_checkBoxColumn);
            _checkBoxColumn = null;
        }
    }

    private void EnsureCheckBoxColumnPosition()
    {
        if (_checkBoxColumn is null || !Columns.Contains(_checkBoxColumn))
        {
            return;
        }

        _checkBoxColumn.CanUserReorder = false;
        _checkBoxColumn.CanUserResize = false;
        _checkBoxColumn.Width = new DataGridLength(32);
        _checkBoxColumn.DisplayIndex = 0;
        ScheduleUpdateColumnVisualStates();
    }

    private DataGridTemplateColumn CreateCheckBoxColumn()
    {
        var column = new DataGridTemplateColumn
        {
            Width = new DataGridLength(32),
            CanUserResize = false,
            CanUserReorder = false,
            IsReadOnly = true,
            Header = CreateHeaderSelectionCheckBox(),
            HeaderStyle = TryFindResource("EnecaDataGridCheckBoxColumnHeaderStyle") as Style
        };

        var rowCheckBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
        rowCheckBoxFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        rowCheckBoxFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        rowCheckBoxFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
            Path = new PropertyPath("(0)", IsRowCheckBoxCheckedProperty),
            Mode = BindingMode.OneWay
        });

        if (TryFindResource("EnecaDataGridCellCheckBoxStyle") is Style checkBoxStyle)
        {
            rowCheckBoxFactory.SetValue(StyleProperty, checkBoxStyle);
        }

        column.CellTemplate = new DataTemplate { VisualTree = rowCheckBoxFactory };
        return column;
    }

    private CheckBox CreateHeaderSelectionCheckBox()
    {
        var headerCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (TryFindResource("EnecaDataGridCheckAllBoxStyle") is Style style)
        {
            headerCheckBox.Style = style;
        }

        headerCheckBox.Checked += (_, _) => SetAllRowsSelected(true);
        headerCheckBox.Unchecked += (_, _) => SetAllRowsSelected(false);
        return headerCheckBox;
    }

    private void SetAllRowsSelected(bool selected)
    {
        if (SelectionUnit != DataGridSelectionUnit.Cell)
        {
            if (selected)
            {
                SelectAll();
            }
            else
            {
                UnselectAll();
            }

            return;
        }

        if (selected)
        {
            SelectedCells.Clear();
            foreach (var item in Items)
            {
                if (ReferenceEquals(item, CollectionView.NewItemPlaceholder))
                {
                    continue;
                }

                SelectAllVisibleCellsForItem(item);
            }

            return;
        }

        SelectedCells.Clear();
    }

    private void UpdateFrozenColumns()
    {
        if (!FreezeFirstColumn)
        {
            FrozenColumnCount = 0;
            return;
        }

        var target = FirstColumnIsCheckBoxColumn ? 2 : 1;
        FrozenColumnCount = Math.Min(target, Columns.Count);
    }

    private void ScheduleUpdateColumnVisualStates()
    {
        Dispatcher.BeginInvoke(UpdateColumnVisualStates, DispatcherPriority.Loaded);
    }

    private void ScheduleUpdateRowSelectionStates()
    {
        Dispatcher.BeginInvoke(UpdateRowSelectionStates, DispatcherPriority.Loaded);
    }

    private void UpdateColumnVisualStates()
    {
        var lastDisplayIndex = GetLastVisibleDisplayIndex();

        foreach (var header in FindDescendants<DataGridColumnHeader>(this))
        {
            UpdateHeaderVisualState(header, lastDisplayIndex);
        }

        foreach (var cell in FindDescendants<DataGridCell>(this))
        {
            UpdateCellVisualState(cell, lastDisplayIndex);
        }
    }

    private void UpdateRowSelectionStates()
    {
        foreach (var row in FindDescendants<DataGridRow>(this))
        {
            UpdateRowSelectionState(row);
        }
    }

    private void DataGridCellLoaded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridCell>(source) is not { } cell)
        {
            return;
        }

        UpdateCellVisualState(cell, GetLastVisibleDisplayIndex());
        if (FindAncestor<DataGridRow>(cell) is { } row)
        {
            UpdateRowSelectionState(row);
        }
    }

    private void DataGridCellPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridCell>(source) is not { } cell)
        {
            return;
        }

        SetIsPressed(cell, true);

        if (SelectionUnit != DataGridSelectionUnit.Cell)
        {
            return;
        }

        if (ReferenceEquals(cell.Column, _checkBoxColumn))
        {
            return;
        }

        if (IsInteractiveContentHit(e.OriginalSource as DependencyObject, cell))
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        var cellInfo = new DataGridCellInfo(cell);
        if (SelectedCells.Count == 1 && SelectedCells.Contains(cellInfo))
        {
            return;
        }

        SelectedCells.Clear();
        SelectedCells.Add(cellInfo);
        e.Handled = true;
    }

    private void DataGridCellPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridCell>(source) is not { } cell)
        {
            return;
        }

        SetIsPressed(cell, false);
    }

    private void RowCheckBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<CheckBox>(source) is not { } checkBox)
        {
            return;
        }

        var cell = FindAncestor<DataGridCell>(checkBox);
        if (cell is null || !ReferenceEquals(cell.Column, _checkBoxColumn))
        {
            return;
        }

        e.Handled = true;

        var row = FindAncestor<DataGridRow>(checkBox);
        if (row is null)
        {
            return;
        }

        SetRowSelected(row, !IsItemFullySelected(row.Item));
    }

    private void SetRowSelected(DataGridRow row, bool selected)
    {
        if (SelectionUnit != DataGridSelectionUnit.Cell)
        {
            row.IsSelected = selected;
            if (selected)
            {
                SelectedItem = row.Item;
            }

            ScheduleUpdateRowSelectionStates();
            return;
        }

        var rowCells = Columns
            .Where(column => column.Visibility == Visibility.Visible)
            .Select(column => new DataGridCellInfo(row.Item, column))
            .ToList();

        if (selected)
        {
            foreach (var cellInfo in rowCells)
            {
                if (!SelectedCells.Contains(cellInfo))
                {
                    SelectedCells.Add(cellInfo);
                }
            }

            ScheduleUpdateRowSelectionStates();
            return;
        }

        foreach (var cellInfo in rowCells)
        {
            SelectedCells.Remove(cellInfo);
        }

        ScheduleUpdateRowSelectionStates();
    }

    private void SelectAllVisibleCellsForItem(object item)
    {
        foreach (var column in Columns.Where(column => column.Visibility == Visibility.Visible))
        {
            var cellInfo = new DataGridCellInfo(item, column);
            if (!SelectedCells.Contains(cellInfo))
            {
                SelectedCells.Add(cellInfo);
            }
        }
    }

    private bool IsItemFullySelected(object item)
    {
        var visibleColumns = Columns.Where(column => column.Visibility == Visibility.Visible).ToList();
        if (visibleColumns.Count == 0)
        {
            return false;
        }

        return visibleColumns.All(column => SelectedCells.Contains(new DataGridCellInfo(item, column)));
    }

    private void UpdateRowSelectionState(DataGridRow row)
    {
        var isChecked = SelectionUnit == DataGridSelectionUnit.Cell
            ? row.Item is not null && IsItemFullySelected(row.Item)
            : row.IsSelected;

        SetIsRowCheckBoxChecked(row, isChecked);
    }

    private void DataGridColumnHeaderLoaded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridColumnHeader>(source) is not { } header)
        {
            return;
        }

        var lastDisplayIndex = GetLastVisibleDisplayIndex();
        UpdateHeaderVisualState(header, lastDisplayIndex);
    }

    private void DataGridColumnHeaderPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridColumnHeader>(source) is not { } header)
        {
            return;
        }

        SetIsPressed(header, true);
        SetIsClicked(header, false);
    }

    private void DataGridColumnHeaderPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridColumnHeader>(source) is not { } header)
        {
            return;
        }

        ResetClickedHeaders(header);
        SetIsPressed(header, false);
        SetIsClicked(header, true);
    }

    private void DataGridColumnHeaderMouseLeave(object sender, MouseEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<DataGridColumnHeader>(source) is not { } header)
        {
            return;
        }

        SetIsPressed(header, false);
    }

    private void UpdateHeaderVisualState(DataGridColumnHeader header, int lastDisplayIndex)
    {
        if (header.Column is null)
        {
            header.Visibility = Visibility.Collapsed;
            SetIsFilterVisible(header, false);
            return;
        }

        header.Visibility = Visibility.Visible;

        var isCheckBoxHeader = ReferenceEquals(header.Column, _checkBoxColumn);
        var hideRightSeparator = isCheckBoxHeader || IsLastVisibleColumn(header.Column, lastDisplayIndex);
        var isFilterVisible = !isCheckBoxHeader
            && header.Column.Header is ColumnFilterViewModel
            && GetIsFilterVisible(header.Column);

        SetIsCheckBoxHeader(header, isCheckBoxHeader);
        SetHideRightSeparator(header, hideRightSeparator);
        SetIsFilterVisible(header, isFilterVisible);
    }

    private void UpdateCellVisualState(DataGridCell cell, int lastDisplayIndex)
    {
        var hideRightSeparator = ReferenceEquals(cell.Column, _checkBoxColumn)
            || IsLastVisibleColumn(cell.Column, lastDisplayIndex);
        SetHideRightSeparator(cell, hideRightSeparator);
    }

    private int GetLastVisibleDisplayIndex()
    {
        return Columns
            .Where(column => column.Visibility == Visibility.Visible)
            .Select(column => column.DisplayIndex)
            .DefaultIfEmpty(-1)
            .Max();
    }

    private bool IsLastVisibleColumn(DataGridColumn? column, int lastDisplayIndex)
    {
        return column is not null
            && column.Visibility == Visibility.Visible
            && column.DisplayIndex == lastDisplayIndex;
    }

    private void ResetClickedHeaders(DataGridColumnHeader activeHeader)
    {
        foreach (var header in FindDescendants<DataGridColumnHeader>(this))
        {
            if (!ReferenceEquals(header, activeHeader))
            {
                SetIsPressed(header, false);
                SetIsClicked(header, false);
            }
        }
    }

    private T? FindAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private bool IsInteractiveContentHit(DependencyObject? originalSource, DataGridCell ownerCell)
    {
        var current = originalSource;
        while (current is not null && !ReferenceEquals(current, ownerCell))
        {
            if (current is ButtonBase
                || current is TextBoxBase
                || current is Selector
                || current is DatePicker)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private IEnumerable<T> FindDescendants<T>(DependencyObject current)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
        {
            var child = VisualTreeHelper.GetChild(current, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static void OnResizeThumbIconIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Thumb thumb)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            AttachResizeThumbIcon(thumb);
        }
        else
        {
            DetachResizeThumbIcon(thumb);
        }
    }

    private static void OnResizeThumbIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Thumb thumb && GetResizeThumbIconState(thumb) is { } state)
        {
            state.Icon.Source = (ImageSource?)e.NewValue;
        }
    }

    private static void AttachResizeThumbIcon(Thumb thumb)
    {
        if (GetResizeThumbIconState(thumb) is not null)
        {
            return;
        }

        var icon = new Image
        {
            Width = 16,
            Height = 16,
            Source = GetResizeThumbIconSource(thumb),
            IsHitTestVisible = false
        };

        var popup = new Popup
        {
            AllowsTransparency = true,
            Focusable = false,
            IsHitTestVisible = false,
            Placement = PlacementMode.Relative,
            PlacementTarget = thumb,
            Child = icon
        };

        var state = new ResizeThumbIconState(popup, icon);
        SetResizeThumbIconState(thumb, state);

        thumb.Cursor = Cursors.None;
        thumb.MouseEnter += ThumbMouseEnter;
        thumb.MouseMove += ThumbMouseMove;
        thumb.MouseLeave += ThumbMouseLeave;
        thumb.DragStarted += ThumbDragStarted;
        thumb.DragDelta += ThumbDragDelta;
        thumb.DragCompleted += ThumbDragCompleted;
        thumb.Unloaded += ThumbUnloaded;
    }

    private static void DetachResizeThumbIcon(Thumb thumb)
    {
        if (GetResizeThumbIconState(thumb) is not { } state)
        {
            return;
        }

        state.Popup.IsOpen = false;
        SetResizeThumbIconState(thumb, null);

        thumb.MouseEnter -= ThumbMouseEnter;
        thumb.MouseMove -= ThumbMouseMove;
        thumb.MouseLeave -= ThumbMouseLeave;
        thumb.DragStarted -= ThumbDragStarted;
        thumb.DragDelta -= ThumbDragDelta;
        thumb.DragCompleted -= ThumbDragCompleted;
        thumb.Unloaded -= ThumbUnloaded;
    }

    private static void ThumbMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            ShowResizeThumbIconAtMouse(thumb);
        }
    }

    private static void ThumbMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            ShowResizeThumbIconAtMouse(thumb);
        }
    }

    private static void ThumbMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb && GetResizeThumbIconState(thumb) is { IsDragging: false } state)
        {
            state.Popup.IsOpen = false;
        }
    }

    private static void ThumbDragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is Thumb thumb && GetResizeThumbIconState(thumb) is { } state)
        {
            state.IsDragging = true;
            ShowResizeThumbIconAtMouse(thumb);
        }
    }

    private static void ThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            ShowResizeThumbIconAtMouse(thumb);
        }
    }

    private static void ThumbDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb && GetResizeThumbIconState(thumb) is { } state)
        {
            state.IsDragging = false;
            state.Popup.IsOpen = thumb.IsMouseOver;
            if (thumb.IsMouseOver)
            {
                ShowResizeThumbIconAtMouse(thumb);
            }
        }
    }

    private static void ThumbUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            DetachResizeThumbIcon(thumb);
        }
    }

    private static void ShowResizeThumbIconAtMouse(Thumb thumb)
    {
        if (GetResizeThumbIconState(thumb) is not { } state)
        {
            return;
        }

        var position = Mouse.GetPosition(thumb);
        state.Popup.HorizontalOffset = position.X - state.Icon.Width / 2;
        state.Popup.VerticalOffset = position.Y - state.Icon.Height / 2;
        state.Popup.IsOpen = true;
    }

    private sealed class ResizeThumbIconState
    {
        public ResizeThumbIconState(Popup popup, Image icon)
        {
            Popup = popup;
            Icon = icon;
        }

        public Popup Popup { get; }

        public Image Icon { get; }

        public bool IsDragging { get; set; }
    }
}
