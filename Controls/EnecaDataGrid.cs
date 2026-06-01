using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace EnecaDataGrid.Controls;

public sealed class EnecaDataGrid : DataGrid
{
    private DataGridTemplateColumn? _checkBoxColumn;

    public EnecaDataGrid()
    {
        Loaded += (_, _) => ApplyCurrentConfiguration();
        Columns.CollectionChanged += ColumnsCollectionChanged;
        ColumnReordered += (_, _) =>
        {
            EnsureCheckBoxColumnPosition();
            ScheduleUpdateColumnHeaderStates();
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

    public static readonly DependencyProperty FilterColumnIndicesProperty =
        DependencyProperty.Register(
            nameof(FilterColumnIndices),
            typeof(string),
            typeof(EnecaDataGrid),
            new PropertyMetadata(null, OnFilterColumnIndicesChanged));

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

    public string? FilterColumnIndices
    {
        get => (string?)GetValue(FilterColumnIndicesProperty);
        set => SetValue(FilterColumnIndicesProperty, value);
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
            grid.UpdateFilterVisibility();
        }
    }

    private static void OnFreezeFirstColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnecaDataGrid grid)
        {
            grid.UpdateFrozenColumns();
        }
    }

    private static void OnFilterColumnIndicesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnecaDataGrid grid)
        {
            grid.UpdateFilterVisibility();
        }
    }

    private void ColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        EnsureCheckBoxColumnPosition();
        UpdateFrozenColumns();
        UpdateFilterVisibility();
        ScheduleUpdateColumnHeaderStates();
    }

    private void ApplyCurrentConfiguration()
    {
        UpdateCheckBoxColumn();
        UpdateFrozenColumns();
        UpdateFilterVisibility();
        ScheduleUpdateColumnHeaderStates();
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
            if (SelectionMode == DataGridSelectionMode.Single)
            {
                SelectionMode = DataGridSelectionMode.Extended;
            }
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
        ScheduleUpdateColumnHeaderStates();
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
        rowCheckBoxFactory.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
            Mode = BindingMode.TwoWay
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

        headerCheckBox.Checked += (_, _) => SelectAll();
        headerCheckBox.Unchecked += (_, _) => UnselectAll();
        return headerCheckBox;
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

    private void UpdateFilterVisibility()
    {
        var allowedIndices = ParseIndices(FilterColumnIndices);
        var dataColumnIndex = 0;

        foreach (var column in Columns)
        {
            if (ReferenceEquals(column, _checkBoxColumn))
            {
                continue;
            }

            if (column.Header is ColumnFilterViewModel filterViewModel)
            {
                filterViewModel.IsFilterVisible = allowedIndices.Contains(dataColumnIndex);
            }

            dataColumnIndex++;
        }
    }

    private static HashSet<int> ParseIndices(string? value)
    {
        var result = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (var chunk in value.Split(','))
        {
            if (int.TryParse(chunk.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private void ScheduleUpdateColumnHeaderStates()
    {
        Dispatcher.BeginInvoke(UpdateColumnHeaderStates, DispatcherPriority.Loaded);
    }

    private void UpdateColumnHeaderStates()
    {
        var lastDisplayIndex = Columns.Count - 1;

        foreach (var header in FindDescendants<DataGridColumnHeader>(this))
        {
            var isCheckBoxHeader = ReferenceEquals(header.Column, _checkBoxColumn);
            var hideRightSeparator = isCheckBoxHeader || header.Column?.DisplayIndex == lastDisplayIndex;

            ColumnHeaderState.SetIsCheckBoxHeader(header, isCheckBoxHeader);
            ColumnHeaderState.SetHideRightSeparator(header, hideRightSeparator);
        }
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
