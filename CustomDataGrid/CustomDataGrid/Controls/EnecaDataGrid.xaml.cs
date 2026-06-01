using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Eneca.CustomDataGrid.Models;
using Eneca.CustomDataGrid.Theme;

namespace Eneca.CustomDataGrid.Controls
{
    public partial class EnecaDataGrid : UserControl
    {
        private ScrollViewer _headerScroll;
        private ScrollViewer _bodyHorizontalScroll;
        private Grid _headerFrozen;
        private Grid _headerScrollable;
        private Grid _bodyFrozenHost;
        private StackPanel _bodyFrozenRows;
        private StackPanel _bodyScrollableRows;
        private bool _isRebuilding;
        private Grid _bodyFrozenLayout;
        private Grid _headerFrozenLayout;
        private readonly System.Collections.Generic.HashSet<int> _filterColumnIndices =
            new System.Collections.Generic.HashSet<int>();
        private readonly System.Collections.Generic.List<ColumnLayoutHost> _layoutHosts =
            new System.Collections.Generic.List<ColumnLayoutHost>();

        private sealed class ColumnLayoutHost
        {
            public Grid Grid { get; set; }
            public int ColumnOffset { get; set; }
        }

        public static readonly DependencyProperty FreezeFirstColumnProperty =
            DependencyProperty.Register(
                nameof(FreezeFirstColumn),
                typeof(bool),
                typeof(EnecaDataGrid),
                new PropertyMetadata(false, OnLayoutOptionChanged));

        public static readonly DependencyProperty FirstColumnIsCheckBoxColumnProperty =
            DependencyProperty.Register(
                nameof(FirstColumnIsCheckBoxColumn),
                typeof(bool),
                typeof(EnecaDataGrid),
                new PropertyMetadata(false, OnLayoutOptionChanged));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(ObservableCollection<DataGridColumnModel>),
                typeof(EnecaDataGrid),
                new PropertyMetadata(null, OnColumnsChanged));

