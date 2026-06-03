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

    static EnecaDataGrid()
    {
        EventManager.RegisterClassHandler(typeof(DataGridCell), LoadedEvent, new RoutedEventHandler(DataGridCellLoaded));
        EventManager.RegisterClassHandler(typeof(DataGridColumnHeader), LoadedEvent, new RoutedEventHandler(DataGridColumnHeaderLoaded));
        EventManager.RegisterClassHandler(typeof(DataGridCell), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(DataGridCellPreviewMouseLeftButtonDown), true);
        EventManager.RegisterClassHandler(typeof(DataGridCell), PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(DataGridCellPreviewMouseLeftButtonUp), true);
        EventManager.RegisterClassHandler(typeof(CheckBox), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(RowCheckBoxPreviewMouseLeftButtonDown), true);
    }

    public EnecaDataGrid()
    {
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

    public static readonly DependencyProperty FirstColumnIsCheckBoxColumnProperty =
        DependencyProperty.Register(
            nameof(FirstColumnIsCheckBoxColumn),
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false, OnFirstColumnIsCheckBoxColumnChanged));

    public static readonly DependencyProperty FreezeFirstColumnProperty =
        DependencyProperty.Register(
            nameof(FreezeFirstColumn),
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(false, OnFreezeFirstColumnChanged));

    public static readonly DependencyProperty EnableHeaderSelectionProperty =
        DependencyProperty.Register(
            nameof(EnableHeaderSelection),
            typeof(bool),
            typeof(EnecaDataGrid),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CellVisualStateModeProperty =
        DependencyProperty.Register(
            nameof(CellVisualStateMode),
            typeof(CellVisualStateMode),
            typeof(EnecaDataGrid),
            new PropertyMetadata(CellVisualStateMode.Interactive));

    public bool FirstColumnIsCheckBoxColumn
    {
        get => (bool)GetValue(FirstColumnIsCheckBoxColumnProperty);
        set => SetValue(FirstColumnIsCheckBoxColumnProperty, value);
    }

    public bool FreezeFirstColumn
    {
        get => (bool)GetValue(FreezeFirstColumnProperty);
        set => SetValue(FreezeFirstColumnProperty, value);
    }

    public bool EnableHeaderSelection
    {
        get => (bool)GetValue(EnableHeaderSelectionProperty);
        set => SetValue(EnableHeaderSelectionProperty, value);
    }

    public CellVisualStateMode CellVisualStateMode
    {
        get => (CellVisualStateMode)GetValue(CellVisualStateModeProperty);
        set => SetValue(CellVisualStateModeProperty, value);
    }

    private static void OnFirstColumnIsCheckBoxColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnecaDataGrid grid)
        {
            grid.UpdateCheckBoxColumn();
            grid.UpdateFrozenColumns();
        }
    }

    private static void OnFreezeFirstColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnecaDataGrid grid)
        {
            grid.UpdateFrozenColumns();
        }
    }

    private void ColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        EnsureCheckBoxColumnPosition();
        UpdateFrozenColumns();
        ScheduleUpdateColumnVisualStates();
        ScheduleUpdateRowSelectionStates();
    }

    private void ApplyCurrentConfiguration()
    {
        UpdateCheckBoxColumn();
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
            Path = new PropertyPath("(0)", ColumnHeaderState.IsRowCheckBoxCheckedProperty),
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

    private static void DataGridCellLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGridCell cell && FindAncestor<EnecaDataGrid>(cell) is { } grid)
        {
            grid.UpdateCellVisualState(cell, grid.GetLastVisibleDisplayIndex());
            if (FindAncestor<DataGridRow>(cell) is { } row)
            {
                grid.UpdateRowSelectionState(row);
            }
        }
    }

    private static void DataGridCellPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridCell cell || FindAncestor<EnecaDataGrid>(cell) is not { } grid)
        {
            return;
        }

        ColumnHeaderState.SetIsPressed(cell, true);

        if (grid.SelectionUnit != DataGridSelectionUnit.Cell)
        {
            return;
        }

        if (ReferenceEquals(cell.Column, grid._checkBoxColumn))
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
        if (grid.SelectedCells.Count == 1 && grid.SelectedCells.Contains(cellInfo))
        {
            return;
        }

        grid.SelectedCells.Clear();
        grid.SelectedCells.Add(cellInfo);
        e.Handled = true;
    }

    private static void DataGridCellPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridCell cell)
        {
            ColumnHeaderState.SetIsPressed(cell, false);
        }
    }

    private static void RowCheckBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not CheckBox checkBox || FindAncestor<EnecaDataGrid>(checkBox) is not { } grid)
        {
            return;
        }

        var cell = FindAncestor<DataGridCell>(checkBox);
        if (cell is null || !ReferenceEquals(cell.Column, grid._checkBoxColumn))
        {
            return;
        }

        e.Handled = true;

        var row = FindAncestor<DataGridRow>(checkBox);
        if (row is null)
        {
            return;
        }

        grid.SetRowSelected(row, !grid.IsItemFullySelected(row.Item));
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

        ColumnHeaderState.SetIsRowCheckBoxChecked(row, isChecked);
    }

    private static void DataGridColumnHeaderLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGridColumnHeader header && FindAncestor<EnecaDataGrid>(header) is { } grid)
        {
            var lastDisplayIndex = grid.GetLastVisibleDisplayIndex();
            grid.UpdateHeaderVisualState(header, lastDisplayIndex);
        }
    }

    private void UpdateHeaderVisualState(DataGridColumnHeader header, int lastDisplayIndex)
    {
        if (header.Column is null)
        {
            header.Visibility = Visibility.Collapsed;
            ColumnHeaderState.SetIsFilterVisible(header, false);
            return;
        }

        header.Visibility = Visibility.Visible;

        var isCheckBoxHeader = ReferenceEquals(header.Column, _checkBoxColumn);
        var hideRightSeparator = isCheckBoxHeader || IsLastVisibleColumn(header.Column, lastDisplayIndex);
        var isFilterVisible = !isCheckBoxHeader
            && header.Column.Header is ColumnFilterViewModel
            && GetColumnFilterVisibility(header.Column);

        ColumnHeaderState.SetIsCheckBoxHeader(header, isCheckBoxHeader);
        ColumnHeaderState.SetHideRightSeparator(header, hideRightSeparator);
        ColumnHeaderState.SetIsFilterVisible(header, isFilterVisible);
    }

    private static bool GetColumnFilterVisibility(DataGridColumn column)
    {
        if (column is IFilterVisibilityColumn filterVisibilityColumn)
        {
            return filterVisibilityColumn.IsFilterVisible;
        }

        return ColumnHeaderState.GetIsFilterVisible(column);
    }

    private void UpdateCellVisualState(DataGridCell cell, int lastDisplayIndex)
    {
        var hideRightSeparator = ReferenceEquals(cell.Column, _checkBoxColumn)
            || IsLastVisibleColumn(cell.Column, lastDisplayIndex);
        ColumnHeaderState.SetHideRightSeparator(cell, hideRightSeparator);
    }

    private int GetLastVisibleDisplayIndex()
    {
        return Columns
            .Where(column => column.Visibility == Visibility.Visible)
            .Select(column => column.DisplayIndex)
            .DefaultIfEmpty(-1)
            .Max();
    }

    private static bool IsLastVisibleColumn(DataGridColumn? column, int lastDisplayIndex)
    {
        return column is not null
            && column.Visibility == Visibility.Visible
            && column.DisplayIndex == lastDisplayIndex;
    }

    private static T? FindAncestor<T>(DependencyObject current)
        where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(current);
        while (parent is not null)
        {
            if (parent is T match)
            {
                return match;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private static bool IsInteractiveContentHit(DependencyObject? originalSource, DataGridCell ownerCell)
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

    private static IEnumerable<T> FindDescendants<T>(DependencyObject current)
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
}