        public static readonly DependencyProperty FilterColumnIndicesProperty =
            DependencyProperty.Register(
                nameof(FilterColumnIndices),
                typeof(string),
                typeof(EnecaDataGrid),
                new PropertyMetadata(string.Empty, OnFilterColumnIndicesChanged));

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(ObservableCollection<DataGridRowModel>),
                typeof(EnecaDataGrid),
                new PropertyMetadata(null, OnRowsChanged));

        public EnecaDataGrid()
        {
            InitializeComponent();
            Columns = new ObservableCollection<DataGridColumnModel>();
            Rows = new ObservableCollection<DataGridRowModel>();
            ParseFilterColumnIndices();
            Loaded += (_, __) => RebuildGrid();
        }

        public event EventHandler<DataGridColumnEventArgs> FilterIconClick;

        public bool FreezeFirstColumn
        {
            get => (bool)GetValue(FreezeFirstColumnProperty);
            set => SetValue(FreezeFirstColumnProperty, value);
        }

        public bool FirstColumnIsCheckBoxColumn
        {
            get => (bool)GetValue(FirstColumnIsCheckBoxColumnProperty);
            set => SetValue(FirstColumnIsCheckBoxColumnProperty, value);
        }

        public ObservableCollection<DataGridColumnModel> Columns
        {
            get => (ObservableCollection<DataGridColumnModel>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// Zero-based indices of data columns (Columns collection) that show a filter icon in the header.
        /// Example in XAML: FilterColumnIndices="2,3"
        /// </summary>
        public string FilterColumnIndices
        {
            get => (string)GetValue(FilterColumnIndicesProperty);
            set => SetValue(FilterColumnIndicesProperty, value);
        }

        public ObservableCollection<DataGridRowModel> Rows
        {
            get => (ObservableCollection<DataGridRowModel>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        private static void OnLayoutOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((EnecaDataGrid)d).RebuildGrid();

        private static void OnFilterColumnIndicesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (EnecaDataGrid)d;
            grid.ParseFilterColumnIndices();
            grid.RebuildGrid();
        }

        private void ParseFilterColumnIndices()
        {
            _filterColumnIndices.Clear();
            if (string.IsNullOrWhiteSpace(FilterColumnIndices))
                return;

            foreach (var part in FilterColumnIndices.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var index) && index >= 0)
                    _filterColumnIndices.Add(index);
            }
        }

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (EnecaDataGrid)d;
            if (e.OldValue is ObservableCollection<DataGridColumnModel> oldCols)
            {
                oldCols.CollectionChanged -= grid.OnColumnsCollectionChanged;
                grid.UnhookColumns(oldCols);
            }
            if (e.NewValue is ObservableCollection<DataGridColumnModel> newCols)
            {
                newCols.CollectionChanged += grid.OnColumnsCollectionChanged;
                grid.HookColumns(newCols);
            }
            grid.RebuildGrid();
        }

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (EnecaDataGrid)d;
            if (e.OldValue is ObservableCollection<DataGridRowModel> oldRows)
            {
                oldRows.CollectionChanged -= grid.OnRowsCollectionChanged;
                grid.UnhookRows(oldRows);
            }
            if (e.NewValue is ObservableCollection<DataGridRowModel> newRows)
            {
                newRows.CollectionChanged += grid.OnRowsCollectionChanged;
                grid.HookRows(newRows);
            }
            grid.RebuildGrid();
        }

        private void OnColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (DataGridColumnModel col in e.OldItems)
                    col.PropertyChanged -= OnColumnPropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (DataGridColumnModel col in e.NewItems)
                    col.PropertyChanged += OnColumnPropertyChanged;
            }
            RebuildGrid();
        }

        private void HookColumns(ObservableCollection<DataGridColumnModel> columns)
        {
            foreach (var col in columns)
                col.PropertyChanged += OnColumnPropertyChanged;
        }

        private void UnhookColumns(ObservableCollection<DataGridColumnModel> columns)
        {
            foreach (var col in columns)
                col.PropertyChanged -= OnColumnPropertyChanged;
        }

        private void OnColumnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isRebuilding)
                return;

            if (e.PropertyName == nameof(DataGridColumnModel.Width))
                SyncColumnWidths();
            else if (e.PropertyName == nameof(DataGridColumnModel.ShowFilterIcon)
                     || e.PropertyName == nameof(DataGridColumnModel.Header))
                RebuildGrid();
            else if (sender is DataGridColumnModel column)
                OnHeaderSelectionColumnPropertyChanged(column, e.PropertyName);
        }

        private void OnRowsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RebuildGrid();

        private void HookRows(ObservableCollection<DataGridRowModel> rows)
        {
            foreach (var row in rows)
                row.PropertyChanged += OnRowPropertyChanged;
        }

        private void UnhookRows(ObservableCollection<DataGridRowModel> rows)
        {
            foreach (var row in rows)
                row.PropertyChanged -= OnRowPropertyChanged;
        }

        private void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataGridRowModel.IsChecked) && !_isRebuilding)
                return;
        }

        private void RebuildGrid()
        {
            if (!IsLoaded)
                return;

            _isRebuilding = true;
            try
            {
                HeaderHost.Children.Clear();
                RowsPanel.Children.Clear();
                _layoutHosts.Clear();
                ClearHeaderSelectionState();
                _headerScroll = null;
                _bodyHorizontalScroll = null;
                _bodyFrozenLayout = null;
                _headerFrozenLayout = null;

                var columnWidths = GetEffectiveColumnWidths();
                if (columnWidths.Count == 0)
                    return;

                var frozenColumnCount = GetFrozenColumnCount(columnWidths);
                var useFreeze = frozenColumnCount > 0;
                var frozenWidth = useFreeze ? SumWidths(columnWidths, 0, frozenColumnCount) : 0.0;
                var scrollWidths = useFreeze
                    ? columnWidths.GetRange(frozenColumnCount, columnWidths.Count - frozenColumnCount)
                    : columnWidths;
                var frozenWidths = useFreeze
                    ? columnWidths.GetRange(0, frozenColumnCount)
                    : new System.Collections.Generic.List<double>();

                BuildHeader(useFreeze, frozenWidth, frozenWidths, scrollWidths, frozenColumnCount, columnWidths);
                BuildBody(useFreeze, frozenWidth, frozenWidths, scrollWidths, frozenColumnCount, columnWidths);
            }
            finally
            {
                _isRebuilding = false;
            }
        }

        private System.Collections.Generic.List<double> GetEffectiveColumnWidths()
        {
            var widths = new System.Collections.Generic.List<double>();
            if (FirstColumnIsCheckBoxColumn)
                widths.Add(DataGridTheme.CheckboxColumnWidth);

            foreach (var col in Columns)
                widths.Add(col.Width > 0 ? col.Width : DataGridTheme.DefaultColumnWidth);

            return widths;
        }

        private int GetFrozenColumnCount(System.Collections.Generic.List<double> allWidths)
        {
            if (!FreezeFirstColumn || allWidths.Count == 0)
                return 0;

            if (FirstColumnIsCheckBoxColumn)
                return System.Math.Min(2, allWidths.Count);

            return 1;
        }

        private static double SumWidths(System.Collections.Generic.List<double> widths, int start, int count)
        {
            var sum = 0.0;
            for (var i = start; i < start + count && i < widths.Count; i++)
                sum += widths[i];
            return sum;
        }

        private void BuildHeader(
            bool useFreeze,
            double frozenWidth,
            System.Collections.Generic.List<double> frozenWidths,
            System.Collections.Generic.List<double> scrollWidths,
            int frozenColumnCount,
            System.Collections.Generic.List<double> allWidths)
        {
            var headerShell = CreateHeaderShell();
            var headerInner = new Grid { Height = DataGridTheme.HeaderHeight };

            if (!useFreeze)
            {
                var row = BuildHeaderCellsRow(allWidths, isHeader: true);
                headerInner.Children.Add(row);
                headerShell.Child = headerInner;
                HeaderHost.Children.Add(headerShell);
                return;
            }

            headerShell.Child = headerInner;
            var layout = new Grid();
            _headerFrozenLayout = layout;
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(frozenWidth) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _headerFrozen = new Grid();
            _headerFrozen.Children.Add(BuildHeaderCellsRow(
                frozenWidths,
                isHeader: true,
                columnOffset: 0));

            _headerScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.Transparent
            };
            _headerScrollable = new Grid();
            _headerScrollable.Children.Add(BuildHeaderCellsRow(
                scrollWidths,
                isHeader: true,
                columnOffset: frozenColumnCount));
            _headerScroll.Content = _headerScrollable;
            _headerScroll.ScrollChanged += OnHeaderScrollChanged;

            Grid.SetColumn(_headerFrozen, 0);
            Grid.SetColumn(_headerScroll, 1);
            layout.Children.Add(_headerFrozen);
            layout.Children.Add(_headerScroll);
            headerInner.Children.Add(layout);
            HeaderHost.Children.Add(headerShell);
        }

        private void BuildBody(
            bool useFreeze,
            double frozenWidth,
            System.Collections.Generic.List<double> frozenWidths,
            System.Collections.Generic.List<double> scrollWidths,
            int frozenColumnCount,
            System.Collections.Generic.List<double> allWidths)
        {
            BodyScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            RowsPanel.Children.Clear();

            if (!useFreeze)
            {
                foreach (var rowModel in Rows)
                    RowsPanel.Children.Add(CreateRow(rowModel, allWidths, 0));
                return;
            }

            var bodyLayout = new Grid();
            _bodyFrozenLayout = bodyLayout;
            bodyLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(frozenWidth) });
            bodyLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _bodyFrozenHost = new Grid();
            _bodyFrozenRows = new StackPanel();
            _bodyFrozenHost.Children.Add(_bodyFrozenRows);

            _bodyHorizontalScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.Transparent
            };
            _bodyScrollableRows = new StackPanel();
            _bodyHorizontalScroll.Content = _bodyScrollableRows;
            _bodyHorizontalScroll.ScrollChanged += OnBodyHorizontalScrollChanged;

            foreach (var rowModel in Rows)
            {
                _bodyFrozenRows.Children.Add(CreateRow(rowModel, frozenWidths, 0));
                _bodyScrollableRows.Children.Add(CreateRow(rowModel, scrollWidths, frozenColumnCount));
            }

            Grid.SetColumn(_bodyFrozenHost, 0);
            Grid.SetColumn(_bodyHorizontalScroll, 1);
            bodyLayout.Children.Add(_bodyFrozenHost);
            bodyLayout.Children.Add(_bodyHorizontalScroll);

            RowsPanel.Children.Add(bodyLayout);
            BodyScroll.Content = RowsPanel;
        }

        private void OnHeaderScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_bodyHorizontalScroll != null && Math.Abs(_bodyHorizontalScroll.HorizontalOffset - e.HorizontalOffset) > 0.5)
                _bodyHorizontalScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void OnBodyHorizontalScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_headerScroll != null && Math.Abs(_headerScroll.HorizontalOffset - e.HorizontalOffset) > 0.5)
                _headerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private static Border CreateHeaderShell()
        {
            return new Border
            {
                Background = new SolidColorBrush(DataGridTheme.HeaderBackground),
                CornerRadius = new CornerRadius(DataGridTheme.HeaderCornerRadius),
                Padding = new Thickness(0),
                ClipToBounds = true
            };
        }

        private Grid BuildHeaderCellsRow(
            System.Collections.Generic.List<double> widths,
            bool isHeader,
            int columnOffset = 0)
        {
            var grid = new Grid { Height = DataGridTheme.HeaderHeight };
            for (var i = 0; i < widths.Count; i++)
            {
                var globalIndex = columnOffset + i;
                var width = widths[i];
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });

                var cell = CreateHeaderCell(globalIndex, width);
                Grid.SetColumn(cell, i);
                grid.Children.Add(cell);
            }

            _layoutHosts.Add(new ColumnLayoutHost { Grid = grid, ColumnOffset = columnOffset });
            return grid;
        }

        private UIElement CreateHeaderCell(int globalColumnIndex, double width)
        {
            var host = new Grid
            {
                Height = DataGridTheme.HeaderHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var text = GetHeaderText(globalColumnIndex);
            var showFilter = ShouldShowFilterIcon(globalColumnIndex);
            var showThumb = ShouldShowHeaderResizeThumb(globalColumnIndex);
            TextBlock label = null;

            if (IsCheckboxColumn(globalColumnIndex))
            {
                var checkAll = new CheckBox
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsThreeState = false
                };
                checkAll.Checked += (_, __) => SetAllRowsChecked(true);
                checkAll.Unchecked += (_, __) => SetAllRowsChecked(false);
                host.Children.Add(checkAll);
                return host;
            }

            if (showFilter && showThumb)
            {
                host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                host.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(GetResizeThumbHitWidth() + DataGridTheme.HeaderSeparatorFilterOffset)
                });

                label = CreateHeaderLabel(text, rightMargin: 4);
                Grid.SetColumn(label, 0);
                host.Children.Add(label);

                var filterIcon = CreateHeaderFilterIcon(globalColumnIndex);
                Grid.SetColumn(filterIcon, 1);
                host.Children.Add(filterIcon);

                var thumb = CreateResizeThumb(globalColumnIndex, alignWithFilter: true);
                Grid.SetColumn(thumb, 2);
                Panel.SetZIndex(thumb, 100);
                host.Children.Add(thumb);

                return WrapHeaderCellForSelection(host, label, globalColumnIndex);
            }

            if (showFilter)
            {
                host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                label = CreateHeaderLabel(text, rightMargin: 4);
                Grid.SetColumn(label, 0);
                host.Children.Add(label);

                var filterIcon = CreateHeaderFilterIcon(globalColumnIndex);
                ((FrameworkElement)filterIcon).Margin = new Thickness(0, 0, DataGridTheme.HeaderFilterIconRightPadding, 0);
                Grid.SetColumn(filterIcon, 1);
                host.Children.Add(filterIcon);

                return WrapHeaderCellForSelection(host, label, globalColumnIndex);
            }

            label = CreateHeaderLabel(text, rightMargin: DataGridTheme.HeaderMargin);
            host.Children.Add(label);

            if (showThumb)
                PlaceHeaderResizeThumb(host, CreateResizeThumb(globalColumnIndex));

            return WrapHeaderCellForSelection(host, label, globalColumnIndex);
        }

        private static TextBlock CreateHeaderLabel(string text, double rightMargin)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = DataGridTheme.Montserrat,
                FontWeight = FontWeights.Bold,
                FontSize = DataGridTheme.HeaderFontSize,
                Foreground = new SolidColorBrush(DataGridTheme.HeaderText),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(DataGridTheme.HeaderMargin, 0, rightMargin, 0)
            };
        }

        private UIElement CreateHeaderFilterIcon(int globalColumnIndex)
        {
            var dataIndex = GetDataColumnIndex(globalColumnIndex);
            var iconHost = new Border
            {
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, DataGridTheme.HeaderFilterIconRightPadding, 0),
                Tag = "HeaderFilterIcon",
                Child = new Image
                {
                    Source = DataGridIcons.Filter,
                    Width = DataGridTheme.HeaderFilterIconSize,
                    Height = DataGridTheme.HeaderFilterIconSize,
                    Stretch = Stretch.Uniform,
                    IsHitTestVisible = false
                }
            };

            iconHost.MouseLeftButtonUp += (_, __) =>
            {
                if (dataIndex >= 0 && dataIndex < Columns.Count)
                    FilterIconClick?.Invoke(this, new DataGridColumnEventArgs(dataIndex, Columns[dataIndex]));
            };

            return iconHost;
        }

        private DataGridColumnModel GetDataColumn(int globalColumnIndex)
        {
            var dataIndex = GetDataColumnIndex(globalColumnIndex);
            if (dataIndex < 0 || dataIndex >= Columns.Count)
                return null;
            return Columns[dataIndex];
        }

        private double GetResizeThumbHitWidth() =>
            EnableHeaderSelection
                ? DataGridTheme.ResizeThumbHitWidthInteractive
                : DataGridTheme.ResizeThumbHitWidth;

        private ColumnResizeThumb CreateResizeThumb(int globalColumnIndex, bool alignWithFilter = false)
        {
            var hitWidth = GetResizeThumbHitWidth();

            if (alignWithFilter)
            {
                return new ColumnResizeThumb
                {
                    OwnerGrid = this,
                    DataColumnIndex = GetDataColumnIndex(globalColumnIndex),
                    Width = hitWidth,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(DataGridTheme.HeaderSeparatorFilterOffset, 0, 0, 0)
                };
            }

            var hitOverflow = (hitWidth - DataGridTheme.SeparatorWidth) / 2;
            return new ColumnResizeThumb
            {
                OwnerGrid = this,
                DataColumnIndex = GetDataColumnIndex(globalColumnIndex),
                Width = hitWidth,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 0, -hitOverflow, 0)
            };
        }

        private static void PlaceHeaderResizeThumb(Grid host, ColumnResizeThumb thumb)
        {
            Panel.SetZIndex(thumb, 100);
            host.Children.Add(thumb);
        }

        internal void ResizeDataColumn(int dataColumnIndex, double horizontalDelta)
        {
            if (dataColumnIndex < 0 || dataColumnIndex >= Columns.Count)
                return;

            var column = Columns[dataColumnIndex];
            var newWidth = Math.Max(DataGridTheme.MinColumnWidth, column.Width + horizontalDelta);
            if (Math.Abs(newWidth - column.Width) < 0.01)
                return;

            column.Width = newWidth;
        }

        private static void SyncCellWidth(FrameworkElement element, double width)
        {
            element.Width = width;

            if (element is Border border && border.Child is FrameworkElement child)
                child.Width = width;
        }

        private void SyncColumnWidths()
        {
            if (_isRebuilding)
                return;

            var widths = GetEffectiveColumnWidths();

            foreach (var host in _layoutHosts)
            {
                var grid = host.Grid;
                for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
                {
                    var globalIdx = host.ColumnOffset + i;
                    if (globalIdx >= widths.Count)
                        continue;

                    var w = widths[globalIdx];
                    grid.ColumnDefinitions[i].Width = new GridLength(w);

                    foreach (UIElement child in grid.Children)
                    {
                        if (Grid.GetColumn(child) == i && child is FrameworkElement fe)
                            SyncCellWidth(fe, w);
                    }
                }
            }

            if (FreezeFirstColumn && widths.Count > 0)
            {
                var frozenColumnCount = GetFrozenColumnCount(widths);
                var frozenWidth = SumWidths(widths, 0, frozenColumnCount);
                if (_headerFrozenLayout != null && _headerFrozenLayout.ColumnDefinitions.Count > 0)
                    _headerFrozenLayout.ColumnDefinitions[0].Width = new GridLength(frozenWidth);
                if (_bodyFrozenLayout != null && _bodyFrozenLayout.ColumnDefinitions.Count > 0)
                    _bodyFrozenLayout.ColumnDefinitions[0].Width = new GridLength(frozenWidth);
            }
        }

        private FrameworkElement CreateRow(
            DataGridRowModel rowModel,
            System.Collections.Generic.List<double> widths,
            int columnOffset)
        {
            var rowBorder = new Border
            {
                Height = DataGridTheme.RowHeight,
                Background = new SolidColorBrush(DataGridTheme.RowBackground),
                Margin = new Thickness(0, 0, 0, 4),
                CornerRadius = new CornerRadius(8)
            };

            var grid = new Grid { Height = DataGridTheme.RowHeight };
            for (var i = 0; i < widths.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[i]) });
                var globalIndex = columnOffset + i;
                var cell = CreateRowCell(rowModel, globalIndex, widths[i]);
                Grid.SetColumn(cell, i);
                grid.Children.Add(cell);
            }

            _layoutHosts.Add(new ColumnLayoutHost { Grid = grid, ColumnOffset = columnOffset });
            rowBorder.Child = grid;
            return rowBorder;
        }

        private UIElement CreateRowCell(DataGridRowModel rowModel, int globalColumnIndex, double width)
        {
            var host = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };

            if (FirstColumnIsCheckBoxColumn && globalColumnIndex == 0)
            {
                var check = new CheckBox
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(DataGridRowModel.IsChecked))
                {
                    Source = rowModel,
                    Mode = BindingMode.TwoWay
                });
                host.Children.Add(check);
            }
            else
            {
                var dataIndex = FirstColumnIsCheckBoxColumn ? globalColumnIndex - 1 : globalColumnIndex;
                var text = dataIndex >= 0 && dataIndex < rowModel.Cells.Count
                    ? rowModel.Cells[dataIndex]
                    : string.Empty;

                var label = new TextBlock
                {
                    Text = text,
                    FontFamily = DataGridTheme.Montserrat,
                    FontWeight = FontWeights.Normal,
                    FontSize = DataGridTheme.RowFontSize,
                    Foreground = new SolidColorBrush(DataGridTheme.RowText),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = DataGridTheme.RowTextMargin,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                host.Children.Add(label);
            }

            if (ShouldDrawTrailingSeparator(globalColumnIndex))
            {
                var sep = CreateRowSeparator();
                sep.HorizontalAlignment = HorizontalAlignment.Right;
                sep.Margin = new Thickness(0, (DataGridTheme.RowHeight - DataGridTheme.SeparatorHeight) / 2, 0, 0);
                host.Children.Add(sep);
            }

            return host;
        }

        private static FrameworkElement CreateRowSeparator()
        {
            return new Border
            {
                Width = DataGridTheme.SeparatorWidth,
                Height = DataGridTheme.SeparatorHeight,
                Background = new SolidColorBrush(DataGridTheme.RowSeparator),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private string GetHeaderText(int globalColumnIndex)
        {
            if (FirstColumnIsCheckBoxColumn && globalColumnIndex == 0)
                return string.Empty;

            var dataIndex = FirstColumnIsCheckBoxColumn ? globalColumnIndex - 1 : globalColumnIndex;
            if (dataIndex < 0 || dataIndex >= Columns.Count)
                return string.Empty;
            return Columns[dataIndex].Header;
        }

        private bool IsCheckboxColumn(int globalColumnIndex) =>
            FirstColumnIsCheckBoxColumn && globalColumnIndex == 0;

        private int GetDataColumnIndex(int globalColumnIndex) =>
            FirstColumnIsCheckBoxColumn ? globalColumnIndex - 1 : globalColumnIndex;

        private bool ShouldShowFilterIcon(int globalColumnIndex)
        {
            if (IsCheckboxColumn(globalColumnIndex))
                return false;

            var dataIndex = GetDataColumnIndex(globalColumnIndex);
            if (dataIndex < 0 || dataIndex >= Columns.Count)
                return false;

            return _filterColumnIndices.Contains(dataIndex) || Columns[dataIndex].ShowFilterIcon;
        }

        private bool ShouldShowHeaderResizeThumb(int globalColumnIndex)
        {
            if (IsCheckboxColumn(globalColumnIndex))
                return false;

            var dataIndex = GetDataColumnIndex(globalColumnIndex);
            if (dataIndex < 0 || dataIndex >= Columns.Count)
                return false;

            var total = GetEffectiveColumnWidths().Count;
            return globalColumnIndex < total - 1;
        }

        private bool ShouldDrawTrailingSeparator(int globalColumnIndex)
        {
            if (IsCheckboxColumn(globalColumnIndex))
                return false;

            var total = GetEffectiveColumnWidths().Count;
            return globalColumnIndex < total - 1;
        }

        private void SetAllRowsChecked(bool isChecked)
        {
            foreach (var row in Rows)
                row.IsChecked = isChecked;
        }
    }
}
